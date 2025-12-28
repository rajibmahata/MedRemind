# ?? DeepSeek API URL Configuration

## ? Change Summary

Made the DeepSeek API URL configurable through `appsettings.json` instead of being hardcoded in the service.

---

## ?? What Changed

### 1. **Configuration Models Updated**

#### **Mobile: EmbeddedConfigurationLoader.cs**
```csharp
public class DeepSeekConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "deepseek-chat";
    public string ApiUrl { get; set; } = "https://api.deepseek.com/chat/completions"; // NEW
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 1;
}
```

#### **Backend: EnvironmentConfig.cs**
```csharp
public class DeepSeekConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "deepseek-chat";
    public string ApiUrl { get; set; } = "https://api.deepseek.com/chat/completions"; // NEW
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 1;
}
```

### 2. **appsettings.json Updated**

All three environments now include `ApiUrl`:

```json
{
  "Environments": {
    "Development": {
      "DeepSeek": {
        "ApiKey": "sk-xxx",
        "Model": "deepseek-chat",
        "ApiUrl": "https://api.deepseek.com/chat/completions",
        "Enabled": true,
        "Priority": 1
      }
    },
    "Staging": {
      "DeepSeek": {
        "ApiUrl": "https://api.deepseek.com/chat/completions",
        // ... other settings
      }
    },
    "Production": {
      "DeepSeek": {
        "ApiUrl": "https://api.deepseek.com/chat/completions",
        // ... other settings
      }
    }
  }
}
```

### 3. **DeepSeekPrescriptionParserAgent.cs Updated**

#### **Constructor**
```csharp
public DeepSeekPrescriptionParserAgent(
    HttpClient httpClient, 
    string apiKey, 
    string apiUrl = "https://api.deepseek.com/chat/completions") // NEW parameter with default
{
    _httpClient = httpClient;
    _apiKey = apiKey;
    _apiUrl = apiUrl; // Store for use
    
    System.Diagnostics.Debug.WriteLine($"? DeepSeek Parser: Initialized");
    System.Diagnostics.Debug.WriteLine($"   Model: {_model}");
    System.Diagnostics.Debug.WriteLine($"   API URL: {_apiUrl}"); // Log configured URL
}
```

#### **API Call**
```csharp
// Before: Hardcoded URL
var response = await _httpClient.PostAsJsonAsync(
    "https://api.deepseek.com/chat/completions", // ? Hardcoded
    requestBody,
    cancellationToken);

// After: Configurable URL
var response = await _httpClient.PostAsJsonAsync(
    _apiUrl, // ? From configuration
    requestBody,
    cancellationToken);
```

### 4. **MauiProgram.cs Updated**

```csharp
builder.Services.AddScoped<DeepSeekPrescriptionParserAgent?>(sp =>
{
    var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
    var deepSeek = config.DeepSeek;
    
    if (deepSeek != null && deepSeek.Enabled)
    {
        var httpClient = sp.GetRequiredService<HttpClient>();
        
        // Pass API URL from configuration
        return new DeepSeekPrescriptionParserAgent(
            httpClient, 
            deepSeek.ApiKey,
            deepSeek.ApiUrl); // ? NEW: API URL from config
    }
    
    return null;
});
```

---

## ?? Benefits

### **1. Flexibility**
```
Development:  https://api.deepseek.com/chat/completions
Staging:      https://staging.api.deepseek.com/chat/completions (if available)
Production:   https://api.deepseek.com/chat/completions
Custom:       https://your-proxy.com/deepseek (your own proxy)
```

### **2. Easy API Version Updates**
```json
// Switch to v2 without code changes
"ApiUrl": "https://api.deepseek.com/v2/chat/completions"
```

### **3. Proxy Support**
```json
// Use a proxy for cost tracking or caching
"ApiUrl": "https://your-proxy.com/deepseek"
```

### **4. Local Development**
```json
// Point to mock server for testing
"ApiUrl": "http://localhost:5000/mock/deepseek"
```

### **5. Regional Endpoints**
```json
// Use regional endpoints if DeepSeek adds them
"ApiUrl": "https://api.eu.deepseek.com/chat/completions"
```

---

## ?? Configuration Examples

### **Example 1: Default (Official API)**
```json
{
  "DeepSeek": {
    "ApiKey": "sk-xxx",
    "Model": "deepseek-chat",
    "ApiUrl": "https://api.deepseek.com/chat/completions",
    "Enabled": true,
    "Priority": 1
  }
}
```

### **Example 2: Custom Proxy**
```json
{
  "DeepSeek": {
    "ApiKey": "sk-xxx",
    "Model": "deepseek-chat",
    "ApiUrl": "https://your-company-proxy.com/ai/deepseek",
    "Enabled": true,
    "Priority": 1
  }
}
```

### **Example 3: Mock Server (Testing)**
```json
{
  "DeepSeek": {
    "ApiKey": "test-key",
    "Model": "deepseek-chat",
    "ApiUrl": "http://localhost:5000/mock/deepseek",
    "Enabled": true,
    "Priority": 1
  }
}
```

---

## ?? API Endpoint Comparison

| Provider | Endpoint | Pattern |
|----------|----------|---------|
| **DeepSeek** | `https://api.deepseek.com/chat/completions` | No `/v1` prefix |
| **OpenAI** | `https://api.openai.com/v1/chat/completions` | Has `/v1` prefix |
| **Claude** | `https://api.anthropic.com/v1/messages` | Has `/v1` prefix |

**Note**: DeepSeek uses a different URL pattern than OpenAI (no `/v1`), which was causing the 404 error before.

---

## ? Verification Steps

### **1. Check Configuration Loading**
```csharp
var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
System.Diagnostics.Debug.WriteLine($"DeepSeek API URL: {config.DeepSeek?.ApiUrl}");

Expected Output:
? DeepSeek API URL: https://api.deepseek.com/chat/completions
```

### **2. Check Service Initialization**
```
Expected Debug Log:
? DeepSeek Parser: Initialized
   Model: deepseek-chat
   API URL: https://api.deepseek.com/chat/completions
```

### **3. Test API Call**
Upload a prescription and check logs:
```
?? DeepSeek Parser: Sending to DeepSeek API...
?? URL: https://api.deepseek.com/chat/completions
? DeepSeek Parser: Response received
```

---

## ?? Migration Guide

### **If you have existing configuration:**

1. **Add `ApiUrl` to each environment:**
```json
"DeepSeek": {
  "ApiKey": "your-existing-key",
  "Model": "deepseek-chat",
  "ApiUrl": "https://api.deepseek.com/chat/completions", // ADD THIS
  "Enabled": true,
  "Priority": 1
}
```

2. **Rebuild the app:**
```bash
dotnet build
```

3. **Test prescription upload**

### **If configuration is missing ApiUrl:**

The service has a **default fallback**:
```csharp
public DeepSeekPrescriptionParserAgent(
    HttpClient httpClient, 
    string apiKey, 
    string apiUrl = "https://api.deepseek.com/chat/completions") // Default
```

So old configurations will still work! ?

---

## ?? Testing Different URLs

### **Test 1: Official API**
```json
"ApiUrl": "https://api.deepseek.com/chat/completions"
```
**Expected**: ? Works with official DeepSeek API

### **Test 2: Invalid URL**
```json
"ApiUrl": "https://api.deepseek.com/invalid"
```
**Expected**: ? Retries 3 times, then fails gracefully

### **Test 3: Local Mock**
```json
"ApiUrl": "http://localhost:5000/mock"
```
**Expected**: ? Calls your local mock server

---

## ?? Best Practices

### ? **DO:**
1. **Always specify ApiUrl** in appsettings.json
2. **Use HTTPS** in production
3. **Test with different environments** (Dev/Staging/Prod)
4. **Log API URL** on initialization for debugging
5. **Document custom URLs** in your deployment guide

### ? **DON'T:**
1. **Don't hardcode URLs** in code anymore
2. **Don't use HTTP** in production (security risk)
3. **Don't forget to update** all environments (Dev/Staging/Prod)
4. **Don't commit sensitive URLs** (use secrets for proxies with auth)

---

## ?? Security Considerations

### **If using a proxy:**
```json
{
  "DeepSeek": {
    "ApiUrl": "https://your-proxy.com/deepseek",
    "ApiKey": "your-proxy-token"
  }
}
```

**Considerations**:
- ? Proxy can log/monitor API calls
- ? Can add rate limiting
- ? Can cache responses
- ?? Proxy has access to all requests/responses
- ?? Ensure proxy uses HTTPS
- ?? Don't commit proxy credentials to Git

---

## ?? Summary Table

| Aspect | Before | After |
|--------|--------|-------|
| **API URL** | Hardcoded in code | Configurable in appsettings.json |
| **Flexibility** | ? Need code change to update | ? Just update config |
| **Environments** | ? Same URL for all | ? Different URLs per environment |
| **Proxy Support** | ? Not possible | ? Easy to configure |
| **Testing** | ? Hard to mock | ? Easy to point to mock server |
| **Deployment** | ? Rebuild for URL change | ? Just update config file |

---

## ? Status

| Check | Status |
|-------|--------|
| **Configuration Models** | ? Updated |
| **appsettings.json** | ? Updated (all environments) |
| **DeepSeekParserAgent** | ? Updated |
| **MauiProgram.cs** | ? Updated |
| **Build** | ? Successful |
| **Backward Compatible** | ? Has default value |

---

**Implementation Complete! ??**

The DeepSeek API URL is now fully configurable through `appsettings.json`, making it easy to:
- Switch between API versions
- Use custom proxies
- Point to mock servers for testing
- Use different URLs per environment

No code changes needed for future API URL updates! ?
