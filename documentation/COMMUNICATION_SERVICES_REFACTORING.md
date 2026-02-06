# Communication Services Refactoring - Complete Guide

## Overview
The authentication OTP functionality has been refactored to separate SMS and Email communication logic into dedicated services under a new `Communication` folder. This provides better separation of concerns and flexibility in choosing OTP delivery methods.

## What Changed?

### Before: Monolithic Authentication Service
- ? AuthenticationService handled both authentication AND SMS communication
- ? Tightly coupled to 2Factor.in SMS API
- ? No email OTP support
- ? Hard to test and maintain

### After: Separated Communication Services
- ? `SmsService` handles SMS communication
- ? `EmailService` handles email communication
- ? `AuthenticationService` only handles authentication logic
- ? Feature flags to choose SMS or Email OTP
- ? Easy to test with mocking
- ? Loosely coupled design

## New Structure

```
backend\MedRemind.Services\
??? Communication\
?   ??? SmsService.cs          (NEW)
?   ??? EmailService.cs        (NEW)
??? Authentication\
    ??? AuthenticationService.cs (REFACTORED)

backend\MedRemind.Core\
??? Interfaces\
    ??? ISmsService.cs         (NEW)
    ??? IEmailService.cs       (NEW)
```

## Services Created

### 1. ISmsService Interface
**Location:** `backend\MedRemind.Core\Interfaces\ISmsService.cs`

```csharp
public interface ISmsService
{
    Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string phoneNumber, 
        string otp, 
        CancellationToken cancellationToken = default);
}
```

### 2. IEmailService Interface
**Location:** `backend\MedRemind.Core\Interfaces\IEmailService.cs`

```csharp
public interface IEmailService
{
    Task<(bool Success, string? ErrorMessage)> SendOtpAsync(
        string email, 
        string otp, 
        CancellationToken cancellationToken = default);
}
```

### 3. SmsService
**Location:** `backend\MedRemind.Services\Communication\SmsService.cs`

**Features:**
- Sends OTP via SMS using 2Factor.in API
- Phone number validation (10 digits)
- Automatic country code formatting (+91)
- Error handling and logging

**Example Usage:**
```csharp
var smsService = new SmsService(httpClient, apiKey, sendUrl, template);
var (success, error) = await smsService.SendOtpAsync("9876543210", "123456");
```

### 4. EmailService
**Location:** `backend\MedRemind.Services\Communication\EmailService.cs`

**Features:**
- Sends OTP via Email using SMTP
- HTML email template
- Email validation
- SSL support
- Configurable SMTP settings

**Example Usage:**
```csharp
var emailService = new EmailService(smtpHost, smtpPort, username, password, fromEmail, fromName);
var (success, error) = await emailService.SendOtpAsync("user@example.com", "123456");
```

### 5. Refactored AuthenticationService
**Location:** `backend\MedRemind.Services\Authentication\AuthenticationService.cs`

**Key Changes:**
- No longer handles SMS/Email logic directly
- Depends on ISmsService and IEmailService
- Uses feature flag to determine which service to use
- Simplified and focused on authentication only

**Constructor:**
```csharp
public AuthenticationService(
    IUnitOfWork unitOfWork,
    ISecureStorageService secureStorage,
    ISmsService? smsService = null,
    IEmailService? emailService = null,
    bool useEmail = false,
    string? jwtSecretKey = null,
    string? jwtIssuer = null,
    string? jwtAudience = null,
    int jwtExpirationDays = 30)
```

## Configuration

### appsettings.Development.json

**SMTP Configuration (Renamed from "Email" to "SmtpSettings"):**
```json
"SmtpSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "your-email@gmail.com",
  "SmtpPassword": "your-app-password",
  "FromEmail": "noreply@medremind.com",
  "FromName": "MedRemind",
  "EnableSsl": true
}
```

**Feature Flags (UPDATED):**
```json
"Features": {
  "EnablePrescriptionUpload": true,
  "EnableVoiceReminders": true,
  "EnableAnalytics": false,
  "EnableCrashReporting": false,
  "EnableSmsOtp": false,      // ? NEW - Toggle SMS OTP
  "EnableEmailOtp": true       // ? NEW - Toggle Email OTP
}
```

### Environment-Specific Settings

**Development:**
- SMS OTP: Disabled
- Email OTP: Enabled (for testing)

**Staging/Production:**
- SMS OTP: Enabled (for real users)
- Email OTP: Disabled

## Service Registration (Program.cs)

```csharp
// Register SMS Service
builder.Services.AddScoped<ISmsService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var twoFactorApiKey = environmentConfig["TwoFactor:ApiKey"] ?? "";
    var sendOtpUrl = environmentConfig["TwoFactor:SendOtpUrl"];
    var otpTemplate = environmentConfig["TwoFactor:OtpTemplate"] ?? "OTP1";
    var logger = sp.GetService<ILogger<SmsService>>();
    
    return new SmsService(httpClient, twoFactorApiKey, sendOtpUrl, otpTemplate, logger);
});

// Register Email Service  
builder.Services.AddScoped<IEmailService>(sp =>
{
    var smtpHost = environmentConfig["SmtpSettings:SmtpHost"] ?? "smtp.gmail.com";
    var smtpPort = int.Parse(environmentConfig["SmtpSettings:SmtpPort"] ?? "587");
    var smtpUsername = environmentConfig["SmtpSettings:SmtpUsername"] ?? "";
    var smtpPassword = environmentConfig["SmtpSettings:SmtpPassword"] ?? "";
    var fromEmail = environmentConfig["SmtpSettings:FromEmail"] ?? "noreply@medremind.com";
    var fromName = environmentConfig["SmtpSettings:FromName"] ?? "MedRemind";
    var enableSsl = bool.Parse(environmentConfig["SmtpSettings:EnableSsl"] ?? "true");
    var logger = sp.GetService<ILogger<EmailService>>();
    
    return new EmailService(smtpHost, smtpPort, smtpUsername, smtpPassword, fromEmail, fromName, enableSsl, logger);
});

// Register AuthenticationService
builder.Services.AddScoped<IAuthenticationService>(sp =>
{
    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
    var secureStorage = sp.GetRequiredService<ISecureStorageService>();
    var smsService = sp.GetService<ISmsService>();
    var emailService = sp.GetService<IEmailService>();
    
    // Check which OTP service is enabled
    var useEmail = bool.Parse(environmentConfig["Features:EnableEmailOtp"] ?? "false");
    
    return new AuthenticationService(
        unitOfWork, 
        secureStorage,
        smsService: smsService,
        emailService: emailService,
        useEmail: useEmail);
});
```

## How It Works

### SMS OTP Flow

```
1. User requests OTP
   ?
2. AuthenticationService.SendOtpAsync()
   ?
3. Check Feature Flag: EnableSmsOtp = true
   ?
4. Call SmsService.SendOtpAsync(phoneNumber, otp)
   ?
5. SmsService calls 2Factor.in API
   ?
6. User receives SMS with OTP
```

### Email OTP Flow

```
1. User requests OTP
   ?
2. AuthenticationService.SendOtpAsync()
   ?
3. Check Feature Flag: EnableEmailOtp = true
   ?
4. Get user email from database
   ?
5. Call EmailService.SendOtpAsync(email, otp)
   ?
6. EmailService sends via SMTP
   ?
7. User receives Email with OTP
```

## Email Template

The email OTP includes a professional HTML template:

```html
<!DOCTYPE html>
<html>
<head>
    <style>
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #4CAF50; color: white; padding: 20px; }
        .otp-box { font-size: 32px; font-weight: bold; letter-spacing: 8px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>MedRemind</h1>
        </div>
        <div class="content">
            <h2>Your OTP Code</h2>
            <p>You have requested to verify your account. Please use the following OTP code:</p>
            <div class="otp-box">123456</div>
            <p><strong>This code is valid for 10 minutes.</strong></p>
        </div>
    </div>
</body>
</html>
```

## Configuration Guide

### Gmail SMTP Setup

1. **Enable 2-Step Verification** in your Google Account
2. **Generate App Password**:
   - Go to Google Account Settings
   - Security ? 2-Step Verification ? App passwords
   - Generate app password for "Mail"
3. **Update appsettings.json**:
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "your-email@gmail.com",
  "SmtpPassword": "xxxx xxxx xxxx xxxx",  // App password
  "FromEmail": "your-email@gmail.com",
  "FromName": "MedRemind",
  "EnableSsl": true
}
```

### Other SMTP Providers

**Outlook/Office 365:**
```json
"SmtpSettings": {
  "SmtpHost": "smtp.office365.com",
  "SmtpPort": 587,
  "SmtpUsername": "your-email@outlook.com",
  "SmtpPassword": "your-password",
  "EnableSsl": true
}
```

**SendGrid:**
```json
"SmtpSettings": {
  "SmtpHost": "smtp.sendgrid.net",
  "SmtpPort": 587,
  "SmtpUsername": "apikey",
  "SmtpPassword": "YOUR_SENDGRID_API_KEY",
  "EnableSsl": true
}
```

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task SendOtpAsync_WithEmailEnabled_ShouldSendViaEmail()
{
    // Arrange
    var mockEmailService = new Mock<IEmailService>();
    mockEmailService.Setup(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((true, null));
    
    var authService = new AuthenticationService(
        unitOfWork,
        secureStorage,
        smsService: null,
        emailService: mockEmailService.Object,
        useEmail: true);
    
    // Act
    var result = await authService.SendOtpAsync("9876543210");
    
    // Assert
    Assert.True(result.Success);
    mockEmailService.Verify(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

## Benefits

### 1. ? Separation of Concerns
- AuthenticationService focuses on authentication
- Communication services handle OTP delivery
- Clear responsibility boundaries

### 2. ? Flexibility
- Easy to switch between SMS and Email
- Feature flags for runtime configuration
- Can add more providers (WhatsApp, Telegram, etc.)

### 3. ? Testability
- Services can be mocked easily
- Unit tests don't need real SMS/Email APIs
- Independent testing of each component

### 4. ? Maintainability
- Changes to SMS provider don't affect authentication logic
- Email configuration changes isolated
- Easier to debug and troubleshoot

### 5. ? Extensibility
- Easy to add new OTP providers
- Can support multiple delivery methods simultaneously
- Future-proof architecture

## Migration Checklist

- [x] Create ISmsService and IEmailService interfaces
- [x] Implement SmsService (2Factor.in)
- [x] Implement EmailService (SMTP)
- [x] Refactor AuthenticationService
- [x] Update appsettings.json (all environments)
- [x] Update Program.cs service registration
- [x] Fix unit tests
- [x] Fix mobile app (MauiProgram.cs)
- [x] Build successful
- [ ] Configure SMTP settings
- [ ] Test SMS OTP
- [ ] Test Email OTP
- [ ] Update documentation

## Next Steps

1. **Configure SMTP Settings** - Update with real email credentials
2. **Test Email OTP** - Send test OTP via email
3. **Test SMS OTP** - Verify SMS still works
4. **Update Mobile App** - Add email OTP support
5. **Production Deployment** - Choose SMS or Email based on requirements

## Troubleshooting

### Issue: Email OTP not sending
**Check:**
1. SMTP credentials are correct
2. App password is generated (for Gmail)
3. `EnableEmailOtp` feature flag is true
4. User has email registered

### Issue: SMS OTP not sending
**Check:**
1. 2Factor.in API key is valid
2. `EnableSmsOtp` feature flag is true
3. Phone number format is correct (10 digits)

### Issue: Both SMS and Email disabled
**Solution:** Enable at least one OTP method in Features configuration

---

**Last Updated:** 2024-02-04  
**Version:** 2.0  
**Build Status:** ? Successful
