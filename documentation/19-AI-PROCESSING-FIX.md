# ?? "Process with AI" Not Working - Complete Fix

## ? **Root Cause**

The OpenAI API key in `MauiProgram.cs` is set to the placeholder value `"your-openai-api-key-here"`, which causes API authentication to fail.

---

## ? **Solution: Configure OpenAI API Key**

### **Option 1: Direct Configuration (Testing Only)**

**File**: `mobile/MedRemind.Mobile/MauiProgram.cs`

**Find this line** (around line 58):
```csharp
var apiKey = "your-openai-api-key-here";
```

**Replace with your actual API key**:
```csharp
var apiKey = "sk-proj-YOUR_ACTUAL_OPENAI_API_KEY_HERE";
```

### **Option 2: Environment Variable (Recommended)**

**Step 1: Set environment variable**

**Windows (PowerShell)**:
```powershell
[System.Environment]::SetEnvironmentVariable('OPENAI_API_KEY', 'sk-proj-YOUR_KEY', 'User')
```

**Windows (CMD)**:
```cmd
setx OPENAI_API_KEY "sk-proj-YOUR_KEY"
```

**Step 2: Update MauiProgram.cs**:
```csharp
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
    ?? throw new InvalidOperationException("OpenAI API key not configured");
```

### **Option 3: Secure Storage (Production)**

**Step 1: Store API key on first run**:
```csharp
// In LoginViewModel or SettingsViewModel
await SecureStorage.SetAsync("OpenAI_API_Key", "sk-proj-YOUR_KEY");
```

**Step 2: Update MauiProgram.cs**:
```csharp
builder.Services.AddScoped<IPrescriptionReaderService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var validationAgent = sp.GetRequiredService<IValidationAgentService>();
    
    // Load from secure storage
    var apiKey = SecureStorage.GetAsync("OpenAI_API_Key").GetAwaiter().GetResult()
        ?? "fallback-key-for-testing";
    
    return new OpenAIPrescriptionReaderService(httpClient, apiKey, validationAgent);
});
```

---

## ?? **How to Get OpenAI API Key**

### **Step 1: Create OpenAI Account**

1. Go to https://platform.openai.com/signup
2. Sign up with email or Google/Microsoft account
3. Verify your email address

### **Step 2: Add Payment Method**

1. Go to https://platform.openai.com/account/billing
2. Click "Add payment method"
3. Add credit card (minimum $5 credit recommended)
4. OpenAI charges ~$0.01-0.05 per prescription scan

### **Step 3: Create API Key**

1. Go to https://platform.openai.com/api-keys
2. Click "Create new secret key"
3. Give it a name: "MedRemind-Development"
4. Copy the key (starts with `sk-proj-...`)
5. **IMPORTANT**: Save it immediately - you can't see it again!

### **Step 4: Verify API Access**

Test your API key with curl:
```bash
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"
```

If it works, you'll see a list of available models.

---

## ?? **After Configuring API Key**

### **Rebuild and Test**

1. **Update API key** in MauiProgram.cs
2. **Clean solution**: Build ? Clean Solution
3. **Rebuild**: Build ? Rebuild Solution
4. **Restart Visual Studio** (if using environment variable)
5. **Deploy** to device: Press F5

### **Test Process Flow**

1. **Open MedRemind app**
2. **Navigate to Upload tab**
3. **Select/capture prescription image**
4. **Tap "?? Process with AI"**
5. **Wait 3-10 seconds**
6. **Verify**:
   - ? Loading indicator shows
   - ? "Processing... Please wait" message displays
   - ? After processing, medications appear
   - ? Confidence score is displayed
   - ? Success message shows

---

## ?? **Troubleshooting**

### **Issue 1: "OpenAI API error: 401 Unauthorized"**

**Cause**: Invalid or missing API key

**Solution**:
1. Verify API key starts with `sk-proj-`
2. Check for extra spaces or quotes
3. Verify API key is active at https://platform.openai.com/api-keys
4. Try creating a new API key

### **Issue 2: "OpenAI API error: 429 Too Many Requests"**

**Cause**: Rate limit exceeded or no credits

**Solution**:
1. Check billing: https://platform.openai.com/account/billing
2. Add credits if balance is $0
3. Wait 1 minute and try again
4. Upgrade to paid tier if on free tier

### **Issue 3: "OpenAI API error: 500 Internal Server Error"**

**Cause**: OpenAI service issue

**Solution**:
1. Check OpenAI status: https://status.openai.com
2. Wait a few minutes and retry
3. Try with a different image

### **Issue 4: Processing never completes (spinning forever)**

**Check**:
```csharp
// Add debug logging in PrescriptionUploadViewModel.cs ProcessPrescriptionAsync method
System.Diagnostics.Debug.WriteLine($"API Key configured: {!string.IsNullOrEmpty(_imageBase64)}");
System.Diagnostics.Debug.WriteLine($"Calling OpenAI API...");
```

**Solution**:
1. Check internet connection
2. Check Visual Studio Output window for errors
3. Add try-catch with detailed error logging
4. Verify HttpClient is configured correctly

### **Issue 5: "Error processing prescription: Task was canceled"**

**Cause**: Request timeout

**Solution**:
```csharp
// In MauiProgram.cs, configure HttpClient timeout
builder.Services.AddSingleton<HttpClient>(sp => 
{
    var client = new HttpClient();
    client.Timeout = TimeSpan.FromSeconds(30); // Increase timeout
    return client;
});
```

---

## ?? **Expected Behavior**

### **Successful Processing**

```
Timeline:
1. User taps "Process with AI"
   ? Loading indicator appears immediately
   ? "Processing... Please wait" shows

2. After 3-8 seconds
   ? Loading indicator stops
   ? "Step 3: Review Results" section appears
   ? Confidence score shows (e.g., "92%")
   ? Extracted medications list displays
   ? Success message: "? All medications validated successfully!"

3. User can:
   ? Edit individual medications
   ? Tap "?? Save All" to save
   ? Tap "?? Try Again" to start over
```

### **Failed Processing**

```
If fails:
? Loading indicator stops
? Error message displays:
  - "OpenAI API error: 401 Unauthorized" (bad API key)
  - "Failed to extract medications. Please try again with a clearer image."
  - "Error processing prescription: [specific error]"
```

---

## ?? **Debug Logging**

Add this to `PrescriptionUploadViewModel.cs` to see what's happening:

```csharp
[RelayCommand]
private async Task ProcessPrescriptionAsync()
{
    System.Diagnostics.Debug.WriteLine("=== START PROCESSING ===");
    
    if (string.IsNullOrEmpty(_imageBase64))
    {
        System.Diagnostics.Debug.WriteLine("ERROR: No image selected");
        ShowError("Please select a photo first");
        return;
    }

    System.Diagnostics.Debug.WriteLine($"Image Base64 length: {_imageBase64.Length}");
    IsProcessing = true;
    
    try
    {
        System.Diagnostics.Debug.WriteLine("Calling OpenAI API...");
        var result = await _prescriptionReader.ReadPrescriptionFromBase64Async(_imageBase64);
        
        System.Diagnostics.Debug.WriteLine($"API Response - Success: {result.Success}");
        System.Diagnostics.Debug.WriteLine($"API Response - Confidence: {result.ConfidenceScore}");
        System.Diagnostics.Debug.WriteLine($"API Response - Medications: {result.Medications.Count}");
        
        if (!result.Success)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR: {result.ErrorMessage}");
        }
        
        // ... rest of processing
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"EXCEPTION: {ex.Message}");
        System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
    }
    finally
    {
        IsProcessing = false;
        System.Diagnostics.Debug.WriteLine("=== END PROCESSING ===");
    }
}
```

**View logs**: Visual Studio ? View ? Output ? Select "Debug"

---

## ?? **Cost Estimation**

| Usage | Estimated Cost |
|-------|----------------|
| 1 prescription scan | $0.01 - $0.05 |
| 10 prescriptions | $0.10 - $0.50 |
| 100 prescriptions | $1.00 - $5.00 |
| 1000 prescriptions | $10 - $50 |

**Tips to reduce costs**:
- Use smaller images (compress to 1024x1024)
- Cache results to avoid re-processing
- Implement manual entry fallback
- Use gpt-4o-mini model (cheaper) if accuracy is acceptable

---

## ? **Verification Checklist**

After fixing:

- [ ] API key is configured (not "your-openai-api-key-here")
- [ ] API key starts with `sk-proj-` or `sk-`
- [ ] OpenAI account has credits ($5+ recommended)
- [ ] Internet connection is working
- [ ] App is rebuilt and redeployed
- [ ] Camera/Gallery can select images
- [ ] "Process with AI" button appears after image selection
- [ ] Tapping button shows loading indicator
- [ ] Processing completes within 10 seconds
- [ ] Medications are extracted and displayed
- [ ] Confidence score is shown
- [ ] Can edit and save medications

---

## ?? **Quick Fix Summary**

**Problem**: `"your-openai-api-key-here"` placeholder in MauiProgram.cs  
**Solution**: Replace with real OpenAI API key  
**Get key**: https://platform.openai.com/api-keys  
**Cost**: ~$0.01-0.05 per prescription  
**Time to fix**: 5 minutes  

---

## ?? **Expected Success Message**

After fixing, you should see:

```
Step 3: Review Results

Confidence: 92%

? All medications validated successfully!

Extracted Medications:
???????????????????????????????????
? Aspirin                    Edit ?
? 100 mg - Once daily            ?
? Duration: 30 days              ?
???????????????????????????????????

[?? Save All]  [?? Try Again]
```

---

**Status**: ?? Fix Ready - Configure OpenAI API key to enable AI processing  
**Priority**: Critical - Core feature blocked  
**Est. Time**: 5 minutes (get key + configure)  
**Impact**: Enables full prescription scanning functionality  

---

**Get your API key at: https://platform.openai.com/api-keys** ??
