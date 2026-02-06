# Quick Reference - SMS & Email OTP Services

## Summary

SMS and Email logic separated from AuthenticationService into dedicated communication services with feature flags.

## New Files Created

```
? backend\MedRemind.Core\Interfaces\ISmsService.cs
? backend\MedRemind.Core\Interfaces\IEmailService.cs
? backend\MedRemind.Services\Communication\SmsService.cs
? backend\MedRemind.Services\Communication\EmailService.cs
```

## Configuration Changes

### Feature Flags (appsettings.json)

```json
"Features": {
  "EnableSmsOtp": false,     // Toggle SMS OTP
  "EnableEmailOtp": true      // Toggle Email OTP
}
```

### Email Configuration (NEW)

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

## Quick Setup

### 1. Configure Gmail SMTP

```bash
# Enable 2-Step Verification
# Generate App Password: Google Account ? Security ? App passwords
# Update appsettings.json with credentials
```

### 2. Update Feature Flags

**For Email OTP (Development):**
```json
"EnableSmsOtp": false,
"EnableEmailOtp": true
```

**For SMS OTP (Production):**
```json
"EnableSmsOtp": true,
"EnableEmailOtp": false
```

### 3. Test

```bash
# Start API
dotnet run --project backend\MedRemind.API

# Send OTP
POST /api/auth/send-otp
{
  "phoneNumber": "9876543210"
}

# Check email or SMS for OTP
```

## How It Works

```
EnableEmailOtp = true
    ?
User requests OTP
    ?
AuthenticationService ? EmailService ? SMTP ? Email sent

EnableSmsOtp = true
    ?
User requests OTP
    ?
AuthenticationService ? SmsService ? 2Factor.in ? SMS sent
```

## Key Benefits

? **Flexibility** - Switch between SMS and Email via config  
? **Testability** - Mock services easily in tests  
? **Maintainability** - Separated concerns  
? **Extensibility** - Easy to add WhatsApp, Telegram, etc.

## Testing

**Unit Test with Mock:**
```csharp
var mockEmailService = new Mock<IEmailService>();
mockEmailService.Setup(s => s.SendOtpAsync(...)).ReturnsAsync((true, null));

var authService = new AuthenticationService(
    unitOfWork, secureStorage,
    smsService: null,
    emailService: mockEmailService.Object,
    useEmail: true);
```

## Environment Settings

| Environment | SMS OTP | Email OTP | Why |
|-------------|---------|-----------|-----|
| Development | ? No | ? Yes | Testing with email easier |
| Staging | ? Yes | ? No | Test real SMS |
| Production | ? Yes | ? No | Use SMS for real users |

## SMTP Providers

### Gmail
```json
"SmtpHost": "smtp.gmail.com",
"SmtpPort": 587
```

### Outlook
```json
"SmtpHost": "smtp.office365.com",
"SmtpPort": 587
```

### SendGrid
```json
"SmtpHost": "smtp.sendgrid.net",
"SmtpPort": 587,
"SmtpUsername": "apikey"
```

## Common Issues

### Email not sending?
- ? Check SMTP credentials
- ? Generate Gmail app password
- ? Check `EnableEmailOtp` = true
- ? Verify user has email

### SMS not sending?
- ? Check 2Factor.in API key
- ? Check `EnableSmsOtp` = true
- ? Verify phone format (10 digits)

## Files Modified

- ? `backend\MedRemind.Services\Authentication\AuthenticationService.cs`
- ? `backend\MedRemind.API\Program.cs`
- ? `backend\MedRemind.API\appsettings.Development.json`
- ? `backend\MedRemind.Tests\Services\AuthenticationServiceTests.cs`
- ? `mobile\MedRemind.Mobile\MauiProgram.cs`

## Build Status

? **Build Successful** - All changes compiled

---

**Full Documentation:** See `COMMUNICATION_SERVICES_REFACTORING.md`
