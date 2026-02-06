# Quick Reference - Authentication Tracking

## Summary
Track which authentication method users use (SMS OTP or Email OTP) with verification flags and timestamps.

## New Fields in User Model

```csharp
public bool IsEmailVerified { get; set; }              // Email verified?
public bool IsPhoneVerified { get; set; }              // Phone verified?
public AuthenticationMethod AuthenticationMethod { get; set; }  // Overall method
public AuthenticationMethod LastAuthenticationMethod { get; set; } // Last login method
public DateTime? EmailVerifiedAt { get; set; }         // When email verified
public DateTime? PhoneVerifiedAt { get; set; }         // When phone verified
```

## Authentication Methods

```csharp
public enum AuthenticationMethod
{
    None = 0,       // Not verified
    SmsOtp = 1,     // SMS OTP only
    EmailOtp = 2,   // Email OTP only
    Both = 3        // Both methods used
}
```

## Quick Setup

### 1. Run Migration
```powershell
.\run-database-migrations.ps1 -SingleScript "AddAuthenticationTracking.sql"
```

### 2. Test
```bash
# Login via SMS
POST /api/auth/verify-otp
Result: isPhoneVerified = true, authenticationMethod = "SmsOtp"

# Login via Email (later)
POST /api/auth/verify-otp  
Result: isEmailVerified = true, authenticationMethod = "Both"
```

## API Response

**GET /api/users/me:**
```json
{
  "id": 1,
  "phoneNumber": "9876543210",
  "email": "user@example.com",
  "isEmailVerified": true,
  "isPhoneVerified": false,
  "authenticationMethod": "EmailOtp",
  "lastAuthenticationMethod": "EmailOtp",
  "emailVerifiedAt": "2024-02-04T10:30:00Z",
  "phoneVerifiedAt": null
}
```

## Common Queries

### Users Verified by Email
```sql
SELECT * FROM Users WHERE IsEmailVerified = 1;
```

### Users Verified by Phone
```sql
SELECT * FROM Users WHERE IsPhoneVerified = 1;
```

### Users with Both Verifications
```sql
SELECT * FROM Users WHERE AuthenticationMethod = 3;
```

### Authentication Statistics
```sql
SELECT AuthenticationMethod, COUNT(*) as Count
FROM Users
GROUP BY AuthenticationMethod;
```

## Use Cases

### Check Email Verified
```csharp
if (user.IsEmailVerified)
{
    await SendEmailNotification(user.Email);
}
```

### Check Phone Verified
```csharp
if (user.IsPhoneVerified)
{
    await SendSmsNotification(user.PhoneNumber);
}
```

### Require Both for Sensitive Operations
```csharp
if (user.AuthenticationMethod != AuthenticationMethod.Both)
{
    return "Please verify both email and phone";
}
```

## Files Modified

```
? backend\MedRemind.Core\Enums\AuthenticationMethod.cs (NEW)
? backend\MedRemind.Core\Models\User.cs
? backend\MedRemind.Core\DTOs\UserDTOs.cs
? backend\MedRemind.Services\Users\UserService.cs
? backend\MedRemind.Services\Authentication\AuthenticationService.cs
? backend\MedRemind.Core\Data\MedRemindDbContext.cs
? backend\MedRemind.API\database_scripts\AddAuthenticationTracking.sql (NEW)
```

## Benefits

? **Security** - Know which verifications completed  
? **Analytics** - Track authentication patterns  
? **Flexibility** - Support multiple auth methods  
? **Audit Trail** - Historical authentication data  

## Build Status

? **Build Successful**

---

**Full Documentation:** See `AUTHENTICATION_TRACKING_IMPLEMENTATION.md`
