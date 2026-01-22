using MedRemind.Core.DTOs;
using MedRemind.Services.AI.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.AI;

/// <summary>
/// Wraps parsers with circuit breaker pattern for resilience
/// Automatically disables failing parsers and re-enables after reset time
/// </summary>
public class ResilientParser : IPrescriptionParser
{
    private readonly IPrescriptionParser _innerParser;
    private readonly ILogger _logger;
    
    private int _failureCount = 0;
    private DateTime? _lastFailureTime;
    private const int MAX_FAILURES = 3;
    private static readonly TimeSpan CIRCUIT_RESET_TIME = TimeSpan.FromMinutes(5);
    
    public string Name => _innerParser.Name;
    public int Priority => _innerParser.Priority;
    public bool IsEnabled => _innerParser.IsEnabled && !IsCircuitOpen();
    public TimeSpan Timeout => _innerParser.Timeout;
    
    public ResilientParser(IPrescriptionParser innerParser, ILogger logger)
    {
        _innerParser = innerParser ?? throw new ArgumentNullException(nameof(innerParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<PrescriptionReadResult> ParseAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen())
        {
            _logger.LogWarning($"?? {Name} circuit is OPEN. Skipping parser.");
            _logger.LogWarning($"   Will reset after {GetTimeUntilReset().TotalMinutes:F1} minutes");
            
            return new PrescriptionReadResult
            {
                Success = false,
                Medications = new List<MedicationData>()
            };
        }
        
        CancellationTokenSource? cts = null;
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation($"?? {Name} starting (timeout: {Timeout.TotalSeconds}s)");
            
            // Execute with timeout
            cts = new CancellationTokenSource(Timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            
            var result = await _innerParser.ParseAsync(ocrText, linked.Token);
            
            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation($"? {Name} completed in {elapsed.TotalSeconds:F2}s");
            
            // Success - reset failure count
            if (result.Success)
            {
                if (_failureCount > 0)
                {
                    _logger.LogInformation($"? {Name} recovered. Resetting failure count.");
                    _failureCount = 0;
                    _lastFailureTime = null;
                }
            }
            else
            {
                // Soft failure (returned false but didn't throw)
                _logger.LogWarning($"?? {Name} returned failure (no medications found)");
                RecordFailure();
            }
            
            return result;
        }
        catch (OperationCanceledException) when (cts?.IsCancellationRequested == true)
        {
            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogWarning($"?? {Name} timed out after {elapsed.TotalSeconds:F2}s (limit: {Timeout.TotalSeconds}s)");
            RecordFailure();
            
            return new PrescriptionReadResult
            {
                Success = false,
                Medications = new List<MedicationData>()
            };
        }
        catch (Exception ex)
        {
            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogError(ex, $"? {Name} failed after {elapsed.TotalSeconds:F2}s: {ex.Message}");
            RecordFailure();
            
            return new PrescriptionReadResult
            {
                Success = false,
                Medications = new List<MedicationData>()
            };
        }
        finally
        {
            cts?.Dispose();
        }
    }
    
    public Task<bool> HealthCheckAsync()
    {
        if (IsCircuitOpen())
        {
            _logger.LogWarning($"?? {Name} circuit is open. Health check skipped.");
            return Task.FromResult(false);
        }
        
        return _innerParser.HealthCheckAsync();
    }
    
    private bool IsCircuitOpen()
    {
        if (_failureCount >= MAX_FAILURES && _lastFailureTime.HasValue)
        {
            var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime.Value;
            
            if (timeSinceLastFailure < CIRCUIT_RESET_TIME)
            {
                return true; // Circuit still open
            }
            else
            {
                // Reset circuit
                _logger.LogInformation($"?? {Name} circuit breaker RESET after {CIRCUIT_RESET_TIME.TotalMinutes} minutes");
                _failureCount = 0;
                _lastFailureTime = null;
                return false;
            }
        }
        
        return false;
    }
    
    private void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;
        
        _logger.LogWarning($"?? {Name} failure #{_failureCount}/{MAX_FAILURES}");
        
        if (_failureCount >= MAX_FAILURES)
        {
            _logger.LogError($"?? {Name} circuit breaker OPENED! Too many failures.");
            _logger.LogError($"   Parser disabled for {CIRCUIT_RESET_TIME.TotalMinutes} minutes");
        }
    }
    
    private TimeSpan GetTimeUntilReset()
    {
        if (_lastFailureTime.HasValue)
        {
            var elapsed = DateTime.UtcNow - _lastFailureTime.Value;
            var remaining = CIRCUIT_RESET_TIME - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        
        return TimeSpan.Zero;
    }
}
