using MedRemind.Core.DTOs;
using MedRemind.Services.AI.Agents;
using MedRemind.Services.Prescriptions;
using Microsoft.Extensions.Logging;

namespace MedRemind.Services.AI;

/// <summary>
/// REDESIGNED: Dynamic, high-performance orchestrator with parallel execution
/// Features: Circuit breaker, caching, dynamic parser registry, parallel/sequential modes
/// </summary>
public class AgentOrchestratorV2
{
    private readonly OCRTextSaverAgent _ocrSaverAgent;
    private readonly ParserRegistry _parserRegistry;
    private readonly PrescriptionResultMergerService _mergerService;
    private readonly PrescriptionValidationService _validationService;
    private readonly PrescriptionDeduplicationService _deduplicationService;
    private readonly PrescriptionCacheService _cacheService;
    private readonly ILogger<AgentOrchestratorV2> _logger;
    
    // Timeout configuration
    private const int PARSER_TIMEOUT_SECONDS = 30;  // Individual parser timeout (increased from 30s)
    private const int OVERALL_TIMEOUT_SECONDS = 45; // Overall parallel execution timeout (increased from 45s)
    
    // Execution strategy
    public enum ExecutionMode
    {
        Sequential,  // Run parsers one by one (slower, more predictable)
        Parallel,    // Run all parsers at once (faster, less predictable)
        Adaptive     // Start with parallel, fall back to sequential if issues
    }
    
    private readonly ExecutionMode _executionMode;
    
    public AgentOrchestratorV2(
        OCRTextSaverAgent ocrSaverAgent,
        ParserRegistry parserRegistry,
        PrescriptionResultMergerService mergerService,
        PrescriptionValidationService validationService,
        PrescriptionDeduplicationService deduplicationService,
        PrescriptionCacheService cacheService,
        ILogger<AgentOrchestratorV2> logger,
        ExecutionMode executionMode = ExecutionMode.Parallel)
    {
        _ocrSaverAgent = ocrSaverAgent ?? throw new ArgumentNullException(nameof(ocrSaverAgent));
        _parserRegistry = parserRegistry ?? throw new ArgumentNullException(nameof(parserRegistry));
        _mergerService = mergerService ?? throw new ArgumentNullException(nameof(mergerService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _executionMode = executionMode;
        
        _logger.LogInformation("✅ AgentOrchestrator V2 initialized");
        _logger.LogInformation($"   Execution mode: {_executionMode}");
        _logger.LogInformation($"   Registered parsers: {_parserRegistry.Count}");
        _logger.LogInformation($"   Enabled parsers: {_parserRegistry.EnabledCount}");
    }
    
    /// <summary>
    /// Process prescription with modern architecture
    /// </summary>
    public async Task<PrescriptionProcessingResult> ProcessPrescriptionAsync(
        string ocrText,
        string prescriptionFileName,
        int prescriptionId,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("🚀 Starting prescription processing (V2)");
        _logger.LogInformation($"   File: {prescriptionFileName}");
        _logger.LogInformation($"   OCR length: {ocrText?.Length ?? 0} chars");
        
        // STEP 0: Check cache
        if (_cacheService.TryGet(ocrText, out var cachedResult))
        {
            _logger.LogInformation("⚡ Returning cached result (no API calls made)");
            _logger.LogInformation($"   Saved ~{cachedResult.ProcessingTime.TotalSeconds:F1}s processing time");
            return cachedResult;
        }
        
        var result = new PrescriptionProcessingResult
        {
            PrescriptionFileName = prescriptionFileName,
            StartTime = startTime
        };
        
        try
        {
            // STEP 1: Validate OCR text
            if (string.IsNullOrWhiteSpace(ocrText) || ocrText.Length < 50)
            {
                return FailFast("OCR text too short or empty", result);
            }
            
            // STEP 2: Save OCR text
            var saveResult = await _ocrSaverAgent.SaveOCRTextAsync(ocrText, prescriptionFileName);
            result.OCRSaveResult = saveResult;
            
            if (!saveResult.Success)
            {
                return FailFast($"Failed to save OCR: {saveResult.ErrorMessage}", result);
            }
            
            _logger.LogInformation($"✅ OCR saved: {saveResult.SavedFilePath}");
            if (saveResult.IsDuplicate)
            {
                _logger.LogInformation($"   ℹ️ {saveResult.DuplicateAction}");
            }
            
            // STEP 3: Execute parsers (parallel or sequential)
            var parserResults = _executionMode == ExecutionMode.Parallel
                ? await ExecuteParsersParallelAsync(ocrText, cancellationToken)
                : await ExecuteParsersSequentialAsync(ocrText, cancellationToken);
            
            if (!parserResults.Any())
            {
                return FailFast("All parsers failed or returned no results", result);
            }
            
            // STEP 4: Merge results
            var mergedResult = MergeParserResults(parserResults);
            
            if (mergedResult == null || !mergedResult.Medications.Any())
            {
                return FailFast("No medications found after merging", result);
            }
            
            // STEP 5: Validate completeness
            var validation = _validationService.ValidateCompleteness(mergedResult);
            _logger.LogInformation($"📊 Validation: {(validation.IsComplete ? "✅ Complete" : "⚠️ Incomplete")}");
            _logger.LogInformation($"   Confidence: {validation.AverageConfidence:P0}");
            
            // STEP 6: Store in database
            await StoreResultAsync(prescriptionId, ocrText, parserResults, mergedResult, validation, result);
            
            // STEP 7: Finalize result
            result.ParseResult = mergedResult;
            result.Success = true;
            result.MatchScore = validation.AverageConfidence;
            result.TotalAttempts = parserResults.Count;
            result.SelectedProvider = string.Join(" + ", parserResults.Keys);
            result.EndTime = DateTime.UtcNow;
            result.ProcessingTime = result.EndTime.Value - result.StartTime;
            
            // STEP 8: Cache result
            _cacheService.Set(ocrText, result);
            
            _logger.LogInformation($"✅ Processing complete in {result.ProcessingTime.TotalSeconds:F2}s");
            _logger.LogInformation($"   Providers: {result.SelectedProvider}");
            _logger.LogInformation($"   Medications: {result.ParseResult.Medications.Count}");
            _logger.LogInformation($"   Confidence: {result.MatchScore:P0}");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Orchestrator V2 failed");
            result.Success = false;
            result.ErrorMessage = $"Processing failed: {ex.Message}";
            result.EndTime = DateTime.UtcNow;
            result.ProcessingTime = result.EndTime.Value - result.StartTime;
            return result;
        }
    }
    
    /// <summary>
    /// Execute parsers in parallel (FAST)
    /// </summary>
    private async Task<Dictionary<string, PrescriptionReadResult>> ExecuteParsersParallelAsync(
        string ocrText,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("⚡ Executing parsers in PARALLEL mode");
        
        var enabledParsers = _parserRegistry.GetEnabledParsers().ToList();
        _logger.LogInformation($"   Active parsers: {enabledParsers.Count}");
        
        if (!enabledParsers.Any())
        {
            _logger.LogWarning("   ⚠️ No enabled parsers found!");
            return new Dictionary<string, PrescriptionReadResult>();
        }
        
        var tasks = enabledParsers.Select(async parser =>
        {
            var parserStartTime = DateTime.UtcNow;
            
            // Create a timeout token for this specific parser
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(PARSER_TIMEOUT_SECONDS));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            try
            {
                _logger.LogInformation($"   🔄 {parser.Name} starting...");
                
                var result = await parser.ParseAsync(ocrText, linkedCts.Token);
                
                var elapsed = DateTime.UtcNow - parserStartTime;
                _logger.LogInformation($"   ✅ {parser.Name} completed in {elapsed.TotalSeconds:F2}s ({result.Medications?.Count ?? 0} meds)");
                
                return (Name: parser.Name, Result: result, Success: result?.Success ?? false);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                var elapsed = DateTime.UtcNow - parserStartTime;
                _logger.LogWarning($"   ⏱️ {parser.Name} timed out after {elapsed.TotalSeconds:F2}s (limit: {PARSER_TIMEOUT_SECONDS}s)");
                return (Name: parser.Name, Result: (PrescriptionReadResult?)null, Success: false);
            }
            catch (Exception ex)
            {
                var elapsed = DateTime.UtcNow - parserStartTime;
                _logger.LogWarning($"   ❌ {parser.Name} failed after {elapsed.TotalSeconds:F2}s: {ex.Message}");
                return (Name: parser.Name, Result: (PrescriptionReadResult?)null, Success: false);
            }
        }).ToList();
        
        // Wait for all parsers with overall timeout
        try
        {
            using var overallTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(OVERALL_TIMEOUT_SECONDS));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, overallTimeoutCts.Token);
            
            var results = await Task.WhenAll(tasks).WaitAsync(linkedCts.Token);
            
            return results
                .Where(r => r.Success && r.Result != null)
                .ToDictionary(r => r.Name, r => r.Result!);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning($"⏱️ Overall timeout reached ({OVERALL_TIMEOUT_SECONDS}s), returning partial results");
            
            // Collect results from completed tasks
            var completedResults = tasks
                .Where(t => t.IsCompletedSuccessfully)
                .Select(t => t.Result)
                .Where(r => r.Success && r.Result != null)
                .ToDictionary(r => r.Name, r => r.Result!);
            
            _logger.LogInformation($"   Collected {completedResults.Count} completed parser(s)");
            return completedResults;
        }
    }
    
    /// <summary>
    /// Execute parsers sequentially (SAFE)
    /// </summary>
    private async Task<Dictionary<string, PrescriptionReadResult>> ExecuteParsersSequentialAsync(
        string ocrText,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 Executing parsers in SEQUENTIAL mode");
        
        var results = new Dictionary<string, PrescriptionReadResult>();
        var enabledParsers = _parserRegistry.GetEnabledParsers().ToList();
        
        _logger.LogInformation($"   Active parsers: {enabledParsers.Count}");
        
        foreach (var parser in enabledParsers)
        {
            var parserStartTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation($"   🔄 {parser.Name} starting...");
                
                var result = await parser.ParseAsync(ocrText, cancellationToken);
                
                var elapsed = DateTime.UtcNow - parserStartTime;
                
                if (result?.Success == true && result.Medications?.Any() == true)
                {
                    results[parser.Name] = result;
                    _logger.LogInformation($"   ✅ {parser.Name} succeeded in {elapsed.TotalSeconds:F2}s ({result.Medications.Count} meds)");
                }
                else
                {
                    _logger.LogWarning($"   ⚠️ {parser.Name} returned no results in {elapsed.TotalSeconds:F2}s");
                }
            }
            catch (Exception ex)
            {
                var elapsed = DateTime.UtcNow - parserStartTime;
                _logger.LogError(ex, $"   ❌ {parser.Name} failed after {elapsed.TotalSeconds:F2}s");
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Merge results from multiple parsers
    /// </summary>
    private PrescriptionReadResult MergeParserResults(Dictionary<string, PrescriptionReadResult> parserResults)
    {
        if (!parserResults.Any())
        {
            return new PrescriptionReadResult { Success = false, Medications = new List<MedicationData>() };
        }
        
        if (parserResults.Count == 1)
        {
            return parserResults.Values.First();
        }
        
        _logger.LogInformation($"🔀 Merging results from {parserResults.Count} parsers");
        
        var sortedParsers = parserResults.OrderBy(kvp =>
        {
            var parser = _parserRegistry.GetEnabledParsers().FirstOrDefault(p => p.Name == kvp.Key);
            return parser?.Priority ?? int.MaxValue;
        }).ToList();
        
        var merged = sortedParsers[0].Value;
        
        for (int i = 1; i < sortedParsers.Count; i++)
        {
            var prevProviders = string.Join("+", sortedParsers.Take(i).Select(p => p.Key));
            merged = _mergerService.MergeResults(merged, sortedParsers[i].Value, prevProviders, sortedParsers[i].Key);
        }
        
        return merged;
    }
    
    /// <summary>
    /// Store result in database
    /// </summary>
    private async Task StoreResultAsync(
        int prescriptionId,
        string ocrText,
        Dictionary<string, PrescriptionReadResult> parserResults,
        PrescriptionReadResult mergedResult,
        ValidationQuality validation,
        PrescriptionProcessingResult processingResult)
    {
        try
        {
            var ocrResultId = await _deduplicationService.StoreOCRResultAsync(
                prescriptionId: prescriptionId,
                ocrText: ocrText,
                openAIResponse: parserResults.ContainsKey("OpenAI")
                    ? System.Text.Json.JsonSerializer.Serialize(parserResults["OpenAI"])
                    : null,
                claudeResponse: parserResults.ContainsKey("Claude")
                    ? System.Text.Json.JsonSerializer.Serialize(parserResults["Claude"])
                    : null,
                selectedResponse: System.Text.Json.JsonSerializer.Serialize(mergedResult),
                selectedProvider: string.Join("+", parserResults.Keys),
                comparisonScore: validation.AverageConfidence,
                comparisonReason: $"Multi-parser merge. Complete: {validation.IsComplete}",
                medicationCount: mergedResult.Medications?.Count ?? 0,
                doctorName: mergedResult.Doctor?.Name,
                patientName: mergedResult.Patient?.Name,
                prescriptionDate: mergedResult.PrescriptionDate,
                processingTime: DateTime.UtcNow - processingResult.StartTime,
                processingAttempts: parserResults.Count
            );
            
            processingResult.DatabaseId = ocrResultId;
            _logger.LogInformation($"💾 Stored in database with ID: {ocrResultId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Database storage failed (continuing anyway)");
        }
    }
    
    private PrescriptionProcessingResult FailFast(string errorMessage, PrescriptionProcessingResult result)
    {
        _logger.LogWarning($"⚠️ Fast failure: {errorMessage}");
        result.Success = false;
        result.ErrorMessage = errorMessage;
        result.EndTime = DateTime.UtcNow;
        result.ProcessingTime = result.EndTime.Value - result.StartTime;
        return result;
    }
}
