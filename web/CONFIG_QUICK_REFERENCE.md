# ? MedRemind Configuration - Quick Reference Card

## ?? **Change API Endpoint (3 Easy Ways)**

### **Method 1: Edit appsettings.json (Recommended)**
```json
// File: web/MedRemind.Web/wwwroot/appsettings.json
{
  "ApiSettings": {
    "BaseUrl": "http://your-api-url-here:5000"
  }
}
```

### **Method 2: Use Environment-Specific Config**
```json
// Development: appsettings.json
"BaseUrl": "http://localhost:5000"

// Production: appsettings.Production.json
"BaseUrl": "https://api.medremind.com"
```

### **Method 3: Runtime Configuration**
```csharp
@inject IConfigurationService Config

var currentUrl = Config.ApiSettings.BaseUrl;
// Change programmatically if needed
```

---

## ?? **Use in Your Code**

### **In Blazor Pages**:
```csharp
@inject IConfigurationService Config

@code {
    var apiUrl = Config.ApiSettings.BaseUrl;
    var registerUrl = Config.Endpoints.Auth.Register;
    
    // Check feature flags
    if (Config.Features.EnableNotifications) {
        // Enable notifications
    }
}
```

### **In Services**:
```csharp
public class MyService
{
    private readonly IConfigurationService _config;
    
    public MyService(IConfigurationService config)
    {
        _config = config;
    }
    
    public async Task DoSomething()
    {
        var endpoint = _config.Endpoints.Prescriptions.List;
        // Use endpoint for API call
    }
}
```

---

## ?? **All Available Endpoints**

### **Authentication**
```csharp
Config.Endpoints.Auth.Register           // /api/users/register
Config.Endpoints.Auth.Login              // /api/auth/login
Config.Endpoints.Auth.VerifyOtp          // /api/auth/verify-otp
Config.Endpoints.Auth.ResendOtp          // /api/auth/resend-otp
Config.Endpoints.Auth.CurrentUser        // /api/users/me
```

### **Prescriptions**
```csharp
Config.Endpoints.Prescriptions.Upload                    // /api/prescriptions/upload-base64
Config.Endpoints.Prescriptions.List                      // /api/prescriptions
Config.Endpoints.Prescriptions.GetDetails(id)            // /api/prescriptions/{id}
Config.Endpoints.Prescriptions.GetImage(id)              // /api/prescriptions/{id}/image
Config.Endpoints.Prescriptions.GetReprocess(id)          // /api/prescriptions/{id}/reprocess
Config.Endpoints.Prescriptions.GetDelete(id)             // /api/prescriptions/{id}
Config.Endpoints.Prescriptions.Statistics                // /api/prescriptions/statistics
```

### **Medications**
```csharp
Config.Endpoints.Medications.List                        // /api/medications
Config.Endpoints.Medications.GetDetails(id)              // /api/medications/{id}
Config.Endpoints.Medications.GetSearch(query)            // /api/medications/search?query={query}
Config.Endpoints.Medications.GetUpdate(id)               // /api/medications/{id}
Config.Endpoints.Medications.GetDelete(id)               // /api/medications/{id}
Config.Endpoints.Medications.GetByPrescription(id)       // /api/medications/prescription/{id}
```

### **Validation**
```csharp
Config.Endpoints.Validation.GetWorkflowUrl(id)           // /api/validation/workflow/{id}
Config.Endpoints.Validation.GetConfirmMedicationUrl(id)  // /api/validation/medication/{id}/confirm
Config.Endpoints.Validation.GetCorrectMedicationUrl(id)  // /api/validation/medication/{id}/correct
Config.Endpoints.Validation.GetDeleteMedicationUrl(id)   // /api/validation/medication/{id}
Config.Endpoints.Validation.GetCompleteWorkflowUrl(id)   // /api/validation/workflow/{id}/complete
```

### **Voice Recordings**
```csharp
Config.Endpoints.VoiceRecordings.Upload                  // /api/voice-recordings/upload-base64
Config.Endpoints.VoiceRecordings.List                    // /api/voice-recordings
Config.Endpoints.VoiceRecordings.GetPlay(id)             // /api/voice-recordings/{id}/play
Config.Endpoints.VoiceRecordings.GetPlaybackUrl(id)      // /api/voice-recordings/{id}/playback-url
Config.Endpoints.VoiceRecordings.GetDelete(id)           // /api/voice-recordings/{id}
```

### **Reminders**
```csharp
Config.Endpoints.Reminders.List                          // /api/reminders
Config.Endpoints.Reminders.CreateBulk                    // /api/reminders/bulk
Config.Endpoints.Reminders.GetToggle(id)                 // /api/reminders/{id}/toggle
Config.Endpoints.Reminders.GetDelete(id)                 // /api/reminders/{id}
Config.Endpoints.Reminders.CalculateTimes                // /api/reminders/calculate-times
Config.Endpoints.Reminders.Upcoming                      // /api/reminders/upcoming
Config.Endpoints.Reminders.GetByMedication(medicationId) // /api/reminders/medication/{id}
```

---

## ??? **All Configuration Options**

### **API Settings**
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000",      // Change this for different environments
    "Timeout": 120,                           // Request timeout in seconds
    "EnableRetry": true,                      // Enable automatic retries
    "MaxRetryAttempts": 3,                    // Number of retry attempts
    "RetryDelaySeconds": 2,                   // Delay between retries
    "EnableLogging": true                     // Enable HTTP logging
  }
}
```

### **Feature Flags**
```json
{
  "Features": {
    "EnableNotifications": true,              // Enable/disable push notifications
    "EnableVoiceRecordings": true,            // Enable/disable voice recording feature
    "EnableDarkMode": true,                   // Enable/disable dark mode
    "EnableOfflineMode": false,               // Enable/disable offline support
    "EnableAnalytics": false                  // Enable/disable analytics
  }
}
```

### **Authentication**
```json
{
  "Authentication": {
    "OtpLength": 6,                           // OTP code length
    "OtpExpiryMinutes": 5,                    // OTP expiry time
    "TokenExpiryDays": 7,                     // JWT token expiry
    "RequireEmailVerification": true          // Require email verification
  }
}
```

### **UI Settings**
```json
{
  "UI": {
    "DefaultTheme": "light",                  // "light" or "dark"
    "EnableAnimations": true,                 // Enable/disable animations
    "ItemsPerPage": 10,                       // Pagination size
    "MaxFileUploadSizeMB": 10                 // Max file upload size
  }
}
```

---

## ?? **Common Tasks**

### **Task 1: Change API URL for Testing**
1. Open `web/MedRemind.Web/wwwroot/appsettings.json`
2. Change `BaseUrl` to your test server:
```json
"BaseUrl": "http://192.168.1.100:5000"
```
3. Save and reload the app

### **Task 2: Deploy to Production**
1. Update `appsettings.Production.json`:
```json
"BaseUrl": "https://api.medremind.com"
```
2. Build for production:
```bash
dotnet publish -c Release
```

### **Task 3: Enable/Disable Features**
```json
// Disable notifications for testing
"Features": {
  "EnableNotifications": false
}
```

### **Task 4: Use Different Environments**
Create new config files:
- `appsettings.Development.json` ? Local
- `appsettings.Staging.json` ? Staging server
- `appsettings.Production.json` ? Production

---

## ?? **Files Created**

? `web/MedRemind.Web/wwwroot/appsettings.json` - Main config (updated)
? `web/MedRemind.Web/Models/AppConfiguration.cs` - Typed config classes
? `web/MedRemind.Web/Services/ConfigurationService.cs` - Config service
? `web/MedRemind.Web/Program.cs` - Service registration (updated)

---

## ?? **Pro Tips**

1. **Never hard-code URLs** - Always use `Config.Endpoints`
2. **Use feature flags** - Check `Config.Features` before using features
3. **Environment-specific configs** - Use different files for dev/staging/prod
4. **Type safety** - Use strongly-typed configuration, not magic strings

---

## ?? **Important Notes**

- After changing `appsettings.json`, **reload the browser** (Ctrl+F5)
- For production, **always use HTTPS** in `BaseUrl`
- **Don't commit** sensitive data (API keys, secrets) to config files
- Use **environment variables** for secrets in production

---

## ?? **Troubleshooting**

**Problem**: Changes not reflected
**Solution**: Hard refresh browser (Ctrl+F5)

**Problem**: 404 Not Found errors
**Solution**: Verify `BaseUrl` is correct and backend is running

**Problem**: CORS errors
**Solution**: Check backend CORS configuration allows your web URL

---

## ?? **Full Documentation**

See `web/CONFIGURATION_SYSTEM_GUIDE.md` for complete examples and advanced usage.

---

**Status**: ? Ready to use
**Location**: `web/MedRemind.Web/wwwroot/appsettings.json`
**Usage**: `@inject IConfigurationService Config`
