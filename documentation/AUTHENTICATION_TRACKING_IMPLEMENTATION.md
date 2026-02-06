# Authentication Tracking Implementation

## Overview
Added comprehensive authentication tracking to monitor how users authenticate (SMS OTP vs Email OTP) and which verification methods they have completed.

## New Features

### ? 1. AuthenticationMethod Enum
**Location:** `backend\MedRemind.Core\Enums\AuthenticationMethod.cs`

```csharp
public enum AuthenticationMethod
{
    None = 0,       // Not verified yet
    SmsOtp = 1,     // Verified via SMS OTP
    EmailOtp = 2,   // Verified via Email OTP
    Both = 3        // Verified via both SMS and Email
}
```

### ? 2. User Model - New Fields

**Location:** `backend\MedRemind.Core\Models\User.cs`

```csharp
// Authentication tracking
public bool IsEmailVerified { get; set; } = false;
public bool IsPhoneVerified { get; set; } = false;
public AuthenticationMethod AuthenticationMethod { get; set; } = AuthenticationMethod.None;
public AuthenticationMethod LastAuthenticationMethod { get; set; } = AuthenticationMethod.None;
public DateTime? EmailVerifiedAt { get; set; }
public DateTime? PhoneVerifiedAt { get; set; }
```

### ? 3. Field Descriptions

| Field | Type | Description |
|-------|------|-------------|
| `IsEmailVerified` | bool | Has user verified their email via OTP |
| `IsPhoneVerified` | bool | Has user verified their phone via SMS OTP |
| `AuthenticationMethod` | enum | Overall auth method (None/SmsOtp/EmailOtp/Both) |
| `LastAuthenticationMethod` | enum | Method used in last login |
| `EmailVerifiedAt` | DateTime? | When email was verified |
| `PhoneVerifiedAt` | DateTime? | When phone was verified |

## How It Works

### Scenario 1: User Authenticates with SMS OTP

```
1. User requests OTP
   EnableSmsOtp = true
   ?
2. SMS sent to phone number
   ?
3. User enters OTP
   ?
4. AuthenticationService.VerifyOtpAsync()
   ?
5. Updates User:
   - IsPhoneVerified = true
   - PhoneVerifiedAt = DateTime.UtcNow
   - AuthenticationMethod = SmsOtp
   - LastAuthenticationMethod = SmsOtp
```

### Scenario 2: User Authenticates with Email OTP

```
1. User requests OTP
   EnableEmailOtp = true
   ?
2. Email sent to user's email
   ?
3. User enters OTP
   ?
4. AuthenticationService.VerifyOtpAsync()
   ?
5. Updates User:
   - IsEmailVerified = true
   - EmailVerifiedAt = DateTime.UtcNow
   - AuthenticationMethod = EmailOtp
   - LastAuthenticationMethod = EmailOtp
```

### Scenario 3: User Uses Both Methods Over Time

```
First Login (SMS):
   - IsPhoneVerified = true
   - AuthenticationMethod = SmsOtp
   - LastAuthenticationMethod = SmsOtp

Later Login (Email):
   - IsEmailVerified = true
   - AuthenticationMethod = Both (upgraded)
   - LastAuthenticationMethod = EmailOtp
```

## Code Changes

### AuthenticationService.VerifyOtpAsync()

**Before:**
```csharp
// Simple verification
user.LastLoginAt = DateTime.UtcNow;
```

**After:**
```csharp
// Track authentication method
var currentAuthMethod = _useEmail ? AuthenticationMethod.EmailOtp : AuthenticationMethod.SmsOtp;

if (_useEmail)
{
    user.IsEmailVerified = true;
    user.EmailVerifiedAt = DateTime.UtcNow;
}
else
{
    user.IsPhoneVerified = true;
    user.PhoneVerifiedAt = DateTime.UtcNow;
}

user.LastAuthenticationMethod = currentAuthMethod;

// Set overall method (Both if user verified both)
if (user.IsEmailVerified && user.IsPhoneVerified)
{
    user.AuthenticationMethod = AuthenticationMethod.Both;
}
else
{
    user.AuthenticationMethod = currentAuthMethod;
}
```

## API Response Changes

### GET /api/users/me

**Before:**
```json
{
  "id": 1,
  "phoneNumber": "9876543210",
  "email": "user@example.com",
  "name": "John Doe"
}
```

**After:**
```json
{
  "id": 1,
  "phoneNumber": "9876543210",
  "email": "user@example.com",
  "name": "John Doe",
  "isEmailVerified": true,
  "isPhoneVerified": false,
  "authenticationMethod": "EmailOtp",
  "lastAuthenticationMethod": "EmailOtp",
  "emailVerifiedAt": "2024-02-04T10:30:00Z",
  "phoneVerifiedAt": null
}
```

## Database Schema

### Migration Script
**File:** `backend\MedRemind.API\database_scripts\AddAuthenticationTracking.sql`

```sql
-- Add authentication verification flags
ALTER TABLE Users ADD IsEmailVerified INTEGER DEFAULT 0 NOT NULL;
ALTER TABLE Users ADD IsPhoneVerified INTEGER DEFAULT 0 NOT NULL;

-- Add authentication method tracking
ALTER TABLE Users ADD AuthenticationMethod INTEGER DEFAULT 0 NOT NULL;
ALTER TABLE Users ADD LastAuthenticationMethod INTEGER DEFAULT 0 NOT NULL;

-- Add verification timestamps
ALTER TABLE Users ADD EmailVerifiedAt TEXT NULL;
ALTER TABLE Users ADD PhoneVerifiedAt TEXT NULL;

-- Create indexes
CREATE INDEX IX_Users_IsEmailVerified ON Users(IsEmailVerified);
CREATE INDEX IX_Users_IsPhoneVerified ON Users(IsPhoneVerified);
CREATE INDEX IX_Users_AuthenticationMethod ON Users(AuthenticationMethod);
```

## Use Cases

### 1. Check if User Has Verified Email
```csharp
if (user.IsEmailVerified)
{
    // User can receive email notifications
    await SendEmailNotification(user.Email);
}
```

### 2. Check if User Has Verified Phone
```csharp
if (user.IsPhoneVerified)
{
    // User can receive SMS notifications
    await SendSmsNotification(user.PhoneNumber);
}
```

### 3. Require Both Verifications for Sensitive Operations
```csharp
if (user.AuthenticationMethod == AuthenticationMethod.Both)
{
    // Allow sensitive operations
    await ProcessPayment();
}
else
{
    return "Please verify both email and phone for this operation";
}
```

### 4. Track Last Authentication Method
```csharp
if (user.LastAuthenticationMethod == AuthenticationMethod.EmailOtp)
{
    _logger.LogInformation("User logged in via Email OTP");
}
else
{
    _logger.LogInformation("User logged in via SMS OTP");
}
```

## Benefits

### ? 1. Enhanced Security
- Know which verification methods user has completed
- Can require both for sensitive operations
- Track authentication patterns

### ? 2. Better Analytics
- Track which OTP method is more popular
- Monitor verification rates
- Identify authentication trends

### ? 3. Flexible Notifications
- Send notifications via verified channels only
- Offer multiple notification options
- Respect user preferences

### ? 4. Audit Trail
- Know when user verified email/phone
- Track last authentication method
- Historical authentication data

## Testing

### Test Case 1: SMS OTP Login
```bash
# 1. Register user
POST /api/users/register
{
  "phoneNumber": "9876543210",
  "email": "test@example.com",
  "name": "Test User"
}

# 2. Set EnableSmsOtp = true in appsettings.json

# 3. Send OTP
POST /api/auth/send-otp
{
  "phoneNumber": "9876543210"
}

# 4. Verify OTP
POST /api/auth/verify-otp
{
  "phoneNumber": "9876543210",
  "otp": "123456"
}

# 5. Check profile
GET /api/users/me
# Should show:
# isPhoneVerified: true
# authenticationMethod: "SmsOtp"
```

### Test Case 2: Email OTP Login
```bash
# 1. Set EnableEmailOtp = true in appsettings.json

# 2. Send OTP (goes to email)
POST /api/auth/send-otp
{
  "phoneNumber": "9876543210"
}

# 3. Verify OTP
POST /api/auth/verify-otp
{
  "phoneNumber": "9876543210",
  "otp": "123456"
}

# 4. Check profile
GET /api/users/me
# Should show:
# isEmailVerified: true
# authenticationMethod: "EmailOtp"
```

### Test Case 3: Both Methods
```bash
# 1. Login via SMS (first time)
# Result: authenticationMethod = "SmsOtp"

# 2. Switch to Email OTP in config

# 3. Login via Email (second time)
# Result: authenticationMethod = "Both"
```

## Query Examples

### Find Users Verified by Email
```sql
SELECT * FROM Users WHERE IsEmailVerified = 1;
```

### Find Users Verified by Phone
```sql
SELECT * FROM Users WHERE IsPhoneVerified = 1;
```

### Find Users with Both Verifications
```sql
SELECT * FROM Users WHERE AuthenticationMethod = 3; -- Both
```

### Find Recent Email Verifications
```sql
SELECT * FROM Users 
WHERE IsEmailVerified = 1 
  AND EmailVerifiedAt > datetime('now', '-7 days')
ORDER BY EmailVerifiedAt DESC;
```

### Authentication Method Statistics
```sql
SELECT 
  AuthenticationMethod,
  COUNT(*) as UserCount,
  CASE AuthenticationMethod
    WHEN 0 THEN 'None'
    WHEN 1 THEN 'SmsOtp'
    WHEN 2 THEN 'EmailOtp'
    WHEN 3 THEN 'Both'
  END as MethodName
FROM Users
GROUP BY AuthenticationMethod;
```

## Migration Steps

### 1. Run Database Migration
```powershell
.\run-database-migrations.ps1 -SingleScript "AddAuthenticationTracking.sql"
```

### 2. Restart API
```bash
dotnet run --project backend\MedRemind.API
```

### 3. Test Authentication
```bash
# Test SMS OTP
POST /api/auth/send-otp + verify-otp

# Check tracking
GET /api/users/me
```

## Backward Compatibility

? **No Breaking Changes**
- Existing users will have default values (all false)
- API still works as before
- New fields are optional in responses
- Existing authentication flow unchanged

## Future Enhancements

Potential additions:

- [ ] Require re-verification after X days
- [ ] Send verification reminders
- [ ] Multi-factor authentication (require both)
- [ ] Social login tracking (Google, Facebook, etc.)
- [ ] Device-based authentication tracking
- [ ] Location-based verification alerts

## Troubleshooting

### Issue: Fields not showing in API response
**Solution:** Run database migration to add columns

### Issue: Authentication method always "None"
**Solution:** Ensure users login via OTP after migration

### Issue: Both verifications not detected
**Solution:** User must login via both SMS and Email OTP over time

---

**Last Updated:** 2024-02-04  
**Version:** 1.0  
**Build Status:** ? Successful
