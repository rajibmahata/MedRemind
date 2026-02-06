# OTP Code Collection System - Implementation Complete

## Summary
Implemented a comprehensive OTP management system with code collection, automatic expiration, and configuration-based sending (SMS, Email, or Both).

## What Was Created

### ? 1. OtpCode Model (`OtpCode.cs`)
Database entity for storing OTP codes with:
- Phone number & email
- 6-digit OTP code
- Purpose (Registration, Login, etc.)
- Delivery method (SMS, Email, Both)
- Active/Verified flags
- Expiration (10 minutes)
- Attempt tracking (max 3 attempts)
- Sender info (IP address/device)

### ? 2. OtpCodeService (`OtpCodeService.cs`)
Manages OTP lifecycle:
- **GenerateAndSendOtpAsync()** - Creates OTP and sends via SMS/Email/Both
- **VerifyOtpAsync()** - Verifies OTP code with attempt tracking
- **CleanupExpiredOtpsAsync()** - Removes expired OTPs

### ? 3. Background Cleanup Service (`OtpCleanupBackgroundService.cs`)
Automatic cleanup every 5 minutes:
- Removes OTPs older than 15 minutes
- Runs in background continuously
- No manual intervention needed

### ? 4. Database Migration Scripts
- **CreateOtpCodesTable.sql** - Creates OtpCodes table with indexes
- **CleanupExpiredOtps.sql** - Manual cleanup script

### ? 5. Updated Registration Flow
- Authentication flags set as null/false during registration
- Post-registration OTP sent automatically
- SMS and/or Email based on configuration

## How It Works

### Registration Flow
```
1. User registers
   POST /api/users/register
   ?
2. User created with:
   IsEmailVerified = false
   IsPhoneVerified = false
   AuthenticationMethod = None
   ?
3. OTP automatically generated and sent
   (SMS, Email, or Both based on config)
   ?
4. OTP stored in OtpCodes table
   Expires in 10 minutes
   ?
5. User receives OTP
   Via SMS and/or Email
```

### Login Flow with OTP Verification
```
1. User requests OTP
   POST /api/auth/send-otp
   ?
2. OTP generated and stored in database
   ?
3. Previous OTPs deactivated
   ?
4. OTP sent via configured method(s)
   ?
5. User enters OTP
   POST /api/auth/verify-otp
   ?
6. OTP verified against database:
   - Check expiration (10 min)
   - Check attempts (max 3)
   - Validate code
   ?
7. If valid:
   - Mark as verified
   - Update user authentication flags
   - Generate JWT token
   - Deactivate OTP
```

### Automatic Cleanup (Every 5 Minutes)
```
Background Service runs every 5 min
   ?
Finds OTPs older than 15 minutes
   ?
Deletes expired OTPs from database
   ?
Logs cleanup statistics
```

## Configuration

### Features Settings (appsettings.json)
```json
"Features": {
  "EnableSmsOtp": true,    // Enable SMS OTP
  "EnableEmailOtp": false   // Enable Email OTP
}
```

**Delivery Methods:**
- `EnableSmsOtp: true, EnableEmailOtp: false` ? SMS only
- `EnableSmsOtp: false, EnableEmailOtp: true` ? Email only
- `EnableSmsOtp: true, EnableEmailOtp: true` ? Both SMS and Email

## Database Schema

### OtpCodes Table
```sql
CREATE TABLE OtpCodes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PhoneNumber TEXT NOT NULL,
    Email TEXT NULL,
    UserId INTEGER NULL,
    Code TEXT NOT NULL,
    Purpose TEXT NOT NULL,
    DeliveryMethod TEXT NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    IsVerified INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    ExpiresAt TEXT NOT NULL,
    VerifiedAt TEXT NULL,
    AttemptCount INTEGER NOT NULL DEFAULT 0,
    MaxAttempts INTEGER NOT NULL DEFAULT 3,
    SenderInfo TEXT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

## API Changes

### POST /api/users/register
**Before:**
```json
// Registration only
```

**After:**
```json
// Registration + automatic OTP sending
// OTP sent via SMS, Email, or Both
```

### POST /api/auth/send-otp
**Before:**
```
// Generated OTP in memory
// Not stored anywhere
```

**After:**
```
// OTP stored in database
// Expires in 10 minutes
// Tracks attempts
```

### POST /api/auth/verify-otp
**Before:**
```
// Simple verification
// No expiration check
// No attempt limiting
```

**After:**
```
// Verified against database
// Expiration checking (10 min)
// Attempt limiting (max 3)
// Automatic deactivation
```

## Security Features

### ? 1. Expiration
- OTPs expire after 10 minutes
- Automatic cleanup after 15 minutes
- Cannot use expired OTPs

### ? 2. Attempt Limiting
- Maximum 3 verification attempts
- OTP deactivated after max attempts
- Must request new OTP

### ? 3. Single Use
- OTP deactivated after successful verification
- Cannot reuse verified OTP
- Previous OTPs deactivated on new request

### ? 4. Audit Trail
- Tracks creation time
- Tracks verification time
- Tracks attempt count
- Stores sender info (IP/device)

## Testing

### Test OTP Flow
```bash
# 1. Register user
POST /api/users/register
{
  "phoneNumber": "9876543210",
  "email": "test@example.com",
  "name": "Test User"
}
# Result: OTP automatically sent

# 2. Check OTP in database
SELECT * FROM OtpCodes WHERE PhoneNumber = '9876543210';

# 3. Request OTP for login
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
```

## Migration Steps

### 1. Run Migrations
```powershell
# Create OtpCodes table
.\run-database-migrations.ps1 -SingleScript "CreateOtpCodesTable.sql"

# Add authentication tracking
.\run-database-migrations.ps1 -SingleScript "AddAuthenticationTracking.sql"
```

### 2. Restart API
```bash
dotnet run --project backend\MedRemind.API
```

### 3. Test Registration
```bash
POST /api/users/register
# OTP will be sent automatically
```

## Query Examples

### Active OTPs
```sql
SELECT * FROM OtpCodes WHERE IsActive = 1;
```

### Expired OTPs
```sql
SELECT * FROM OtpCodes 
WHERE datetime(ExpiresAt) < datetime('now');
```

### OTP Statistics
```sql
SELECT 
    Purpose,
    DeliveryMethod,
    COUNT(*) as Count,
    AVG(AttemptCount) as AvgAttempts
FROM OtpCodes
GROUP BY Purpose, DeliveryMethod;
```

### Failed Verifications
```sql
SELECT * FROM OtpCodes 
WHERE AttemptCount >= MaxAttempts;
```

## Benefits

### ? 1. Secure OTP Management
- Centralized storage
- Expiration handling
- Attempt limiting
- Audit trail

### ? 2. Flexible Delivery
- SMS only
- Email only
- Both SMS and Email
- Easy to add new methods

### ? 3. Automatic Cleanup
- No manual intervention
- Runs every 5 minutes
- Keeps database clean

### ? 4. Better User Experience
- Automatic OTP after registration
- Clear error messages
- Attempt counter
- Reliable delivery

### ? 5. Audit and Analytics
- Track delivery methods
- Monitor success rates
- Identify patterns
- Security insights

## Build Status

? **Build Successful**

## Files Created

```
? backend\MedRemind.Core\Models\OtpCode.cs
? backend\MedRemind.Services\Communication\OtpCodeService.cs
? backend\MedRemind.API\BackgroundServices\OtpCleanupBackgroundService.cs
? backend\MedRemind.API\database_scripts\CreateOtpCodesTable.sql
? backend\MedRemind.API\database_scripts\CleanupExpiredOtps.sql
```

## Files Modified

```
? backend\MedRemind.Services\Users\UserService.cs
? backend\MedRemind.API\Controllers\UsersController.cs
? backend\MedRemind.Services\Authentication\AuthenticationService.cs
? backend\MedRemind.Core\Data\MedRemindDbContext.cs
? backend\MedRemind.API\Program.cs
? mobile\MedRemind.Mobile\MauiProgram.cs
? backend\MedRemind.Tests\Services\AuthenticationServiceTests.cs
```

---

**Last Updated:** 2024-02-04  
**Version:** 1.0  
**Status:** ? Complete & Production Ready
