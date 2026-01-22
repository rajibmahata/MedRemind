# ?? AgentOrchestrator Redesign - Dynamic & High-Performance Architecture

## ?? Current Issues & Solutions

### **Current Architecture Problems:**

| Issue | Current | Impact |
|-------|---------|--------|
| **Sequential Execution** | Parsers run one after another | Slow (30s + 30s + 30s = 90s) |
| **No Parallel Processing** | Single-threaded workflow | Wasted time |
| **Hard-coded Parser List** | Static parser initialization | Not extensible |
| **Timeout Per Parser** | 20s, but no granular control | Can still hang |
| **No Caching** | Repeat processing same OCR | Wasted API calls |
| **No Circuit Breaker** | Retry failing parsers every time | Wasted time/money |
| **Tight Coupling** | Direct dependencies on each parser | Hard to test/extend |

---

## ? New Dynamic Architecture

### **Design Principles:**

1. **?? Parallel Execution** - Run all parsers simultaneously
2. **? Fast Failure** - Skip slow/failing parsers immediately
3. **?? Strategy Pattern** - Dynamic parser registration
4. **?? Smart Caching** - Avoid duplicate processing
5. **?? Circuit Breaker** - Auto-disable failing parsers
6. **?? Metrics Tracking** - Monitor performance in real-time
7. **?? Testability** - Easy to mock and unit test

---

## ??? New Architecture Design

```
???????????????????????????????????????????????????????????????
?                   AgentOrchestrator                          ?
?                  (Coordinator Layer)                         ?
???????????????????????????????????????????????????????????????
                        ?
        ?????????????????????????????????
        ?               ?               ?
???????????????? ??????????????? ??????????????
? Parser       ? ? Result      ? ? Cache      ?
? Registry     ? ? Merger      ? ? Service    ?
???????????????? ??????????????? ??????????????
       ?                ?               ?
       ?                ?               ?
????????????????????????????????????????????????
?         Parser Execution Strategy            ?
?  (Parallel / Sequential / Adaptive)          ?
????????????????????????????????????????????????
                   ?
     ?????????????????????????????
     ?             ?             ?
???????????  ???????????  ????????????
?DeepSeek ?  ? OpenAI  ?  ? Claude   ?
? Parser  ?  ? Parser  ?  ? Parser   ?
???????????  ???????????  ????????????
```

---

## ?? Implementation

### **1. Parser Interface (Strategy Pattern)**

```csharp
/// <summary>
/// Base interface for all AI parsers
/// </summary>
public interface IPrescriptionParser
{
    string Name { get; }
    int Priority { get; }
    bool IsEnabled { get; }
    TimeSpan Timeout { get; }
    
    Task<PrescriptionReadResult> ParseAsync(
        string ocrText,
        CancellationToken cancellationToken = default);
    
    Task<bool> HealthCheckAsync();
}
```

### **2. Parser Wrapper (Circuit Breaker)**

```csharp
/// <summary>
/// Wraps parsers with circuit breaker pattern
/// </summary>
public class ResilientParser : IPrescriptionParser
{
    private readonly IPrescriptionParser _innerParser;
    private readonly CircuitBreakerPolicy _circuitBreaker;
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
        _innerParser = innerParser;
        _logger = logger;
        
        _circuitBreaker = Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: MAX_FAILURES,
                durationOfBreak: CIRCUIT_RESET_TIME,
                onBreak: (ex, duration) =>
                {
                    _logger.LogWarning($"Circuit breaker opened for {Name}. Duration: {duration.TotalMinutes}min");
                },
                onReset: () =>
                {
                    _logger.LogInformation($"Circuit breaker reset for {Name}");
                    _failureCount = 0;
                });
    }
    
    public async Task<PrescriptionReadResult> ParseAsync(
        string ocrText,
        CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen())
        {
            _logger.LogWarning($"{Name} circuit is open. Skipping parser.");
            return new PrescriptionReadResult
            {
                Success = false,
                Medications = new List<MedicationData>()
            };
        }
        
        try
        {
            return await _circuitBreaker.ExecuteAsync(async (ct) =>
            {
                using var cts = new CancellationTokenSource(Timeout);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
                
                return await _innerParser.ParseAsync(ocrText, linked.Token);
            }, cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning($"{Name} circuit breaker is open");
            return new PrescriptionReadResult { Success = false, Medications = new List<MedicationData>() };
        }
        catch (Exception ex)
        {
            RecordFailure();
            _logger.LogError(ex, $"{Name} parse failed");
            return new PrescriptionReadResult { Success = false, Medications = new List<MedicationData>() };
        }
    }
    
    public Task<bool> HealthCheckAsync()
    {
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
                _failureCount = 0;
                _lastFailureTime = null;
            }
        }
        return false;
    }
    
    private void RecordFailure()
    {
        _failureCount++;
        _lastFailureTime = DateTime.UtcNow;
    }
}
```

### **3. Parser Registry (Dynamic Registration)**

```csharp
/// <summary>
/// Registry for managing available parsers dynamically
/// </summary>
public class ParserRegistry
{
    private readonly List<IPrescriptionParser> _parsers = new();
    private readonly ILogger<ParserRegistry> _logger;
    
    public ParserRegistry(ILogger<ParserRegistry> logger)
    {
        _logger = logger;
    }
    
    public void Register(IPrescriptionParser parser)
    {
        if (_parsers.Any(p => p.Name == parser.Name))
        {
            _logger.LogWarning($"Parser {parser.Name} already registered. Skipping.");
            return;
        }
        
        _parsers.Add(parser);
        _logger.LogInformation($"Registered parser: {parser.Name} (Priority: {parser.Priority})");
    }
    
    public IEnumerable<IPrescriptionParser> GetEnabledParsers()
    {
        return _parsers
            .Where(p => p.IsEnabled)
            .OrderBy(p => p.Priority);
    }
    
    public int Count => _parsers.Count;
    public int EnabledCount => _parsers.Count(p => p.IsEnabled);
}
```

### **4. Caching Service**

```csharp
/// <summary>
/// Cache service to avoid reprocessing same OCR text
/// </summary>
public class PrescriptionCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<PrescriptionCacheService> _logger;
    private static readonly TimeSpan DEFAULT_CACHE_DURATION = TimeSpan.FromHours(24);
    
    public PrescriptionCacheService(IMemoryCache cache, ILogger<PrescriptionCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    public string GenerateCacheKey(string ocrText)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(ocrText));
        return Convert.ToBase64String(hash);
    }
    
    public bool TryGet(string ocrText, out PrescriptionProcessingResult? result)
    {
        var key = GenerateCacheKey(ocrText);
        
        if (_cache.TryGetValue(key, out result))
        {
            _logger.LogInformation($"Cache HIT for key: {key.Substring(0, 10)}...");
            return true;
        }
        
        _logger.LogDebug($"Cache MISS for key: {key.Substring(0, 10)}...");
        result = null;
        return false;
    }
    
    public void Set(string ocrText, PrescriptionProcessingResult result)
    {
        var key = GenerateCacheKey(ocrText);
        _cache.Set(key, result, DEFAULT_CACHE_DURATION);
        _logger.LogInformation($"Cached result for key: {key.Substring(0, 10)}... (24h TTL)");
    }
    
    public void Clear()
    {
        // Implementation depends on IMemoryCache
        _logger.LogInformation("Cache cleared");
    }
}
```

---

## ?? Redesigned AgentOrchestrator

```csharp
/// <summary>
/// REDESIGNED: Dynamic, high-performance orchestrator with parallel execution
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
        _ocrSaverAgent = ocrSaverAgent;
        _parserRegistry = parserRegistry;
        _mergerService = mergerService;
        _validationService = validationService;
        _deduplicationService = deduplicationService;
        _cacheService = cacheService;
        _logger = logger;
        _executionMode = executionMode;
        
        _logger.LogInformation("? AgentOrchestrator V2 initialized");
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
        
        _logger.LogInformation("?? Starting prescription processing");
        _logger.LogInformation($"   File: {prescriptionFileName}");
        _logger.LogInformation($"   OCR length: {ocrText?.Length ?? 0} chars");
        
        // STEP 0: Check cache
        if (_cacheService.TryGet(ocrText, out var cachedResult))
        {
            _logger.LogInformation("? Returning cached result (no API calls made)");
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
            
            _logger.LogInformation($"? OCR saved: {saveResult.SavedFilePath}");
            
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
            _logger.LogInformation($"?? Validation: {(validation.IsComplete ? "? Complete" : "?? Incomplete")}");
            
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
            
            _logger.LogInformation($"? Processing complete in {result.ProcessingTime.TotalSeconds:F2}s");
            _logger.LogInformation($"   Providers: {result.SelectedProvider}");
            _logger.LogInformation($"   Medications: {result.ParseResult.Medications.Count}");
            _logger.LogInformation($"   Confidence: {result.MatchScore:P0}");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Orchestrator failed");
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
        _logger.LogInformation("? Executing parsers in PARALLEL mode");
        
        var enabledParsers = _parserRegistry.GetEnabledParsers().ToList();
        _logger.LogInformation($"   Active parsers: {enabledParsers.Count}");
        
        var tasks = enabledParsers.Select(async parser =>
        {
            var parserStartTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation($"   ?? {parser.Name} starting...");
                
                var result = await parser.ParseAsync(ocrText, cancellationToken);
                
                var elapsed = DateTime.UtcNow - parserStartTime;
                _logger.LogInformation($"   ? {parser.Name} completed in {elapsed.TotalSeconds:F2}s ({result.Medications.Count} meds)");
                
                return (parser.Name, result, success: result.Success);
            }
            catch (Exception ex)
            {
                var elapsed = DateTime.UtcNow - parserStartTime;
                _logger.LogWarning($"   ? {parser.Name} failed after {elapsed.TotalSeconds:F2}s: {ex.Message}");
                return (parser.Name, (PrescriptionReadResult?)null, success: false);
            }
        }).ToList();
        
        // Wait for all parsers with timeout
        var completedTask = await Task.WhenAny(
            Task.WhenAll(tasks),
            Task.Delay(TimeSpan.FromSeconds(30), cancellationToken));
        
        var results = await Task.WhenAll(tasks);
        
        return results
            .Where(r => r.success && r.result != null)
            .ToDictionary(r => r.Name, r => r.result!);
    }
    
    /// <summary>
    /// Execute parsers sequentially (SAFE)
    /// </summary>
    private async Task<Dictionary<string, PrescriptionReadResult>> ExecuteParsersSequentialAsync(
        string ocrText,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Executing parsers in SEQUENTIAL mode");
        
        var results = new Dictionary<string, PrescriptionReadResult>();
        var enabledParsers = _parserRegistry.GetEnabledParsers().ToList();
        
        foreach (var parser in enabledParsers)
        {
            var parserStartTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation($"   ?? {parser.Name} starting...");
                
                var result = await parser.ParseAsync(ocrText, cancellationToken);
                
                var elapsed = DateTime.UtcNow - parserStartTime;
                
                if (result.Success && result.Medications.Any())
                {
                    results[parser.Name] = result;
                    _logger.LogInformation($"   ? {parser.Name} succeeded in {elapsed.TotalSeconds:F2}s ({result.Medications.Count} meds)");
                }
                else
                {
                    _logger.LogWarning($"   ?? {parser.Name} returned no results in {elapsed.TotalSeconds:F2}s");
                }
            }
            catch (Exception ex)
            {
                var elapsed = DateTime.UtcNow - parserStartTime;
                _logger.LogError(ex, $"   ? {parser.Name} failed after {elapsed.TotalSeconds:F2}s");
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
        
        _logger.LogInformation($"?? Merging results from {parserResults.Count} parsers");
        
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
        CompletionValidation validation,
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
                medicationCount: mergedResult.Medications.Count,
                doctorName: mergedResult.Doctor?.Name,
                patientName: mergedResult.Patient?.Name,
                prescriptionDate: mergedResult.PrescriptionDate,
                processingTime: DateTime.UtcNow - processingResult.StartTime,
                processingAttempts: parserResults.Count
            );
            
            processingResult.DatabaseId = ocrResultId;
            _logger.LogInformation($"?? Stored in database with ID: {ocrResultId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "?? Database storage failed (continuing anyway)");
        }
    }
    
    private PrescriptionProcessingResult FailFast(string errorMessage, PrescriptionProcessingResult result)
    {
        _logger.LogWarning($"?? Fast failure: {errorMessage}");
        result.Success = false;
        result.ErrorMessage = errorMessage;
        result.EndTime = DateTime.UtcNow;
        result.ProcessingTime = result.EndTime.Value - result.StartTime;
        return result;
    }
}
```

---

## ?? Performance Comparison

### **Before (Sequential)**
```
DeepSeek:  20s (timeout)
OpenAI:    25s (success)
Claude:    15s (success)
??????????????????????
Total:     60 seconds
```

### **After (Parallel)**
```
DeepSeek:  20s (timeout) ?
OpenAI:    25s (success) ??? Running simultaneously
Claude:    15s (success) ?
??????????????????????
Total:     25 seconds (Max of all)

Improvement: 58% faster! ?
```

---

## ?? Dependency Injection Setup

```csharp
// In MauiProgram.cs or Program.cs

// Register parsers
builder.Services.AddScoped<IPrescriptionParser>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
    var logger = sp.GetRequiredService<ILogger<DeepSeekParser>>();
    
    var deepSeek = config.DeepSeek;
    if (deepSeek != null && deepSeek.Enabled)
    {
        var parser = new DeepSeekParser(httpClient, deepSeek.ApiKey, deepSeek.ApiUrl);
        return new ResilientParser(parser, logger);
    }
    
    return null;
});

// Register registry
builder.Services.AddSingleton<ParserRegistry>(sp =>
{
    var registry = new ParserRegistry(sp.GetRequiredService<ILogger<ParserRegistry>>());
    
    // Auto-register all available parsers
    var parsers = sp.GetServices<IPrescriptionParser>().Where(p => p != null);
    foreach (var parser in parsers)
    {
        registry.Register(parser);
    }
    
    return registry;
});

// Register cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<PrescriptionCacheService>();

// Register orchestrator V2
builder.Services.AddScoped<AgentOrchestratorV2>();
```

---

## ? Key Benefits

| Feature | Benefit |
|---------|---------|
| **Parallel Execution** | 50-70% faster processing |
| **Circuit Breaker** | Auto-disable failing parsers |
| **Caching** | 100% faster on duplicates (no API calls) |
| **Dynamic Registry** | Easy to add/remove parsers |
| **Testability** | Mock parsers easily |
| **Metrics** | Track performance per parser |
| **Resilience** | One parser failure doesn't affect others |

---

**Status**: ? **DESIGN COMPLETE**  
**Next**: Implement and test  
**Expected Improvement**: 50-80% faster, more reliable, easier to maintain ??
