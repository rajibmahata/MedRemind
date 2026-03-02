# ?? MedRemind Configuration System - Complete Guide

## ? **What's Been Created**

### **1. Configuration Files** ?
- `appsettings.json` - Development configuration (enhanced)
- `appsettings.Production.json` - Production configuration
- All API endpoints organized by feature

### **2. Strongly-Typed Classes** ?
- `AppConfiguration.cs` - Complete configuration model
- Type-safe access to all settings
- Helper methods for URL building

### **3. Configuration Service** ?
- `IConfigurationService` / `ConfigurationService`
- Centralized access point
- Registered in `Program.cs`

---

## ?? **How to Use**

### **Method 1: Using ConfigurationService (RECOMMENDED)**

```csharp
@inject IConfigurationService Config

@code {
    protected override async Task OnInitializedAsync()
    {
        // Access API base URL
        var baseUrl = Config.ApiSettings.BaseUrl;
        
        // Get full endpoint URL
        var registerUrl = Config.GetApiUrl(Config.Endpoints.Auth.Register);
        // Result: "http://localhost:5000/api/users/register"
        
        // Check if feature is enabled
        if (Config.Features.EnableNotifications)
        {
            // Enable notifications
        }
        
        // Get timeout setting
        var timeout = Config.ApiSettings.Timeout; // 120 seconds
    }
}
```

---

### **Method 2: Using AppConfiguration Directly**

```csharp
@inject AppConfiguration AppConfig

@code {
    private string apiBaseUrl;
    private bool darkModeEnabled;
    
    protected override void OnInitialized()
    {
        apiBaseUrl = AppConfig.ApiSettings.BaseUrl;
        darkModeEnabled = AppConfig.Features.EnableDarkMode;
        
        // Access endpoint templates
        var uploadEndpoint = AppConfig.Endpoints.Prescriptions.Upload;
    }
}
```

---

### **Method 3: In Services (Best Practice)**

```csharp
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _config;

    public AuthService(HttpClient httpClient, IConfigurationService config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<bool> RegisterAsync(RegisterModel model)
    {
        // Use endpoint from configuration
        var endpoint = _config.Endpoints.Auth.Register;
        
        var response = await _httpClient.PostAsJsonAsync(endpoint, model);
        return response.IsSuccessStatusCode;
    }

    public async Task<UserProfileData?> GetCurrentUserAsync()
    {
        // Use endpoint from configuration
        var endpoint = _config.Endpoints.Auth.CurrentUser;
        
        return await _httpClient.GetFromJsonAsync<UserProfileData>(endpoint);
    }
}
```

---

## ?? **Complete Usage Examples**

### **Example 1: Authentication Service**

```csharp
// web/MedRemind.Web/Services/AuthService.cs

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _config;
    private readonly ILocalStorageService _localStorage;

    public AuthService(
        HttpClient httpClient, 
        IConfigurationService config,
        ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _config = config;
        _localStorage = localStorage;
    }

    public async Task<bool> RegisterAsync(RegisterModel model)
    {
        var endpoint = _config.Endpoints.Auth.Register;
        var response = await _httpClient.PostAsJsonAsync(endpoint, model);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LoginAsync(string phoneNumber)
    {
        var endpoint = _config.Endpoints.Auth.Login;
        var response = await _httpClient.PostAsJsonAsync(endpoint, new { phoneNumber });
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool success, string? token)> VerifyOtpAsync(string phoneNumber, string otp)
    {
        var endpoint = _config.Endpoints.Auth.VerifyOtp;
        var response = await _httpClient.PostAsJsonAsync(endpoint, new { phoneNumber, otp });
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<OtpVerificationResponse>();
            if (result?.Success == true && !string.IsNullOrEmpty(result.Token))
            {
                // Store token using configured key
                await _localStorage.SetItemAsync(_config.Storage.LocalStorageKey, result.Token);
                return (true, result.Token);
            }
        }
        
        return (false, null);
    }

    public async Task<UserProfileData?> GetCurrentUserAsync()
    {
        var endpoint = _config.Endpoints.Auth.CurrentUser;
        var token = await _localStorage.GetItemAsync(_config.Storage.LocalStorageKey);
        
        if (string.IsNullOrEmpty(token))
            return null;

        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await _httpClient.GetFromJsonAsync<UserProfileData>(endpoint);
    }
}
```

---

### **Example 2: Prescription Service**

```csharp
// web/MedRemind.Web/Services/PrescriptionService.cs

public class PrescriptionService : IPrescriptionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _config;

    public PrescriptionService(HttpClient httpClient, IConfigurationService config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<PrescriptionDto>> GetAllPrescriptionsAsync()
    {
        var endpoint = _config.Endpoints.Prescriptions.List;
        return await _httpClient.GetFromJsonAsync<List<PrescriptionDto>>(endpoint) 
               ?? new List<PrescriptionDto>();
    }

    public async Task<PrescriptionDto?> GetPrescriptionByIdAsync(int id)
    {
        // Use helper method to replace {id}
        var endpoint = _config.Endpoints.Prescriptions.GetDetails(id);
        return await _httpClient.GetFromJsonAsync<PrescriptionDto>(endpoint);
    }

    public async Task<byte[]?> GetPrescriptionImageAsync(int id)
    {
        var endpoint = _config.Endpoints.Prescriptions.GetImage(id);
        return await _httpClient.GetByteArrayAsync(endpoint);
    }

    public async Task<bool> ReprocessPrescriptionAsync(int id)
    {
        var endpoint = _config.Endpoints.Prescriptions.GetReprocess(id);
        var response = await _httpClient.PostAsync(endpoint, null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeletePrescriptionAsync(int id)
    {
        var endpoint = _config.Endpoints.Prescriptions.GetDelete(id);
        var response = await _httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }

    public async Task<PrescriptionStatistics?> GetStatisticsAsync()
    {
        var endpoint = _config.Endpoints.Prescriptions.Statistics;
        return await _httpClient.GetFromJsonAsync<PrescriptionStatistics>(endpoint);
    }
}
```

---

### **Example 3: Medication Service**

```csharp
// web/MedRemind.Web/Services/MedicationService.cs

public class MedicationService : IMedicationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _config;

    public MedicationService(HttpClient httpClient, IConfigurationService config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<MedicationDto>> GetAllMedicationsAsync()
    {
        var endpoint = _config.Endpoints.Medications.List;
        return await _httpClient.GetFromJsonAsync<List<MedicationDto>>(endpoint) 
               ?? new List<MedicationDto>();
    }

    public async Task<List<MedicationDto>> SearchMedicationsAsync(string query)
    {
        // Use helper method to add query parameter
        var endpoint = _config.Endpoints.Medications.GetSearch(query);
        return await _httpClient.GetFromJsonAsync<List<MedicationDto>>(endpoint) 
               ?? new List<MedicationDto>();
    }

    public async Task<List<MedicationDto>> GetByPrescriptionAsync(int prescriptionId)
    {
        var endpoint = _config.Endpoints.Medications.GetByPrescription(prescriptionId);
        return await _httpClient.GetFromJsonAsync<List<MedicationDto>>(endpoint) 
               ?? new List<MedicationDto>();
    }

    public async Task<MedicationDto?> UpdateMedicationAsync(int id, UpdateMedicationRequest request)
    {
        var endpoint = _config.Endpoints.Medications.GetUpdate(id);
        var response = await _httpClient.PutAsJsonAsync(endpoint, request);
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MedicationDto>();
        }
        
        return null;
    }

    public async Task<bool> DeleteMedicationAsync(int id)
    {
        var endpoint = _config.Endpoints.Medications.GetDelete(id);
        var response = await _httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }
}
```

---

### **Example 4: Using in Blazor Pages**

```razor
@page "/prescriptions"
@inject IConfigurationService Config
@inject IPrescriptionService PrescriptionService

<MudContainer MaxWidth="MaxWidth.Large">
    <MudText Typo="Typo.h4">My Prescriptions</MudText>
    
    @if (Config.Features.EnableAnalytics)
    {
        <MudAlert Severity="Severity.Info">Analytics Enabled</MudAlert>
    }
    
    <MudButton OnClick="LoadPrescriptions">
        Load Prescriptions
    </MudButton>
    
    <MudText>API: @Config.ApiSettings.BaseUrl</MudText>
    <MudText>Environment: @Config.AppSettings.Environment</MudText>
</MudContainer>

@code {
    private List<PrescriptionDto> prescriptions = new();
    
    protected override async Task OnInitializedAsync()
    {
        // Check feature flag
        if (!Config.Features.EnableOfflineMode)
        {
            await LoadPrescriptions();
        }
    }
    
    private async Task LoadPrescriptions()
    {
        // Service already uses configuration internally
        prescriptions = await PrescriptionService.GetAllPrescriptionsAsync();
    }
}
```

---

## ?? **Changing API Endpoint**

### **For Development** (Local Testing):
Edit `appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### **For Production**:
Edit `appsettings.Production.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.medremind.com"
  }
}
```

### **For Different Environments**:
Create environment-specific files:
- `appsettings.Development.json` - Local development
- `appsettings.Staging.json` - Staging server
- `appsettings.Production.json` - Production

---

## ?? **Available Settings**

### **API Settings**
```csharp
Config.ApiSettings.BaseUrl               // "http://localhost:5000"
Config.ApiSettings.Timeout               // 120 seconds
Config.ApiSettings.EnableRetry           // true/false
Config.ApiSettings.MaxRetryAttempts      // 3
Config.ApiSettings.RetryDelaySeconds     // 2
```

### **Endpoints**
```csharp
// Authentication
Config.Endpoints.Auth.Register
Config.Endpoints.Auth.Login
Config.Endpoints.Auth.VerifyOtp

// Prescriptions
Config.Endpoints.Prescriptions.Upload
Config.Endpoints.Prescriptions.List
Config.Endpoints.Prescriptions.GetDetails(id)

// Medications
Config.Endpoints.Medications.List
Config.Endpoints.Medications.GetSearch(query)

// Validation
Config.Endpoints.Validation.GetWorkflowUrl(id)
Config.Endpoints.Validation.GetConfirmMedicationUrl(id)

// Voice Recordings
Config.Endpoints.VoiceRecordings.Upload
Config.Endpoints.VoiceRecordings.GetPlay(id)

// Reminders
Config.Endpoints.Reminders.CreateBulk
Config.Endpoints.Reminders.GetByMedication(medicationId)
```

### **Features**
```csharp
Config.Features.EnableNotifications      // true/false
Config.Features.EnableVoiceRecordings    // true/false
Config.Features.EnableDarkMode           // true/false
Config.Features.EnableOfflineMode        // true/false
```

### **Authentication**
```csharp
Config.Authentication.OtpLength          // 6
Config.Authentication.OtpExpiryMinutes   // 5
Config.Authentication.TokenExpiryDays    // 7
```

### **UI Settings**
```csharp
Config.UI.DefaultTheme                   // "light" or "dark"
Config.UI.EnableAnimations               // true/false
Config.UI.ItemsPerPage                   // 10
Config.UI.MaxFileUploadSizeMB            // 10
```

---

## ?? **Best Practices**

### ? **DO**:
1. **Always use ConfigurationService** in services
2. **Use typed endpoints** with helper methods
3. **Check feature flags** before using features
4. **Use environment-specific configs** for deployment

### ? **DON'T**:
1. ~~Hard-code API URLs~~ in services
2. ~~Use magic strings~~ for endpoints
3. ~~Duplicate configuration~~ across files

---

## ?? **Quick Reference**

### **Change API URL**:
```json
// appsettings.json
{
  "ApiSettings": {
    "BaseUrl": "http://your-new-api-url.com"
  }
}
```

### **Access in Code**:
```csharp
@inject IConfigurationService Config

var apiUrl = Config.ApiSettings.BaseUrl;
var registerEndpoint = Config.Endpoints.Auth.Register;
```

### **Build Full URL**:
```csharp
var fullUrl = Config.GetApiUrl("/api/prescriptions");
// Result: "http://localhost:5000/api/prescriptions"
```

---

## ?? **Next Steps**

1. ? Configuration system is ready
2. ? Update existing services to use `IConfigurationService`
3. ? Test with different API endpoints
4. ? Deploy with production settings

---

**Configuration System Status**: ? **READY TO USE**

*Last Updated: February 9, 2024*
