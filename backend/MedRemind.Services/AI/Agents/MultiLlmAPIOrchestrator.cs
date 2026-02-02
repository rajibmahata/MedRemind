using MedRemind.Core.Configuration;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Services.AI.Python;
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
    private readonly LlmOrchestratorConfiguration _config;
    private readonly IFileStorageService? _fileStorageService;
    private readonly PrescriptionValidationAgent? _validationAgent;
    
    // Optional services for advanced features
    private readonly PrescriptionResultMergerService? _mergerService;
    private readonly PrescriptionValidationService? _validationService;
    private readonly PrescriptionCacheService? _cacheService;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly ILogger<MultiLlmAPIOrchestrator>? _logger;

    private readonly PythonMiddlewareClient _pythonClient;
    private readonly MedRemind.Core.Configuration.PythonMiddlewareConfiguration? _pythonConfig;

    // For saving LLM responses
    private string? _currentPrescriptionFileName;

    public MultiLlmAPIOrchestrator(
        OpenAIPrescriptionParserAgent openAIAgent,
        DeepSeekPrescriptionParserAgent deepSeekAgent,
        ClaudePrescriptionParserAgent claudeAgent,
        LlmOrchestratorConfiguration config,
        PythonMiddlewareClient pythonMiddlewareClient,
        IFileStorageService? fileStorageService = null,
        ExecutionMode executionMode = ExecutionMode.Parallel,
        PrescriptionResultMergerService? mergerService = null,
        PrescriptionValidationService? validationService = null,
        PrescriptionValidationAgent? multiAgentValidationAgent = null,
        MedRemind.Core.Configuration.PythonMiddlewareConfiguration? pythonConfig = null,
        PrescriptionCacheService? cacheService = null,
        IUnitOfWork? unitOfWork = null,
        ILogger<MultiLlmAPIOrchestrator>? logger = null)
    {
        _openAIAgent = openAIAgent ?? throw new ArgumentNullException(nameof(openAIAgent));
        _deepSeekAgent = deepSeekAgent ?? throw new ArgumentNullException(nameof(deepSeekAgent));
        _claudeAgent = claudeAgent ?? throw new ArgumentNullException(nameof(claudeAgent));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _fileStorageService = fileStorageService;
        _validationAgent = multiAgentValidationAgent;
        _executionMode = executionMode;
        _mergerService = mergerService;
        _validationService = validationService;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _pythonClient = pythonMiddlewareClient ?? throw new ArgumentNullException(nameof(pythonMiddlewareClient));
        _pythonConfig = pythonConfig;

        var mode = executionMode == ExecutionMode.Parallel ? "Parallel" : "Sequential";
        _logger?.LogInformation($"?? MultiLlmAPIOrchestrator initialized - Mode: {mode}");
        _logger?.LogInformation($"   OpenAI: {(config.OpenAI.Enabled ? "Enabled" : "Disabled")} (Priority: {config.OpenAI.Priority})");
        _logger?.LogInformation($"   DeepSeek: {(config.DeepSeek.Enabled ? "Enabled" : "Disabled")} (Priority: {config.DeepSeek.Priority})");
        _logger?.LogInformation($"   Claude: {(config.Claude.Enabled ? "Enabled" : "Disabled")} (Priority: {config.Claude.Priority})");
        _logger?.LogInformation($"   Python Middleware: {(pythonConfig?.Enabled == true ? "Enabled" : "Disabled")}");
        _logger?.LogInformation($"   Advanced Features: {(HasAdvancedFeatures() ? "Enabled" : "Disabled")}");
        _logger?.LogInformation($"   LLM Response Saving: {(_fileStorageService != null ? "Enabled" : "Disabled")}");
        _logger?.LogInformation($"   Multi-Agent Validation: {(_validationAgent != null ? "Enabled" : "Disabled")}");
    }

    private bool HasAdvancedFeatures() => _mergerService != null && _validationService != null;

    /// <summary>
    /// Get Python Middleware configuration (if available)
    /// </summary>
    private MedRemind.Core.Configuration.PythonMiddlewareConfiguration? GetPythonMiddlewareConfig()
    {
        return _pythonConfig;
    }

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
            // Store prescription file name for LLM response saving
            _currentPrescriptionFileName = prescriptionFileName;

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

            // STEP 3: Try Python Middleware (CrewAI) - Primary processing method
            PrescriptionReadResult? crewAiResult = null;
            var pythonConfig = GetPythonMiddlewareConfig();
            
            if (pythonConfig != null && pythonConfig.Enabled)
            {
                _logger?.LogInformation("?? Processing with Python Middleware (CrewAI)...");
                try
                {
                    crewAiResult = await _pythonClient.ParsePrescriptionAsync(ocrText, prescriptionFileName, true, cancellationToken);
                    
                    if (crewAiResult != null && crewAiResult.Success && crewAiResult.Medications.Any())
                    {
                        _logger?.LogInformation("? Python Middleware returned successful result");
                        _logger?.LogInformation($"   Medications: {crewAiResult.Medications.Count}");
                        _logger?.LogInformation($"   Confidence: {crewAiResult.ConfidenceScore:P0}");
                        _logger?.LogInformation($"   Patient: {crewAiResult.Patient?.Name ?? "N/A"}");
                        _logger?.LogInformation($"   Doctor: {crewAiResult.Doctor?.Name ?? "N/A"}");
                        _logger?.LogInformation($"   Date: {crewAiResult.PrescriptionDate?.ToString("yyyy-MM-dd") ?? "N/A"}");
                        
                        // Use CrewAI result (all validation and multi-agent processing done in Python)
                        result.ParseResult = crewAiResult;
                        result.PrescriptionResult = crewAiResult;
                        result.Success = true;
                        result.MatchScore = crewAiResult.ConfidenceScore;
                        result.TotalAttempts = 1;
                        result.ProcessingAttempts = 1;
                        result.SelectedProvider = "Python Middleware (CrewAI)";
                    }
                    else
                    {
                        _logger?.LogError("? Python Middleware did not return successful result");
                        return FailFast("Python Middleware processing failed: No medications found or processing unsuccessful", result);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "? Python Middleware processing failed");
                    return FailFast($"Python Middleware error: {ex.Message}", result);
                }
            }
            else
            {
                _logger?.LogError("? Python Middleware is disabled - no processing method available");
                return FailFast("Python Middleware is disabled. Please enable it in configuration to process prescriptions.", result);
            }

            // STEP 4: Store in database (if unit of work available)
            if (_unitOfWork != null && result.ParseResult != null)
            {
                await StoreResultAsync(prescriptionId, ocrText, result.ParseResult, result);
            }
            
            // STEP 5: Finalize result
            result.EndTime = DateTime.UtcNow;
            result.ProcessingTime = result.EndTime.Value - result.StartTime;
            
            // STEP 6: Cache result (if cache service available)
            if (_cacheService != null)
            {
                _cacheService.Set(ocrText, result);
                _logger?.LogInformation("?? Result cached for future requests");
            }
            
            _logger?.LogInformation($"? Processing complete in {result.ProcessingTime.TotalSeconds:F2}s");
            _logger?.LogInformation($"   Provider: {result.SelectedProvider}");
            _logger?.LogInformation($"   Medications: {result.ParseResult?.Medications.Count ?? 0}");
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

    // ===================================================================================================
    // LEGACY METHODS - Kept for backward compatibility but not used when Python Middleware is enabled
    // ===================================================================================================

    /// <summary>
    /// Execute all parsers in parallel (LEGACY - Not used with Python Middleware)
    /// </summary>
    private async Task<Dictionary<string, PrescriptionReadResult>> ExecuteParsersParallelAsync(
        string ocrText,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation("? Executing parsers in PARALLEL...");
        
        var tasks = new List<Task<(string Provider, PrescriptionReadResult? Result, Exception? Error)>>();
        
        // Check Enabled flag before adding to tasks
        if (_config.OpenAI.Enabled)
        {
            _logger?.LogInformation("   ? OpenAI enabled - adding to execution");
            tasks.Add(ExecuteParserAsync("OpenAI", () => _openAIAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
        }
        else
        {
            _logger?.LogInformation("   ?? OpenAI disabled - skipping");
        }
        
        if (_config.DeepSeek.Enabled)
        {
            _logger?.LogInformation("   ? DeepSeek enabled - adding to execution");
            tasks.Add(ExecuteParserAsync("DeepSeek", () => _deepSeekAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
        }
        else
        {
            _logger?.LogInformation("   ?? DeepSeek disabled - skipping");
        }
        
        if (_config.Claude.Enabled)
        {
            _logger?.LogInformation("   ? Claude enabled - adding to execution");
            tasks.Add(ExecuteParserAsync("Claude", () => _claudeAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
        }
        else
        {
            _logger?.LogInformation("   ?? Claude disabled - skipping");
        }

        if (!tasks.Any())
        {
            _logger?.LogWarning("?? No parsers are enabled! Returning empty results.");
            return new Dictionary<string, PrescriptionReadResult>();
        }

        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r.Result != null && r.Result.Success && r.Result.Medications.Any())
            .ToDictionary(r => r.Provider, r => r.Result!);
    }


    /// <summary>
    /// Execute parsers sequentially with early termination on success (LEGACY - Not used with Python Middleware)
    /// </summary>
    private async Task<Dictionary<string, PrescriptionReadResult>> ExecuteParsersSequentialAsync(
        string ocrText,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation("?? Executing parsers SEQUENTIALLY...");
        
        var results = new Dictionary<string, PrescriptionReadResult>();
        
        // Build list of enabled parsers only, sorted by priority
        var parsers = new List<(string Name, int Priority, Func<Task<PrescriptionReadResult>> Parser)>();
        
        if (_config.OpenAI.Enabled)
        {
            parsers.Add(("OpenAI", _config.OpenAI.Priority, () => _openAIAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
        }
        
        if (_config.DeepSeek.Enabled)
        {
            parsers.Add(("DeepSeek", _config.DeepSeek.Priority, () => _deepSeekAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
        }
        
        if (_config.Claude.Enabled)
        {
            parsers.Add(("Claude", _config.Claude.Priority, () => _claudeAgent.ParsePrescriptionTextAsync(ocrText, cancellationToken)));
        }

        if (!parsers.Any())
        {
            _logger?.LogWarning("?? No parsers are enabled! Returning empty results.");
            return results;
        }

        // Sort by priority (lower number = higher priority)
        var sortedParsers = parsers.OrderBy(p => p.Priority).ToList();
        
        _logger?.LogInformation($"   Execution order (by priority):");
        foreach (var parser in sortedParsers)
        {
            _logger?.LogInformation($"   {parser.Priority}. {parser.Name}");
        }

        foreach (var (provider, priority, parseFunc) in sortedParsers)
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
    /// Execute a single parser with error handling (LEGACY - Not used with Python Middleware)
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

            // Save LLM response to file
            if (_fileStorageService != null && !string.IsNullOrEmpty(_currentPrescriptionFileName))
            {
                try
                {
                    var jsonResponse = JsonSerializer.Serialize(result, new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    });
                    
                    await _fileStorageService.SaveLlmResponseAsync(provider, jsonResponse, _currentPrescriptionFileName);
                    _logger?.LogInformation($"      ?? Saved {provider} response to file");
                }
                catch (Exception saveEx)
                {
                    _logger?.LogWarning($"      ?? Failed to save {provider} response: {saveEx.Message}");
                }
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
        PrescriptionReadResult parseResult,
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
                SelectedProvider = result.SelectedProvider,
                SelectedResponse = JsonSerializer.Serialize(parseResult),
                ComparisonScore = parseResult.ConfidenceScore,
                MedicationCount = parseResult.Medications.Count,
                DoctorName = parseResult.Doctor?.Name,
                PatientName = parseResult.Patient?.Name,
                PrescriptionDate = parseResult.PrescriptionDate,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = result.ProcessingTime,
                ProcessingAttempts = 1,
                OpenAIResponse = JsonSerializer.Serialize(parseResult) // Store as primary response
            };

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
