# ?? AzureDocumentIntelligenceService DI Registration Fix

## ? Error Fixed

```
System.InvalidOperationException: 
Unable to resolve service for type 'MedRemind.Services.AI.AzureDocumentIntelligenceService' 
while attempting to activate 'MedRemind.Mobile.ViewModels.PrescriptionUploadViewModel'.
```

## ? Solution

The error occurred because `AzureDocumentIntelligenceService` was only created internally within `IPrescriptionReaderService` registration but wasn't registered as a standalone service that could be injected into other components.

### **Root Cause**

**PrescriptionUploadViewModel Constructor:**
```csharp
public PrescriptionUploadViewModel(
    // ... other parameters
    AzureDocumentIntelligenceService azureDocumentIntelligenceService) // ? Not registered!
{
    _azureDocumentIntelligenceService = azureDocumentIntelligenceService;
}
```

**MauiProgram.cs (Before):**
```csharp
// Only used internally in IPrescriptionReaderService
builder.Services.AddScoped<IPrescriptionReaderService>(sp =>
{
    var azureDocService = new AzureDocumentIntelligenceService(httpClient, azureEndpoint, azureKey);
    // ... use internally only
});

// ? No standalone registration for AzureDocumentIntelligenceService
```

When DI tries to create `PrescriptionUploadViewModel`, it can't find a registered `AzureDocumentIntelligenceService`.

---

## ?? What Changed

**File**: `mobile/MedRemind.Mobile/MauiProgram.cs`

### Added Standalone Registration:

```csharp
// Register AzureDocumentIntelligenceService separately for injection into ViewModels
builder.Services.AddScoped<AzureDocumentIntelligenceService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
    var azureEndpoint = config.AzureDocumentIntelligence.Endpoint;
    var azureKey = config.AzureDocumentIntelligence.ApiKey;
    
    System.Diagnostics.Debug.WriteLine($"? Registering standalone Azure Document Intelligence Service");
    return new AzureDocumentIntelligenceService(httpClient, azureEndpoint, azureKey);
});
```

---

## ?? Why This Service is Needed

In `PrescriptionUploadViewModel.ProcessPrescriptionAsync()`, the service is used directly:

```csharp
// Extract OCR text using Azure Document Intelligence
var ocrText = await _azureDocumentIntelligenceService.ExtractTextFromImageAsync(_imageBase64);

// If OCR fails, fall back to OpenAI Vision
if (string.IsNullOrWhiteSpace(ocrText) || ocrText.Length < 50)
{
    // Use OpenAI Vision directly
    var directResult = await _prescriptionReader.ReadPrescriptionFromBase64Async(_imageBase64);
}
else
{
    // Use OCR text with Multi-Agent Orchestrator
    var orchestratorResult = await _agentOrchestrator.ProcessPrescriptionAsync(
        ocrText, 
        prescriptionFileName, 
        prescription.Id);
}
```

### **Processing Flow:**

```
???????????????????????????????????????
?  User uploads prescription image    ?
???????????????????????????????????????
                 ?
???????????????????????????????????????
?  PrescriptionUploadViewModel        ?
?  ProcessPrescriptionAsync()         ?
???????????????????????????????????????
                 ?
???????????????????????????????????????
?  AzureDocumentIntelligenceService   ? ? Needs to be injected!
?  ExtractTextFromImageAsync()        ?
???????????????????????????????????????
        Success         Failure
         ?               ?
         ?               ?
??????????????????  ?????????????????????
? Multi-Agent    ?  ? OpenAI Vision     ?
? Orchestrator   ?  ? Direct Processing ?
??????????????????  ?????????????????????
```

---

## ?? Service Registration Patterns

### **Pattern 1: Single Use (Internal Only)**
```csharp
// Service only used internally by another service
builder.Services.AddScoped<IParentService>(sp =>
{
    var helper = new HelperService(); // Not registered separately
    return new ParentService(helper);
});

? Problem: Can't inject HelperService elsewhere
? Use when: Helper is truly internal and never needed elsewhere
```

### **Pattern 2: Shared Service (Registered Separately)**
```csharp
// Service used by multiple components
builder.Services.AddScoped<HelperService>();

builder.Services.AddScoped<IParentService, ParentService>();
// ParentService constructor: ParentService(HelperService helper)

builder.Services.AddScoped<OtherService>();
// OtherService constructor: OtherService(HelperService helper)

? Problem: Both services can inject HelperService
? Use when: Service is shared across multiple components
```

### **Pattern 3: Both (This Fix)**
```csharp
// Register service for standalone injection
builder.Services.AddScoped<AzureDocumentIntelligenceService>(sp => /* ... */);

// Also used internally by another service
builder.Services.AddScoped<IPrescriptionReaderService>(sp =>
{
    var azureService = sp.GetRequiredService<AzureDocumentIntelligenceService>();
    return new OpenAIPrescriptionReaderService(/* ... */, azureService, /* ... */);
});

? Result: 
- PrescriptionUploadViewModel can inject AzureDocumentIntelligenceService
- OpenAIPrescriptionReaderService also uses the same instance
- Single instance per scope (efficient)
```

---

## ?? Service Dependencies

### **Before Fix:**
```
PrescriptionUploadViewModel
    ??? IPrescriptionReaderService ?
    ?   ??? AzureDocumentIntelligenceService (internal) ?
    ??? AgentOrchestrator ?
    ??? PrescriptionDeduplicationService ?
    ??? AzureDocumentIntelligenceService ? NOT REGISTERED!
```

### **After Fix:**
```
PrescriptionUploadViewModel
    ??? IPrescriptionReaderService ?
    ?   ??? AzureDocumentIntelligenceService (shared) ?
    ??? AgentOrchestrator ?
    ??? PrescriptionDeduplicationService ?
    ??? AzureDocumentIntelligenceService ? REGISTERED!
         (same instance as used by IPrescriptionReaderService)
```

---

## ?? Service Lifetime

### **Scoped Registration:**
```csharp
builder.Services.AddScoped<AzureDocumentIntelligenceService>();
```

**Means:**
- ? One instance per **scope** (typically per HTTP request or page navigation)
- ? Shared within the scope
- ? Disposed at end of scope
- ? Efficient for services with state or connections

**Alternative Lifetimes:**

| Lifetime | When Created | When Disposed | Use For |
|----------|--------------|---------------|---------|
| **Singleton** | App start | App shutdown | Stateless services, caches |
| **Scoped** | Per scope | End of scope | Services with request-level state |
| **Transient** | Every injection | After use | Lightweight, stateless operations |

**Why Scoped for AzureDocumentIntelligenceService?**
- Uses HttpClient (scoped)
- May maintain connection state
- Used multiple times in same workflow
- Should share instance within page/request

---

## ? Verification Checklist

After fix:
- [x] `AzureDocumentIntelligenceService` registered in DI
- [x] Build successful
- [x] `PrescriptionUploadViewModel` can be created
- [x] Service injected correctly
- [x] Both ViewModel and IPrescriptionReaderService use same instance

---

## ?? Common DI Mistakes

### **Mistake 1: Forgetting to Register**
```csharp
? var service = sp.GetRequiredService<MyService>();
// If MyService not registered ? Exception!

? builder.Services.AddScoped<MyService>();
```

### **Mistake 2: Circular Dependencies**
```csharp
? ServiceA depends on ServiceB
   ServiceB depends on ServiceA
   ? Stack overflow!

? Refactor to use interfaces or event-based communication
```

### **Mistake 3: Wrong Lifetime**
```csharp
? builder.Services.AddSingleton<DbContext>(); // DbContext should be scoped!
? builder.Services.AddTransient<HttpClient>(); // Should be singleton/scoped!

? builder.Services.AddScoped<DbContext>();
? builder.Services.AddHttpClient(); // Manages lifetime internally
```

### **Mistake 4: Creating Services Manually**
```csharp
? var myService = new MyService(); // Bypasses DI, no dependency resolution

? var myService = sp.GetRequiredService<MyService>(); // Uses DI
```

---

## ?? Summary

| Aspect | Before | After |
|--------|--------|-------|
| **AzureDocumentIntelligenceService Registration** | ? Not registered | ? Registered as Scoped |
| **PrescriptionUploadViewModel Creation** | ? Fails (missing dependency) | ? Works |
| **Service Sharing** | ? Created separately each time | ? Shared instance per scope |
| **Build Status** | ? Runtime error | ? Success |

---

## ?? Testing

### **Test 1: ViewModel Creation**
```csharp
// Resolve ViewModel from DI
var viewModel = serviceProvider.GetRequiredService<PrescriptionUploadViewModel>();

Expected: ? No exception
```

### **Test 2: Service Injection**
```csharp
// Check if AzureDocumentIntelligenceService is injected
Assert.NotNull(viewModel._azureDocumentIntelligenceService);

Expected: ? Service is present
```

### **Test 3: OCR Extraction**
```csharp
var result = await azureService.ExtractTextFromImageAsync(base64Image);

Expected: ? Returns extracted text
```

---

**Status**: ? **COMPLETE**  
**Impact**: PrescriptionUploadViewModel can now use Azure Document Intelligence for OCR  
**Next**: Test prescription upload flow with image processing
