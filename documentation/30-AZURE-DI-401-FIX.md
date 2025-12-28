# ?? Azure Document Intelligence 401 Error Fix

## Problem

```
{"error":{"code":"401","message":"Access denied due to invalid subscription key or wrong API endpoint. 
Make sure to provide a valid key for an active subscription and use a correct regional API endpoint for your resource."}}
```

This error indicates either:
1. ? **Wrong API endpoint** (most likely)
2. ? Invalid API key
3. ? Expired subscription

---

## ? Solution

### **Step 1: Get Correct Endpoint from Azure Portal**

1. Go to [Azure Portal](https://portal.azure.com)
2. Find your **Document Intelligence** (or Form Recognizer) resource
3. Click on **"Keys and Endpoint"** in the left menu
4. Copy the **Endpoint** URL

**Correct Format**:
```
https://<your-resource-name>.<region>.cognitiveservices.azure.com/
```

**Examples**:
- East US: `https://my-doc-intel.eastus.cognitiveservices.azure.com/`
- East US 2: `https://my-doc-intel.eastus2.cognitiveservices.azure.com/`
- West US: `https://my-doc-intel.westus.cognitiveservices.azure.com/`

### **Step 2: Update appsettings.json**

Replace the endpoint in your `mobile/MedRemind.Mobile/appsettings.json`:

**Current** ?:
```json
{
  "AzureDocumentIntelligence": {
    "Endpoint": "https://documentintelligencecustomermodelservice.cognitiveservices.azure.com/",
    "ApiKey": "7X1mII...",
    "Region": "eastus2"
  }
}
```

**Correct** ?:
```json
{
  "AzureDocumentIntelligence": {
    "Endpoint": "https://YOUR-RESOURCE-NAME.eastus2.cognitiveservices.azure.com/",
    "ApiKey": "7X1mII...",
    "Region": "eastus2"
  }
}
```

Replace `YOUR-RESOURCE-NAME` with your actual Azure resource name.

---

## ?? How to Find Your Resource Name

### **Option 1: Azure Portal**

1. Azure Portal ? All Resources
2. Find your Document Intelligence resource
3. Note the **Name** (e.g., `my-document-intelligence`)
4. Note the **Location** (e.g., `East US 2`)
5. Endpoint format: `https://{name}.{location}.cognitiveservices.azure.com/`

### **Option 2: From Error Message**

If you create a resource in Azure, you'll get:
```
Resource Name: medremind-doc-intel
Location: East US 2
Endpoint: https://medremind-doc-intel.eastus2.cognitiveservices.azure.com/
```

---

## ?? Testing the Endpoint

### **Test with cURL**

```bash
curl -X POST "https://YOUR-RESOURCE-NAME.eastus2.cognitiveservices.azure.com/formrecognizer/documentModels/prebuilt-read:analyze?api-version=2024-11-30" \
  -H "Ocp-Apim-Subscription-Key: YOUR-API-KEY" \
  -H "Content-Type: application/octet-stream" \
  --data-binary "@test-image.jpg"
```

**Expected Response**:
- ? `202 Accepted` with `Operation-Location` header
- ? `401 Unauthorized` = Wrong key or endpoint

---

## ?? Common Endpoint Patterns

| Region | Endpoint Format |
|--------|-----------------|
| East US | `https://<name>.eastus.cognitiveservices.azure.com/` |
| East US 2 | `https://<name>.eastus2.cognitiveservices.azure.com/` |
| West US | `https://<name>.westus.cognitiveservices.azure.com/` |
| West US 2 | `https://<name>.westus2.cognitiveservices.azure.com/` |
| Central US | `https://<name>.centralus.cognitiveservices.azure.com/` |
| North Europe | `https://<name>.northeurope.cognitiveservices.azure.com/` |
| West Europe | `https://<name>.westeurope.cognitiveservices.azure.com/` |

---

## ?? If You Don't Have Azure Resource

If you haven't created an Azure Document Intelligence resource yet:

### **Option 1: Create Free Resource**

1. Go to [Azure Portal](https://portal.azure.com)
2. Create new resource ? **AI + Machine Learning** ? **Document Intelligence**
3. Fill details:
   - Resource name: `medremind-doc-intel`
   - Location: `East US 2`
   - Pricing tier: **Free F0** (5,000 pages/month free)
4. Create and get endpoint + key

### **Option 2: Use OpenAI Vision Only**

If you don't want to use Azure DI, the system will automatically fall back to OpenAI Vision API (works but more expensive).

To disable Azure DI and use only OpenAI:

**Update code** to catch Azure DI errors and go straight to fallback:

```csharp
// In OpenAIPrescriptionReaderService.cs
try
{
    extractedText = await _azureDocService.ExtractTextFromImageAsync(base64Image, cancellationToken);
}
catch (Exception ex)
{
    // Log and fall back to OpenAI Vision
    System.Diagnostics.Debug.WriteLine($"?? Azure DI unavailable, using OpenAI Vision: {ex.Message}");
    return await ProcessWithOpenAIVisionAsync(base64Image, cancellationToken);
}
```

---

## ? After Fixing

### **Expected Logs**

**Before (Error)** ?:
```
?? Step 1: Extracting text with Azure Document Intelligence...
?? Azure DI: Submitting to: https://documentintelligencecustomermodelservice...
? Azure DI: API error! Status: 401
   Error Code: 401
   Error Message: Access denied due to invalid subscription key or wrong API endpoint
```

**After (Success)** ?:
```
?? Step 1: Extracting text with Azure Document Intelligence...
?? Azure DI: Submitting to: https://YOUR-RESOURCE.eastus2.cognitiveservices.azure.com/...
? Azure DI: Analysis submitted. Operation: https://...
? Azure DI: Polling... Status: running (Attempt 1/30)
? Azure DI: Analysis succeeded after 3 attempts
? Text extracted: 487 characters
```

---

## ?? Quick Checklist

- [ ] Get correct endpoint from Azure Portal (Keys and Endpoint)
- [ ] Update `appsettings.json` with correct endpoint
- [ ] Ensure endpoint format: `https://{name}.{region}.cognitiveservices.azure.com/`
- [ ] Rebuild app
- [ ] Test prescription processing
- [ ] Check logs for "? Azure DI: Analysis succeeded"

---

## ?? Example Configuration

**Your Azure Resource**:
- Name: `medremind-doc-intel`
- Location: `East US 2`
- Key: `7X1mIIPYUxCTL9pNNuDQEp8r21yWhRzYyZgNGZJbuQuag2Ib44UvJQQJ99BCACHYHv6XJ3w3AAALACOGo0j2`

**Correct Configuration**:
```json
{
  "Environments": {
    "Development": {
      "AzureDocumentIntelligence": {
        "Endpoint": "https://medremind-doc-intel.eastus2.cognitiveservices.azure.com/",
        "ApiKey": "7X1mIIPYUxCTL9pNNuDQEp8r21yWhRzYyZgNGZJbuQuag2Ib44UvJQQJ99BCACHYHv6XJ3w3AAALACOGo0j2",
        "Region": "eastus2",
        "TimeoutSeconds": 60
      }
    }
  }
}
```

---

## ?? Alternative: Skip Azure DI Temporarily

If you want to test without Azure DI, you can temporarily use only OpenAI Vision:

**Comment out Azure DI** in `OpenAIPrescriptionReaderService.cs`:

```csharp
// TEMPORARY: Skip Azure DI for testing
// return await ProcessWithOpenAIVisionAsync(base64Image, cancellationToken);

// Step 1: Extract text using Azure Document Intelligence
try {
    extractedText = await _azureDocService.ExtractTextFromImageAsync(base64Image, cancellationToken);
}
catch (Exception ex) {
    // Fall back to OpenAI Vision
    return await ProcessWithOpenAIVisionAsync(base64Image, cancellationToken);
}
```

This way, if Azure DI fails, it will automatically use OpenAI Vision (which costs more but works).

---

## ? Summary

**Root Cause**: Wrong API endpoint URL

**Fix**: Update endpoint in `appsettings.json` to:
```
https://YOUR-RESOURCE-NAME.REGION.cognitiveservices.azure.com/
```

**Get from**: Azure Portal ? Your Resource ? Keys and Endpoint

**After fix**: Rebuild app and test prescription processing

---

**Get the correct endpoint from Azure Portal and update your configuration!** ??
