# ?? "Software caused connection abort" - Android Network Fix

## Problem

```
? AI processing failed: OpenAI Vision fallback failed: Software caused connection abort
```

This error indicates a **network connection issue** on Android, typically caused by:
1. ? Emulator network configuration
2. ? Firewall/antivirus blocking connections
3. ? SSL/TLS certificate issues
4. ? Network timeout

---

## ?? Root Cause

### **Android Emulator Specifics**

Android emulators have special network requirements:
- Can't use `localhost` directly
- Need `10.0.2.2` for host machine
- May have SSL certificate validation issues
- May need cleartextTraffic enabled

---

## ? Solutions (In Order of Likelihood)

### **Solution 1: Add Network Security Configuration (Most Likely Fix)**

Android may be blocking HTTPS connections due to certificate issues.

#### **Step 1: Create network_security_config.xml**

Create file: `mobile/MedRemind.Mobile/Platforms/Android/Resources/xml/network_security_config.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
    <!-- Trust user-installed CA certificates -->
    <base-config cleartextTrafficPermitted="false">
        <trust-anchors>
            <!-- Trust preinstalled CAs -->
            <certificates src="system" />
            <!-- Additionally trust user added CAs -->
            <certificates src="user" />
        </trust-anchors>
    </base-config>
    
    <!-- For development/testing only - allow all connections -->
    <domain-config cleartextTrafficPermitted="true">
        <domain includeSubdomains="true">api.openai.com</domain>
        <domain includeSubdomains="true">cognitiveservices.azure.com</domain>
        <domain includeSubdomains="true">documentintelligence.azure.com</domain>
    </domain-config>
</network-security-config>
```

#### **Step 2: Update AndroidManifest.xml**

Add to `<application>` tag:

```xml
<application
    ...
    android:networkSecurityConfig="@xml/network_security_config">
```

---

### **Solution 2: Update HttpClient Configuration**

Update `MauiProgram.cs` to configure HttpClient for Android:

```csharp
// In MauiProgram.cs
builder.Services.AddSingleton<HttpClient>(sp =>
{
    var httpClient = new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            // For development only - log certificate issues
            if (errors != System.Net.Security.SslPolicyErrors.None)
            {
                System.Diagnostics.Debug.WriteLine($"?? SSL Error: {errors}");
            }
            // In production, validate properly. For now, accept all for testing.
            return true; // TEMPORARY - remove in production!
        }
    })
    {
        Timeout = TimeSpan.FromSeconds(120) // Increased timeout
    };

#if DEBUG
    // Log all HTTP requests in debug mode
    httpClient.DefaultRequestHeaders.Add("User-Agent", "MedRemind-Mobile/1.0");
#endif

    return httpClient;
});
```

?? **Warning**: The certificate validation bypass is **only for development/testing**. Remove in production!

---

### **Solution 3: Check Emulator Network**

#### **Verify Internet Access**

1. Open browser in emulator
2. Navigate to https://www.google.com
3. If fails ? Emulator network issue

#### **Restart Emulator with Proper Network**

```sh
# Stop emulator
# Start with DNS settings
emulator -avd YOUR_AVD_NAME -dns-server 8.8.8.8
```

#### **Check Emulator Network Settings**

```sh
# In emulator terminal (adb shell)
adb shell

# Check connectivity
ping 8.8.8.8
ping api.openai.com

# Check DNS
nslookup api.openai.com
```

---

### **Solution 4: Use Physical Device**

If emulator continues to have issues:

1. Enable USB Debugging on physical Android device
2. Connect device via USB
3. Deploy app to device
4. Test - physical devices usually have better network connectivity

---

### **Solution 5: Add Connection Retry Logic**

Update services to handle transient network errors:

```csharp
// In AzureDocumentIntelligenceService.cs
public async Task<string> ExtractTextFromImageAsync(string base64Image, CancellationToken cancellationToken = default)
{
    const int maxRetries = 3;
    int attempt = 0;

    while (attempt < maxRetries)
    {
        try
        {
            attempt++;
            System.Diagnostics.Debug.WriteLine($"?? Azure DI: Attempt {attempt}/{maxRetries}");

            // ... existing code ...

            return extractedText;
        }
        catch (RequestFailedException ex) when (attempt < maxRetries)
        {
            System.Diagnostics.Debug.WriteLine($"?? Attempt {attempt} failed, retrying...");
            await Task.Delay(TimeSpan.FromSeconds(2 * attempt)); // Exponential backoff
            
            if (attempt == maxRetries)
                throw;
        }
    }

    throw new Exception("All retry attempts failed");
}
```

---

## ?? Diagnostic Steps

### **Step 1: Check Logs for Specific Error**

Look for more details in Output window:

```
Should see one of:
- "Connection refused"
- "Network unreachable"  
- "SSL handshake failed"
- "Connection reset"
- "Connection abort"
```

### **Step 2: Test Basic Connectivity**

Add a test endpoint:

```csharp
// Add to ViewModel
private async Task TestConnectivityAsync()
{
    try
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        
        // Test 1: Google (should always work)
        System.Diagnostics.Debug.WriteLine("Testing Google...");
        var response1 = await httpClient.GetAsync("https://www.google.com");
        System.Diagnostics.Debug.WriteLine($"? Google: {response1.StatusCode}");
        
        // Test 2: OpenAI (check if reachable)
        System.Diagnostics.Debug.WriteLine("Testing OpenAI...");
        var response2 = await httpClient.GetAsync("https://api.openai.com");
        System.Diagnostics.Debug.WriteLine($"? OpenAI: {response2.StatusCode}");
        
        // Test 3: Azure
        System.Diagnostics.Debug.WriteLine("Testing Azure...");
        var response3 = await httpClient.GetAsync("https://azure.microsoft.com");
        System.Diagnostics.Debug.WriteLine($"? Azure: {response3.StatusCode}");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"? Connectivity test failed: {ex.Message}");
    }
}
```

### **Step 3: Check Android Logs**

```sh
# Filter for network errors
adb logcat | grep -i "network\|ssl\|certificate"

# Check system errors
adb logcat *:E
```

---

## ?? Quick Fixes Checklist

### **Fix 1: Network Security Config** ?
- [ ] Create `network_security_config.xml`
- [ ] Add domain exceptions for OpenAI and Azure
- [ ] Update `AndroidManifest.xml`
- [ ] Rebuild app
- [ ] Test

### **Fix 2: Permissions** ?
Ensure in `AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
```

### **Fix 3: Clear Cache** ?
```sh
# Clear app data
adb shell pm clear com.medremind.app

# Or in emulator:
Settings ? Apps ? MedRemind ? Storage ? Clear Data
```

### **Fix 4: Restart Emulator** ?
```sh
# Close emulator
# Cold boot (not quick boot)
emulator -avd YOUR_AVD -no-snapshot-load
```

---

## ?? Expected Results

### **Before Fix** ?
```
?? Azure DI: Submitting document...
? Azure DI: Error: Software caused connection abort
?? Fallback: Using OpenAI Vision API...
? OpenAI Vision fallback failed: Software caused connection abort
```

### **After Fix** ?
```
?? Azure DI: Submitting document...
? Azure DI: Analysis completed
? Text extracted: 487 characters
```

---

## ?? Implementation

Let me create the network security config file:

### **File 1: network_security_config.xml**

```xml
<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
    <base-config cleartextTrafficPermitted="false">
        <trust-anchors>
            <certificates src="system" />
            <certificates src="user" />
        </trust-anchors>
    </base-config>
    
    <!-- Allow connections to AI services -->
    <domain-config cleartextTrafficPermitted="false">
        <domain includeSubdomains="true">api.openai.com</domain>
        <domain includeSubdomains="true">cognitiveservices.azure.com</domain>
        <domain includeSubdomains="true">2factor.in</domain>
        <trust-anchors>
            <certificates src="system" />
            <certificates src="user" />
        </trust-anchors>
    </domain-config>
</network-security-config>
```

### **File 2: Update AndroidManifest.xml**

Add this attribute to `<application>`:

```xml
<application
    ...
    android:networkSecurityConfig="@xml/network_security_config"
    android:usesCleartextTraffic="false">
```

---

## ?? Most Likely Solution

Based on your error, the **most likely fix** is:

1. ? **Create network_security_config.xml** (shown above)
2. ? **Update AndroidManifest.xml** to reference it
3. ? **Rebuild and redeploy**

This fixes Android's strict SSL/TLS validation that may be blocking connections to OpenAI and Azure.

---

## ?? If Still Fails

### **Option 1: Use Physical Device**

Emulators can have persistent network issues. Physical devices work better.

### **Option 2: Check Firewall**

Windows Defender or corporate firewall may block emulator network:
- Open Windows Security
- Firewall & network protection
- Allow an app through firewall
- Find and enable your emulator

### **Option 3: Disable Antivirus Temporarily**

Some antivirus software blocks emulator HTTPS connections:
- Disable temporarily
- Test app
- If works, add exception for emulator

---

## ? Summary

**Root Cause**: Android network security blocking HTTPS connections to Azure/OpenAI

**Solutions**:
1. ? Create network_security_config.xml (most important)
2. ? Update AndroidManifest.xml
3. ? Increase HttpClient timeout
4. ? Add retry logic
5. ? Test on physical device

**Status**: Ready to implement

---

Let me create the files now...
