using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Services.Prescriptions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MedRemind.Services.AI.Agents;

/// <summary>
/// Multi-LLM API Orchestrator for backend API
/// Manages multiple AI parsers (OpenAI, DeepSeek, Claude) with parallel/sequential execution, cross-validation, result merging, and caching
/// Provides intelligent routing, fallback mechanisms, and result aggregation across multiple LLM providers
/// </summary>
public class MultiLlmAPIOrchestrator
{
    private readonly OpenAIPrescriptionParserAgent _openAIAgent;
    private readonly DeepSeekPrescriptionParserAgent _deepSeekAgent;
    private readonly ClaudePrescriptionParserAgent _claudeAgent;
    private readonly ExecutionMode _executionMode;
    
    // Optional services for advanced features
    private readonly PrescriptionResultMergerService? _mergerService;
    private readonly PrescriptionValidationService? _validationService;
    private readonly PrescriptionCacheService? _cacheService;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly ILogger<MultiLlmAPIOrchestrator>? _logger;

    public MultiLlmAPIOrchestrator(
        OpenAIPrescriptionParserAgent openAIAgent,
        DeepSeekPrescriptionParserAgent deepSeekAgent,
        ClaudePrescriptionParserAgent claudeAgent,
        ExecutionMode executionMode = ExecutionMode.Parallel,
        PrescriptionResultMergerService? mergerService = null,
        PrescriptionValidationService? validationService = null,
        PrescriptionCacheService? cacheService = null,
        IUnitOfWork? unitOfWork = null,
        ILogger<MultiLlmAPIOrchestrator>? logger = null)
    {
        _openAIAgent = openAIAgent ?? throw new ArgumentNullException(nameof(openAIAgent));
        _deepSeekAgent = deepSeekAgent ?? throw new ArgumentNullException(nameof(deepSeekAgent));
        _claudeAgent = claudeAgent ?? throw new ArgumentNullException(nameof(claudeAgent));
        _executionMode = executionMode;
        _mergerService = mergerService;
        _validationService = validationService;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        
        var mode = executionMode == ExecutionMode.Parallel ? "Parallel" : "Sequential";
        _logger?.LogInformation($"? MultiLlmAPIOrchestrator initialized - Mode: {mode}");
        _logger?.LogInformation("   Parsers: OpenAI, DeepSeek, Claude");
        _logger?.LogInformation($"   Advanced Features: {(HasAdvancedFeatures() ? "Enabled" : "Disabled")}");
    }

    private bool HasAdvancedFeatures() => _mergerService != null && _validationService != null;

    /// <summary>
    /// Process prescription with multiple parsers, cross-validation, result merging, and caching
    /// </summary>
    public async Task<PrescriptionProcessingResult> ProcessPrescriptionAsync(
        string ocrText,
        string prescriptionFileName,
        int prescriptionId,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new PrescriptionProcessingResult
        {
            StartTime = startTime,
            PrescriptionFileName = prescriptionFileName,
            DatabaseId = prescriptionId
        };

        try
        {
            _logger?.LogInformation("?? Starting prescription processing...");
            _logger?.LogInformation($"   File: {prescriptionFileName}");
            _logger?.LogInformation($"   Prescription ID: {prescriptionId}");
            _logger?.LogInformation($"   OCR Text Length: {ocrText?.Length ?? 0}");

            // STEP 1: Check cache
            if (_cacheService != null)
            {
                if (_cacheService.TryGet(ocrText, out var cachedResult) && cachedResult != null)
                {
                    _logger?.LogInformation("?? Cache hit! Returning cached result");
                    cachedResult.DatabaseId = prescriptionId; // Update prescription ID
                    return cachedResult;
                }
            }

            // STEP 2: Validate input
            if (string.IsNullOrWhiteSpace(ocrText))
            {
                return FailFast("OCR text is empty or null", result);
            }

            // STEP 3: Execute parsers (parallel or sequential)
            var parserResults = _executionMode == ExecutionMode.Parallel
                ? await ExecuteParsersParallelAsync(ocrText, cancellationToken)
                : await ExecuteParsersSequentialAsync(ocrText, cancellationToken);
            
            if (!parserResults.Any())
            {
                return FailFast("All parsers failed or returned no results", result);
            }
            
            _logger?.LogInformation($"?? Parser Results: {parserResults.Count} successful");
            foreach (var (provider, parseResult) in parserResults)
            {
                _logger?.LogInformation($"   {provider}: {parseResult.Medications.Count} medications, Confidence: {parseResult.ConfidenceScore:P0}");
            }

            // STEP 4: Merge results (if merger service available)
            PrescriptionReadResult mergedResult;
            if (_mergerService != null && parserResults.Count > 1)
            {
                _logger?.LogInformation("?? Merging results from multiple parsers...");
                
                // Get the two best results to merge
                var sortedResults = parserResults.Values
                    .OrderByDescending(r => r.Medications.Count)
                    .ThenByDescending(r => r.ConfidenceScore)
                    .ToList();
                
                var primary = sortedResults[0];
                var secondary = sortedResults.Count > 1 ? sortedResults[1] : primary;
                var primaryProvider = parserResults.First(kvp => kvp.Value == primary).Key;
                var secondaryProvider = parserResults.Count > 1 ? parserResults.First(kvp => kvp.Value == secondary).Key : primaryProvider;
                
                mergedResult = _mergerService.MergeResults(primary, secondary, primaryProvider, secondaryProvider);
                _logger?.LogInformation($"   Merged: {mergedResult.Medications.Count} medications");
            }
            else
            {
                // Use best single result
                mergedResult = parserResults.Values
                    .OrderByDescending(r => r.Medications.Count)
                    .ThenByDescending(r => r.ConfidenceScore)
                    .First();
                _logger?.LogInformation("   Using single best result (no merger available)");
            }
            
            if (mergedResult == null || !mergedResult.Medications.Any())
            {
                return FailFast("No medications found after merging", result);
            }
            
            // STEP 5: Validate completeness (if validation service available)
            double averageConfidence = mergedResult.ConfidenceScore;
            bool isComplete = true;
            
            if (_validationService != null)
            {
                var validation = _validationService.ValidateCompleteness(mergedResult);
                averageConfidence = validation.AverageConfidence;
                isComplete = validation.IsComplete;
                
                _logger?.LogInformation($"?? Validation: {(validation.IsComplete ? "? Complete" : "?? Incomplete")}");
                _logger?.LogInformation($"   Confidence: {validation.AverageConfidence:P0}");
            }
            
            // STEP 6: Store in database (if unit of work available)
            if (_unitOfWork != null)
            {
                await StoreResultAsync(prescriptionId, ocrText, parserResults, mergedResult, averageConfidence, result);
            }
            
            // STEP 7: Finalize result
            result.ParseResult = mergedResult;
            result.PrescriptionResult = mergedResult; // Alternative property
            result.Success = true;
            result.MatchScore = averageConfidence;
            result.TotalAttempts = parserResults.Count;
            result.ProcessingAttempts = parserResults.Count;
            result.SelectedProvider = string.Join(" + ", parserResults.Keys);
            result.EndTime = DateTime.UtcNow;
            result.ProcessingTime = result.EndTime.Value - result.StartTime;
            
            // Add warning if incomplete
            if (!isComplete)
            {
                result.WarningMessage = "Some medication fields are incomplete. Please review the results.";
            }
            
            // STEP 8: Cache result (if cache service available)
            if (_cacheService != null)
            {
                _cacheService.Set(ocrText, result);
                _logger?.LogInformation("?? Result cached for future requests");
            }
            
            _logger?.LogInformation($"? Processing complete in {result.ProcessingTime.TotalSeconds:F2}s");
            _logger?.LogInformation($"   Providers: {result.SelectedProvider}");
            _logger?.LogInformation($"   Medications: {result.ParseResult.Medications.Count}");
            _logger?.LogInformation($"   Confidence: {result.MatchScore:P0}");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Orchestrator error");
            result.Success = false;
            result.ErrorMessage = $"Processing failed: {ex.Message}";
            result.EndTime = DateTime.UtcNow;
            result.ProcessingTime = result.EndTime.Value - startTime;
            return result;
        }
    }

    /// <summary>
    /// Execute all parsers in parallel
    /// </summary>
    private async Task<Dictionary<string, PrescriptionReadResult>> ExecuteParsersParallelAsync(
        string ocrText,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation("? Executing parsers in PARALLEL...");
        
        var tasks = new List<Task<(string Provider, PrescriptionReadResult? Result, Exception? Error)>>
        {
            ExecuteParserAsync("OpenAI", () => _openAIAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)),
            ExecuteParserAsync("DeepSeek", () => _deepSeekAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)),
            ExecuteParserAsync("Claude", () => _claudeAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken))
        };

        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r.Result != null && r.Result.Success && r.Result.Medications.Any())
            .ToDictionary(r => r.Provider, r => r.Result!);
    }

    /// <summary>
    /// Execute parsers sequentially with early termination on success
    /// </summary>
    private async Task<Dictionary<string, PrescriptionReadResult>> ExecuteParsersSequentialAsync(
        string ocrText,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation("?? Executing parsers SEQUENTIALLY...");
        
        var results = new Dictionary<string, PrescriptionReadResult>();
        var parsers = new[]
        {
            ("OpenAI", new Func<Task<PrescriptionReadResult>>(() => _openAIAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken))),
            ("DeepSeek", new Func<Task<PrescriptionReadResult>>(() => _deepSeekAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken))),
            ("Claude", new Func<Task<PrescriptionReadResult>>(() => _claudeAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)))
        };

        foreach (var (provider, parseFunc) in parsers)
        {
            var (_, result, error) = await ExecuteParserAsync(provider, parseFunc);
            
            if (result != null && result.Success && result.Medications.Any())
            {
                results[provider] = result;
                
                // Early termination if we have a high-confidence result
                if (result.ConfidenceScore >= 0.9 && result.Medications.Count >= 1)
                {
                    _logger?.LogInformation($"? High-confidence result from {provider}, stopping sequential execution");
                    break;
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Execute a single parser with error handling
    /// </summary>
    private async Task<(string Provider, PrescriptionReadResult? Result, Exception? Error)> ExecuteParserAsync(
        string provider,
        Func<Task<PrescriptionReadResult>> parseFunc)
    {
        try
        {
            _logger?.LogInformation($"   Executing {provider} parser...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            var result = await parseFunc();
            
            sw.Stop();
            var status = result.Success ? "? Success" : "? Failed";
            _logger?.LogInformation($"   {provider}: {status} ({sw.ElapsedMilliseconds}ms)");
            
            if (result.Success && result.Medications.Any())
            {
                _logger?.LogInformation($"      Medications: {result.Medications.Count}, Confidence: {result.ConfidenceScore:P0}");
            }
            
            return (provider, result, null);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning($"   {provider}: ?? Exception - {ex.Message}");
            return (provider, null, ex);
        }
    }

    /// <summary>
    /// Store processing results in database
    /// </summary>
    private async Task StoreResultAsync(
        int prescriptionId,
        string ocrText,
        Dictionary<string, PrescriptionReadResult> parserResults,
        PrescriptionReadResult mergedResult,
        double confidence,
        PrescriptionProcessingResult result)
    {
        try
        {
            if (_unitOfWork == null)
            {
                _logger?.LogWarning("?? Cannot store results - UnitOfWork not available");
                return;
            }

            _logger?.LogInformation("?? Storing results in database...");

            var ocrResult = new PrescriptionOCRResult
            {
                PrescriptionId = prescriptionId,
                OCRText = ocrText,
                OCRTextHash = ComputeHash(ocrText),
                SelectedProvider = string.Join(" + ", parserResults.Keys),
                SelectedResponse = JsonSerializer.Serialize(mergedResult),
                ComparisonScore = confidence,
                MedicationCount = mergedResult.Medications.Count,
                DoctorName = mergedResult.Doctor?.Name,
                PatientName = mergedResult.Patient?.Name,
                PrescriptionDate = mergedResult.PrescriptionDate,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = result.ProcessingTime,
                ProcessingAttempts = parserResults.Count
            };

            // Store individual parser responses
            if (parserResults.ContainsKey("OpenAI"))
            {
                ocrResult.OpenAIResponse = JsonSerializer.Serialize(parserResults["OpenAI"]);
            }
            if (parserResults.ContainsKey("Claude"))
            {
                ocrResult.ClaudeResponse = JsonSerializer.Serialize(parserResults["Claude"]);
            }

            var repo = _unitOfWork.Repository<PrescriptionOCRResult>();
            await repo.AddAsync(ocrResult);
            await _unitOfWork.SaveChangesAsync();

            result.DatabaseId = ocrResult.Id;
            
            _logger?.LogInformation($"? Stored in database - ID: {ocrResult.Id}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "? Failed to store results in database");
            // Don't fail the entire operation if storage fails
        }
    }

    /// <summary>
    /// Fail fast with error message
    /// </summary>
    private PrescriptionProcessingResult FailFast(string errorMessage, PrescriptionProcessingResult result)
    {
        _logger?.LogError($"? {errorMessage}");
        
        result.Success = false;
        result.ErrorMessage = errorMessage;
        result.EndTime = DateTime.UtcNow;
        result.ProcessingTime = result.EndTime.Value - result.StartTime;
        
        return result;
    }

    /// <summary>
    /// Compute SHA256 hash of text
    /// </summary>
    private string ComputeHash(string text)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Execution mode for parser orchestration
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Execute all parsers in parallel for maximum speed
    /// </summary>
    Parallel,
    
    /// <summary>
    /// Execute parsers sequentially with early termination on success
    /// Useful for cost optimization
    /// </summary>
    Sequential
}
