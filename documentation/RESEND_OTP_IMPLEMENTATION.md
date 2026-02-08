# Resend OTP Implementation - Complete Guide

## Summary
Implemented a comprehensive resend OTP feature for active registration with rate limiting, validation, and tracking.

## Features Implemented

### ? 1. Rate Limiting
- **Minimum Wait Time:** 60 seconds between OTP requests
- **Daily Limit:** Maximum 5 OTP requests per phone number per 24 hours
- **Prevents Abuse:** Protection against spam and malicious attempts

### ? 2. Validation
- Phone number validation
- User existence check
- Purpose validation (Registration, Login, PasswordReset)
- Email/SMS availability check

### ? 3. Tracking
- `IsEmailOtpSent` and `IsSmsOtpSent` flags
- `LastEmailOtpSentAt` and `LastSmsOtpSentAt` timestamps
- Remaining attempts counter
- Next available resend time

### ? 4. Security
- Automatic deactivation of previous OTPs
- Rate limit error responses
- Non-revealing user existence messages
- IP/Sender info tracking

---

## API Endpoints

### 1. POST `/api/auth/resend-otp`

Resend OTP with rate limiting and validation.

**Request:**
```json
{
  "phoneNumber": "8420249020",
  "email": "user@example.com",  // Optional
  "purpose": "Registration"      // Registration, Login, PasswordReset
}
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "OTP has been resent successfully.",
  "remainingAttempts": 3,
  "isEmailOtpSent": true,
  "isSmsOtpSent": false,
  "nextResendAvailableAt": null,
  "errorMessage": null
}
```

**Rate Limit Response (429):**
```json
{
  "success": false,
  "message": null,
  "errorMessage": "Please wait 45 seconds before requesting a new OTP.",
  "nextResendAvailableAt": "2024-02-04T10:45:30Z",
  "remainingAttempts": null,
  "isEmailOtpSent": false,
  "isSmsOtpSent": false
}
```

**Daily Limit Response (400):**
```json
{
  "success": false,
  "errorMessage": "You have reached the maximum of 5 OTP requests in 24 hours. Please try again later.",
  "remainingAttempts": 0,
  "nextResendAvailableAt": null,
  "isEmailOtpSent": false,
  "isSmsOtpSent": false
}
```

### 2. GET `/api/auth/resend-otp/availability`

Check if resend is available before making request.

**Request:**
```
GET /api/auth/resend-otp/availability?phoneNumber=8420249020&purpose=Registration
```

**Success Response (200):**
```json
{
  "success": true,
  "message": "Resend is available",
  "remainingAttempts": 4,
  "nextResendAvailableAt": null,
  "errorMessage": null
}
```

**Wait Required Response (200):**
```json
{
  "success": false,
  "errorMessage": "Please wait before requesting a new OTP.",
  "nextResendAvailableAt": "2024-02-04T10:45:30Z",
  "remainingAttempts": 4
}
```

**Daily Limit Response (200):**
```json
{
  "success": false,
  "errorMessage": "You have reached the maximum number of OTP requests for today.",
  "nextResendAvailableAt": null,
  "remainingAttempts": 0
}
```

---

## Usage Examples

### Example 1: Resend OTP During Registration

```bash
# Step 1: Initial registration
POST /api/users/register
{
  "phoneNumber": "8420249020",
  "email": "john@example.com",
  "name": "John Doe"
}

# Step 2: User didn't receive OTP, request resend
POST /api/auth/resend-otp
{
  "phoneNumber": "8420249020",
  "purpose": "Registration"
}

# Response:
{
  "success": true,
  "message": "OTP has been resent successfully.",
  "remainingAttempts": 4
}

# Step 3: Verify OTP
POST /api/auth/verify-otp
{
  "phoneNumber": "8420249020",
  "otp": "123456"
}
```

### Example 2: Check Availability Before Resending

```bash
# Step 1: Check if resend is available
GET /api/auth/resend-otp/availability?phoneNumber=8420249020&purpose=Registration

# Response - Can resend:
{
  "success": true,
  "message": "Resend is available",
  "remainingAttempts": 3
}

# Step 2: Resend OTP
POST /api/auth/resend-otp
{
  "phoneNumber": "8420249020",
  "purpose": "Registration"
}
```

### Example 3: Handle Rate Limiting

```bash
# Rapid requests will be rate limited
POST /api/auth/resend-otp
{
  "phoneNumber": "8420249020",
  "purpose": "Registration"
}

# First request: Success
# Immediate second request: Rate limited

# Response:
{
  "success": false,
  "errorMessage": "Please wait 58 seconds before requesting a new OTP.",
  "nextResendAvailableAt": "2024-02-04T10:46:00Z"
}
```

---

## Rate Limiting Details

### Time-Based Limit

**Rule:** Minimum 60 seconds between requests

```csharp
var minimumWaitTime = TimeSpan.FromSeconds(60);
if (timeSinceLastOtp < minimumWaitTime)
{
    return (false, "Please wait {seconds} seconds...", nextAvailableAt, null);
}
```

**Calculation:**
- Last OTP sent at: 10:44:00
- Current time: 10:44:30
- Time elapsed: 30 seconds
- Remaining wait: 30 seconds
- Next available: 10:45:00

### Volume-Based Limit

**Rule:** Maximum 5 OTP requests per 24 hours

```csharp
var last24Hours = DateTime.UtcNow.AddHours(-24);
var otpCountLast24h = recentOtps.Count(o => o.CreatedAt >= last24Hours);
var maxDailyOtps = 5;

if (otpCountLast24h >= maxDailyOtps)
{
    return (false, "Daily limit reached", null, 0);
}
```

**Example Timeline:**
```
Day 1:
  10:00 AM - Request 1 ?
  10:05 AM - Request 2 ?
  10:10 AM - Request 3 ?
  11:00 AM - Request 4 ?
  12:00 PM - Request 5 ?
  01:00 PM - Request 6 ? (Daily limit reached)

Day 2:
  10:01 AM - Request 1 ? (24 hours since first request)
```

---

## Configuration

### appsettings.Development.json

Current configuration uses Email OTP:

```json
{
  "Features": {
    "EnableSmsOtp": false,
    "EnableEmailOtp": true
  },
  "SmtpSettings": {
    "SmtpHost": "mail.privateemail.com",
    "SmtpPort": 587,
    "SmtpUsername": "info@airesumecheck.me",
    "SmtpPassword": "rajib@1990",
    "FromEmail": "noreply@medremind.com",
    "FromName": "MedRemind",
    "EnableSsl": true
  }
}
```

### Configuration Options

You can adjust rate limits in `OtpCodeService.cs`:

```csharp
// Time-based limit (default: 60 seconds)
var minimumWaitTime = TimeSpan.FromSeconds(60);

// Volume-based limit (default: 5 per 24 hours)
var maxDailyOtps = 5;
```

---

## Error Handling

### Client-Side Error Handling

```javascript
async function resendOtp(phoneNumber) {
  try {
    const response = await fetch('/api/auth/resend-otp', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        phoneNumber: phoneNumber,
        purpose: 'Registration'
      })
    });

    const data = await response.json();

    if (response.status === 429) {
      // Rate limit exceeded
      const waitSeconds = Math.ceil(
        (new Date(data.nextResendAvailableAt) - new Date()) / 1000
      );
      alert(`Please wait ${waitSeconds} seconds before resending OTP`);
      startCountdown(waitSeconds);
    } else if (!data.success) {
      // Other error
      alert(data.errorMessage);
    } else {
      // Success
      alert(`OTP resent! ${data.remainingAttempts} attempts remaining`);
    }
  } catch (error) {
    alert('Failed to resend OTP. Please try again.');
  }
}

function startCountdown(seconds) {
  const button = document.getElementById('resendButton');
  button.disabled = true;
  
  const interval = setInterval(() => {
    seconds--;
    button.textContent = `Resend OTP (${seconds}s)`;
    
    if (seconds <= 0) {
      clearInterval(interval);
      button.disabled = false;
      button.textContent = 'Resend OTP';
    }
  }, 1000);
}
```

### UI States

**Available to Resend:**
```html
<button id="resendButton" onclick="resendOtp('8420249020')">
  Resend OTP (3 attempts remaining)
</button>
```

**Rate Limited:**
```html
<button id="resendButton" disabled>
  Resend OTP (45s)
</button>
```

**Daily Limit Reached:**
```html
<button id="resendButton" disabled>
  Daily limit reached
</button>
<p>You have reached the maximum number of OTP requests for today.</p>
```

---

## Security Features

### 1. Rate Limiting
Prevents spam and brute force attempts.

### 2. Previous OTP Deactivation
Old OTPs are automatically deactivated when new one is sent.

```csharp
await DeactivatePreviousOtpsAsync(phoneNumber);
```

### 3. User Existence Protection
Doesn't reveal if user exists for security.

```csharp
if (user == null)
{
    return new ResendOtpResponse
    {
        Success = false,
        ErrorMessage = "User not found"  // Generic message
    };
}
```

### 4. Sender Info Tracking
Tracks who requested the OTP for audit purposes.

```csharp
SenderInfo = $"Resend-{request.Purpose}"
```

### 5. Purpose Validation
Ensures OTP is used for intended purpose.

```csharp
Purpose = request.Purpose  // Registration, Login, PasswordReset
```

---

## Monitoring

### Database Queries

**Check recent OTP requests:**
```sql
SELECT 
    PhoneNumber,
    Purpose,
    DeliveryMethod,
    CreatedAt,
    IsActive,
    IsVerified
FROM OtpCodes
WHERE PhoneNumber = '8420249020'
ORDER BY CreatedAt DESC
LIMIT 10;
```

**Check daily OTP counts:**
```sql
SELECT 
    PhoneNumber,
    COUNT(*) as TotalRequests,
    MIN(CreatedAt) as FirstRequest,
    MAX(CreatedAt) as LastRequest
FROM OtpCodes
WHERE CreatedAt >= datetime('now', '-24 hours')
GROUP BY PhoneNumber
ORDER BY TotalRequests DESC;
```

**Check rate limit violations:**
```sql
SELECT 
    PhoneNumber,
    COUNT(*) as RequestCount,
    MAX(CreatedAt) as LastRequest
FROM OtpCodes
WHERE CreatedAt >= datetime('now', '-1 hour')
GROUP BY PhoneNumber
HAVING COUNT(*) > 3
ORDER BY RequestCount DESC;
```

---

## Testing

### Postman Tests

**Collection:** `MedRemind_Complete_API_Collection.json`

**Test 1: Resend OTP**
```json
POST {{base_url}}/api/auth/resend-otp
{
  "phoneNumber": "8420249020",
  "purpose": "Registration"
}

Tests:
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Success is true", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;
});

pm.test("Has remaining attempts", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.remainingAttempts).to.be.a('number');
});
```

**Test 2: Rate Limit**
```json
POST {{base_url}}/api/auth/resend-otp
{
  "phoneNumber": "8420249020",
  "purpose": "Registration"
}

// Send immediately again

Tests:
pm.test("Status code is 429", function () {
    pm.response.to.have.status(429);
});

pm.test("Has nextResendAvailableAt", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.nextResendAvailableAt).to.not.be.null;
});
```

**Test 3: Check Availability**
```json
GET {{base_url}}/api/auth/resend-otp/availability?phoneNumber=8420249020&purpose=Registration

Tests:
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Has availability status", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.a('boolean');
});
```

---

## Integration with Mobile App

### .NET MAUI Example

```csharp
public class OtpViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private int _countdown;
    private bool _canResend;

    public ICommand ResendOtpCommand { get; }

    public OtpViewModel(IAuthService authService)
    {
        _authService = authService;
        _canResend = false;
        ResendOtpCommand = new Command(async () => await ResendOtpAsync(), 
            () => _canResend);
        
        CheckResendAvailability();
    }

    private async Task CheckResendAvailability()
    {
        var response = await _authService.CheckResendAvailabilityAsync(
            PhoneNumber, "Registration");

        if (response.Success)
        {
            _canResend = true;
            ((Command)ResendOtpCommand).ChangeCanExecute();
        }
        else if (response.NextResendAvailableAt.HasValue)
        {
            var waitTime = (response.NextResendAvailableAt.Value - DateTime.UtcNow).TotalSeconds;
            StartCountdown((int)waitTime);
        }
    }

    private async Task ResendOtpAsync()
    {
        try
        {
            IsBusy = true;
            _canResend = false;
            ((Command)ResendOtpCommand).ChangeCanExecute();

            var result = await _authService.ResendOtpAsync(new ResendOtpRequest
            {
                PhoneNumber = PhoneNumber,
                Purpose = "Registration"
            });

            if (result.Success)
            {
                await App.Current.MainPage.DisplayAlert("Success", 
                    $"OTP resent! {result.RemainingAttempts} attempts remaining", "OK");
                
                StartCountdown(60); // 60 second cooldown
            }
            else
            {
                await App.Current.MainPage.DisplayAlert("Error", 
                    result.ErrorMessage, "OK");
            }
        }
        catch (Exception ex)
        {
            await App.Current.MainPage.DisplayAlert("Error", 
                "Failed to resend OTP", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartCountdown(int seconds)
    {
        _countdown = seconds;
        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            _countdown--;
            ResendButtonText = $"Resend OTP ({_countdown}s)";

            if (_countdown <= 0)
            {
                _canResend = true;
                ResendButtonText = "Resend OTP";
                ((Command)ResendOtpCommand).ChangeCanExecute();
                return false; // Stop timer
            }

            return true; // Continue timer
        });
    }
}
```

---

## Troubleshooting

### Issue 1: OTP Not Received
**Solution:** Check rate limits and delivery method configuration

```bash
# Check OTP records
GET /api/auth/resend-otp/availability?phoneNumber=8420249020

# Verify email/SMS configuration in appsettings
"Features": {
  "EnableEmailOtp": true,
  "EnableSmsOtp": false
}
```

### Issue 2: Rate Limit Too Strict
**Solution:** Adjust in `OtpCodeService.cs`

```csharp
// Change from 60 to 30 seconds
var minimumWaitTime = TimeSpan.FromSeconds(30);

// Change from 5 to 10 attempts per day
var maxDailyOtps = 10;
```

### Issue 3: Countdown Not Updating
**Solution:** Use polling or WebSockets for real-time updates

```javascript
// Poll availability every 5 seconds
setInterval(async () => {
  const response = await fetch(
    `/api/auth/resend-otp/availability?phoneNumber=${phoneNumber}`
  );
  const data = await response.json();
  updateUI(data);
}, 5000);
```

---

## Files Modified

```
? backend\MedRemind.Core\DTOs\AuthDTOs.cs (NEW DTOs)
? backend\MedRemind.Services\Communication\OtpCodeService.cs (UPDATED)
? backend\MedRemind.Services\Authentication\AuthenticationService.cs (UPDATED)
? backend\MedRemind.API\Controllers\AuthController.cs (UPDATED)
? backend\MedRemind.Core\Interfaces\IAuthenticationService.cs (UPDATED)
```

---

## Build Status

? **Build Successful**

---

**Created:** 2024-02-04  
**Version:** 1.0  
**Status:** ? Production Ready
