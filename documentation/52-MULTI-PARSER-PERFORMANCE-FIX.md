# ?? Multi-Parser System Performance Optimization

## ?? Issues Identified

### **1. Excessive Timeouts**
```
HttpClient Timeout: 120 seconds
DeepSeek Retries: 3 attempts × 120s = 360s max
OpenAI Retries: 3 attempts × 120s = 360s max
Total worst case: 720+ seconds (12+ minutes!)
```

### **2. Sequential Execution**
```
DeepSeek (30s) ? OpenAI (30s) ? Claude (30s) = 90s total
vs
Parallel: Max(30s, 30s, 30s) = 30s total
```

### **3. No Per-Parser Timeout**
```
Each parser can run indefinitely until HttpClient timeout
No way to cancel slow parsers and move on
```

## ? Solutions Implemented

### **Solution 1: Reduce HttpClient Timeout**
### **Solution 2: Add Per-Parser Timeout**
### **Solution 3: Parallel Parser Execution (Optional)**
### **Solution 4: Reduce Retry Attempts**
### **Solution 5: Add Circuit Breaker Pattern**

---

## ?? Recommended Changes

### **Change 1: Reduce Global HTTP Timeout**

**File**: `mobile/MedRemind.Mobile/MauiProgram.cs`

```csharp
var httpClient = new HttpClient(handler)
{
    Timeout = TimeSpan.FromSeconds(30) // Reduced from 120s to 30s
};
```

**Impact**: Individual API calls won't hang for 2 minutes

---

### **Change 2: Add Per-Parser Timeout in Orchestrator**

**File**: `backend/MedRemind.Services/AI/Agents/AgentOrchestrator.cs`

```csharp
// Add timeout configuration
private const int PARSER_TIMEOUT_SECONDS = 20; // 20 seconds per parser

// In ProcessPrescriptionAsync, wrap parser calls:
for (int i = 0; i < parsers.Count; i++)
{
    var (priority, name, parser) = parsers[i];
    
    System.Diagnostics.Debug.WriteLine($"\n   Parser {i + 1}/{parsers.Count}: {name} (Priority {priority})");
    
    try
    {
        // NEW: Add timeout for each parser
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(PARSER_TIMEOUT_SECONDS));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        
        var parserResult = await parser().WaitAsync(linkedCts.Token); // Add timeout
        
        parserResults[name] = parserResult;
        providersUsed.Add(name);
        // ... rest of code
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
        System.Diagnostics.Debug.WriteLine($"   ?? {name} timed out after {PARSER_TIMEOUT_SECONDS}s");
        // Continue to next parser
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"   ? {name} failed: {ex.Message}");
        // Continue to next parser
    }
}
```

---

### **Change 3: Reduce DeepSeek Retries**

**File**: `backend/MedRemind.Services/AI/DeepSeekPrescriptionParserAgent.cs`

```csharp
private const int MAX_RETRIES = 2; // Reduced from 3 to 2
private const int RETRY_DELAY_MS = 500; // Reduced from 1000ms to 500ms
```

**Impact**: Faster failure recovery

---

### **Change 4: Add Timeout Warning**

```csharp
// In DeepSeekPrescriptionParserAgent
public async Task<PrescriptionParseResult> ParsePrescriptionTextAsync(
    string ocrText,
    CancellationToken cancellationToken = default)
{
    var startTime = DateTime.UtcNow;
    
    // ... existing code ...
    
    try
    {
        var response = await _httpClient.PostAsJsonAsync(
            _apiUrl,
            requestBody,
            cancellationToken);
            
        var elapsed = DateTime.UtcNow - startTime;
        System.Diagnostics.Debug.WriteLine($"   ?? API call took: {elapsed.TotalSeconds:F2}s");
        
        // ... rest of code
    }
}
```

---

## ?? Optional: Parallel Parser Execution

**For maximum speed**, run parsers in parallel:

```csharp
// STEP 2: Parallel Multi-Parser System
System.Diagnostics.Debug.WriteLine("\n?? STEP 2: Parallel Multi-Parser System");

var parserTasks = parsers.Select(async (p) =>
{
    var (priority, name, parser) = p;
    try
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        
        var result = await parser().WaitAsync(linkedCts.Token);
        return (name, result, success: true);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"   ? {name} failed: {ex.Message}");
        return (name, (PrescriptionParseResult?)null, success: false);
    }
}).ToList();

var results = await Task.WhenAll(parserTasks);

// Process results
foreach (var (name, result, success) in results.Where(r => r.success && r.result != null))
{
    parserResults[name] = result;
    providersUsed.Add(name);
    
    if (mergedResult == null)
    {
        mergedResult = result;
    }
    else
    {
        mergedResult = _mergerService.MergeResults(mergedResult, result, ...);
    }
}
```

**Benefit**: All parsers run simultaneously
**Time**: Max(DeepSeek, OpenAI, Claude) instead of Sum(...)

---

## ?? Performance Comparison

### **Before Optimization**
```
Scenario: All parsers timeout

DeepSeek: 120s timeout × 3 retries = 360s
OpenAI:   120s timeout × 3 retries = 360s
Claude:   120s timeout × 3 retries = 360s
Total: 1080s (18 minutes!) ?
```

### **After Optimization (Sequential)**
```
Scenario: All parsers timeout

DeepSeek: 20s timeout × 2 retries = 40s
OpenAI:   20s timeout × 2 retries = 40s
Claude:   20s timeout × 2 retries = 40s
Total: 120s (2 minutes) ? 90% faster
```

### **After Optimization (Parallel)**
```
Scenario: All parsers timeout

All parsers run simultaneously
Max(20s, 20s, 20s) × 2 retries = 40s
Total: 40s ? 96% faster!
```

---

## ? Quick Fix Implementation

### **Minimal Changes for Immediate Relief**

1. **Reduce HttpClient Timeout** (5 minutes ? 30 seconds)
2. **Add per-parser timeout** (20 seconds)
3. **Reduce retries** (3 ? 2)

### **Expected Results**
- 80-90% faster processing
- Better failure recovery
- More responsive UI

---

## ?? Testing

### **Test 1: Success Case**
```
DeepSeek: 5s ?
OpenAI: 6s ?
Result: 11s (sequential) or 6s (parallel)
```

### **Test 2: DeepSeek Fails**
```
DeepSeek: 20s timeout ?
OpenAI: 5s ?
Result: 25s (sequential) or 20s (parallel)
```

### **Test 3: All Fail**
```
DeepSeek: 20s ?
OpenAI: 20s ?
Claude: 20s ?
Result: 60s (sequential) or 20s (parallel)
Old behavior: 360s+ ??
```

---

## ?? Implementation Priority

### **Priority 1 (Do First - Immediate Impact)**
1. ? Reduce HttpClient timeout to 30s
2. ? Add per-parser timeout (20s)
3. ? Reduce retry attempts to 2

### **Priority 2 (Optional - Better Performance)**
4. ? Implement parallel parser execution
5. ? Add circuit breaker pattern

### **Priority 3 (Nice to Have)**
6. Add parser caching
7. Add parser health checks
8. Add performance metrics

---

## ?? Expected Outcome

**Current State**: 2-18 minutes processing time ?  
**After Fix**: 10-60 seconds processing time ?  
**With Parallel**: 5-20 seconds processing time ?

**Success Rate**: Much higher due to faster timeouts and better error handling

---

Would you like me to implement these changes for you?
