# OTP Delivery Tracking Implementation - Complete Guide

## Summary
Added OTP delivery tracking flags to the User model to track whether email/SMS OTP has been sent to users. This helps monitor OTP delivery status and user engagement.

## What Was Implemented

### ? 1. New Fields in User Model
**File:** `backend\MedRemind.Core\Models\User.cs`

Added 4 new fields in the Authentication tracking section:

```csharp
// OTP Delivery Tracking
public bool IsEmailOtpSent { get; set; } = false;
public bool IsSmsOtpSent { get; set; } = false;
public DateTime? LastEmailOtpSentAt { get; set; }
public DateTime? LastSmsOtpSentAt { get; set; }
```

**Field Descriptions:**
- `IsEmailOtpSent` - Boolean flag indicating if email OTP was sent
- `IsSmsOtpSent` - Boolean flag indicating if SMS OTP was sent
- `LastEmailOtpSentAt` - Timestamp when email OTP was last sent
- `LastSmsOtpSentAt` - Timestamp when SMS OTP was last sent

### ? 2. Updated DbContext Configuration
**File:** `backend\MedRemind.Core\Data\MedRemindDbContext.cs`

Added indexes and configurations:
```csharp
entity.HasIndex(e => e.IsEmailOtpSent);
entity.HasIndex(e => e.IsSmsOtpSent);
entity.Property(e => e.IsEmailOtpSent).IsRequired();
entity.Property(e => e.IsSmsOtpSent).IsRequired();
```

### ? 3. Updated OtpCodeService
**File:** `backend\MedRemind.Services\Communication\OtpCodeService.cs`

Added automatic flag updating:
```csharp
// After sending OTP successfully
if (userId.HasValue)
{
    await UpdateUserOtpSentFlagsAsync(userId.Value, deliveryMethod);
}
```

**New Method:**
```csharp
private async Task UpdateUserOtpSentFlagsAsync(int userId, string deliveryMethod)
{
    // Updates IsEmailOtpSent and/or IsSmsOtpSent based on delivery method
    // Updates LastEmailOtpSentAt and/or LastSmsOtpSentAt timestamps
}
```

### ? 4. Updated DTOs
**File:** `backend\MedRemind.Core\DTOs\UserDTOs.cs`

Added fields to `UserProfileData`:
```csharp
// OTP Delivery Tracking
public bool IsEmailOtpSent { get; set; }
public bool IsSmsOtpSent { get; set; }
public DateTime? LastEmailOtpSentAt { get; set; }
public DateTime? LastSmsOtpSentAt { get; set; }
```

### ? 5. Updated Service Mappings
Updated mapping in both:
- `UserService.MapToProfileData()`
- `AuthenticationService.MapToProfileData()`

### ? 6. Database Migration Script
**File:** `backend\MedRemind.API\database_scripts\AddOtpDeliveryTracking.sql`

```sql
ALTER TABLE Users ADD IsEmailOtpSent INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Users ADD IsSmsOtpSent INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Users ADD LastEmailOtpSentAt TEXT NULL;
ALTER TABLE Users ADD LastSmsOtpSentAt TEXT NULL;

CREATE INDEX IX_Users_IsEmailOtpSent ON Users(IsEmailOtpSent);
CREATE INDEX IX_Users_IsSmsOtpSent ON Users(IsSmsOtpSent);
```

---

## How It Works

### Scenario 1: Send OTP via Email Only

```
1. User requests OTP
   ?
2. OtpCodeService.GenerateAndSendOtpAsync()
   DeliveryMethod = "Email"
   ?
3. Email sent successfully
   ?
4. UpdateUserOtpSentFlagsAsync() called
   ?
5. User flags updated:
   IsEmailOtpSent = true
   LastEmailOtpSentAt = DateTime.UtcNow
```

### Scenario 2: Send OTP via SMS Only

```
1. User requests OTP
   ?
2. OtpCodeService.GenerateAndSendOtpAsync()
   DeliveryMethod = "SMS"
   ?
3. SMS sent successfully
   ?
4. UpdateUserOtpSentFlagsAsync() called
   ?
5. User flags updated:
   IsSmsOtpSent = true
   LastSmsOtpSentAt = DateTime.UtcNow
```

### Scenario 3: Send OTP via Both Email and SMS

```
1. User requests OTP
   ?
2. OtpCodeService.GenerateAndSendOtpAsync()
   DeliveryMethod = "Both"
   ?
3. Email + SMS sent successfully
   ?
4. UpdateUserOtpSentFlagsAsync() called
   ?
5. User flags updated:
   IsEmailOtpSent = true
   IsSmsOtpSent = true
   LastEmailOtpSentAt = DateTime.UtcNow
   LastSmsOtpSentAt = DateTime.UtcNow
```

---

## Database Migration

### Run Migration

```powershell
# Windows PowerShell
.\run-database-migrations.ps1 -SingleScript "AddOtpDeliveryTracking.sql"

# Or batch
.\run-database-migrations.bat AddOtpDeliveryTracking.sql
```

### Verify Migration

```sql
-- Check table structure
PRAGMA table_info(Users);

-- Verify new columns exist
SELECT 
    Id, 
    PhoneNumber, 
    Email,
    IsEmailOtpSent,
    IsSmsOtpSent,
    LastEmailOtpSentAt,
    LastSmsOtpSentAt
FROM Users
LIMIT 5;
```

---

## API Response Examples

### Before Implementation

```json
{
  "id": 1,
  "email": "john@example.com",
  "phoneNumber": "8420249020",
  "isEmailVerified": false,
  "isPhoneVerified": false,
  "authenticationMethod": "None"
}
```

### After Implementation

```json
{
  "id": 1,
  "email": "john@example.com",
  "phoneNumber": "8420249020",
  "isEmailVerified": false,
  "isPhoneVerified": false,
  "isEmailOtpSent": true,
  "isSmsOtpSent": true,
  "lastEmailOtpSentAt": "2024-02-04T10:30:00Z",
  "lastSmsOtpSentAt": "2024-02-04T10:30:00Z",
  "authenticationMethod": "None"
}
```

---

## Usage Examples

### Check if User Received OTP

```csharp
var user = await _userService.GetUserByIdAsync(userId);

if (user.IsEmailOtpSent)
{
    Console.WriteLine($"Email OTP sent at: {user.LastEmailOtpSentAt}");
}

if (user.IsSmsOtpSent)
{
    Console.WriteLine($"SMS OTP sent at: {user.LastSmsOtpSentAt}");
}
```

### Query Users Who Received Email OTP

```csharp
var usersWithEmailOtp = await _unitOfWork.Repository<User>()
    .FindAsync(u => u.IsEmailOtpSent == true);
```

### Query Users Who Haven't Received Any OTP

```csharp
var usersWithoutOtp = await _unitOfWork.Repository<User>()
    .FindAsync(u => u.IsEmailOtpSent == false && u.IsSmsOtpSent == false);
```

### Find Users Who Received OTP Recently

```csharp
var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

var recentEmailOtps = await _unitOfWork.Repository<User>()
    .FindAsync(u => u.LastEmailOtpSentAt >= fiveMinutesAgo);
```

---

## Benefits

### ? 1. Delivery Tracking
- Know exactly when OTP was sent
- Track which delivery method was used
- Monitor delivery patterns

### ? 2. User Support
- Quickly verify if user received OTP
- Check delivery method used
- Troubleshoot delivery issues

### ? 3. Analytics
- Track email vs SMS usage
- Monitor OTP delivery rates
- Identify delivery problems

### ? 4. Debugging
- Verify OTP flow completion
- Check timestamp of last OTP
- Audit OTP delivery attempts

---

## Testing

### Test Scenario 1: Email OTP

```bash
# 1. Request OTP via Email
POST /api/auth/send-otp
{
  "phoneNumber": "8420249020"
}
# Assuming config uses Email delivery

# 2. Get User Profile
GET /api/users/me
Authorization: Bearer <token>

# Expected Response:
{
  "isEmailOtpSent": true,
  "isSmsOtpSent": false,
  "lastEmailOtpSentAt": "2024-02-04T10:30:00Z",
  "lastSmsOtpSentAt": null
}
```

### Test Scenario 2: SMS OTP

```bash
# 1. Request OTP via SMS
POST /api/auth/send-otp
{
  "phoneNumber": "8420249020"
}
# Assuming config uses SMS delivery

# 2. Get User Profile
GET /api/users/me

# Expected Response:
{
  "isEmailOtpSent": false,
  "isSmsOtpSent": true,
  "lastEmailOtpSentAt": null,
  "lastSmsOtpSentAt": "2024-02-04T10:30:00Z"
}
```

### Test Scenario 3: Both Email and SMS

```bash
# 1. Request OTP via Both
POST /api/auth/send-otp
{
  "phoneNumber": "8420249020"
}
# Assuming config uses Both delivery

# 2. Get User Profile
GET /api/users/me

# Expected Response:
{
  "isEmailOtpSent": true,
  "isSmsOtpSent": true,
  "lastEmailOtpSentAt": "2024-02-04T10:30:00Z",
  "lastSmsOtpSentAt": "2024-02-04T10:30:00Z"
}
```

---

## Configuration

### OTP Delivery Methods

Update in `appsettings.Development.json`:

```json
{
  "OtpService": {
    "DeliveryMethod": "Both",  // "Email", "SMS", or "Both"
    "ExpirationMinutes": 10
  }
}
```

**Delivery Methods:**
- `"Email"` - Only email, sets `IsEmailOtpSent = true`
- `"SMS"` - Only SMS, sets `IsSmsOtpSent = true`
- `"Both"` - Email + SMS, sets both flags to `true`

---

## Monitoring Queries

### Users Who Received OTP Today

```sql
SELECT 
    Id,
    Email,
    PhoneNumber,
    IsEmailOtpSent,
    IsSmsOtpSent,
    LastEmailOtpSentAt,
    LastSmsOtpSentAt
FROM Users
WHERE 
    DATE(LastEmailOtpSentAt) = DATE('now')
    OR DATE(LastSmsOtpSentAt) = DATE('now');
```

### Email OTP Delivery Rate

```sql
SELECT 
    COUNT(*) as TotalUsers,
    SUM(CASE WHEN IsEmailOtpSent = 1 THEN 1 ELSE 0 END) as EmailOtpSent,
    (SUM(CASE WHEN IsEmailOtpSent = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) as EmailDeliveryRate
FROM Users;
```

### SMS OTP Delivery Rate

```sql
SELECT 
    COUNT(*) as TotalUsers,
    SUM(CASE WHEN IsSmsOtpSent = 1 THEN 1 ELSE 0 END) as SmsOtpSent,
    (SUM(CASE WHEN IsSmsOtpSent = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) as SmsDeliveryRate
FROM Users;
```

### Users With Failed OTP Delivery

```sql
SELECT 
    Id,
    Email,
    PhoneNumber,
    IsEmailOtpSent,
    IsSmsOtpSent,
    CreatedAt
FROM Users
WHERE 
    IsEmailOtpSent = 0 
    AND IsSmsOtpSent = 0
    AND CreatedAt >= datetime('now', '-24 hours');
```

---

## Troubleshooting

### Issue: Flags Not Updated After OTP Sent

**Check:**
1. Verify OTP was sent successfully
2. Check logs for errors in `UpdateUserOtpSentFlagsAsync`
3. Verify user ID is passed to OTP service

**Solution:**
```csharp
// Ensure userId is passed when generating OTP
var result = await _otpService.GenerateAndSendOtpAsync(
    phoneNumber: user.PhoneNumber,
    email: user.Email,
    purpose: "Registration",
    userId: user.Id,  // ? Make sure this is set
    senderInfo: senderInfo
);
```

### Issue: Timestamp is NULL

**Cause:** Flag not updated or OTP sending failed

**Solution:**
- Check OTP service logs
- Verify delivery method configuration
- Check SMTP/SMS configuration

---

## Future Enhancements

Potential additions:

- [ ] Track OTP retry count
- [ ] OTP delivery failure reasons
- [ ] Preferred delivery method per user
- [ ] Delivery cost tracking
- [ ] Delivery provider tracking (Twilio, SendGrid, etc.)
- [ ] A/B testing different delivery methods
- [ ] User notification preferences

---

## Build Status

? **Build Successful**

---

## Files Created/Modified

```
? backend\MedRemind.Core\Models\User.cs (UPDATED)
? backend\MedRemind.Core\Data\MedRemindDbContext.cs (UPDATED)
? backend\MedRemind.Services\Communication\OtpCodeService.cs (UPDATED)
? backend\MedRemind.Core\DTOs\UserDTOs.cs (UPDATED)
? backend\MedRemind.Services\Users\UserService.cs (UPDATED)
? backend\MedRemind.Services\Authentication\AuthenticationService.cs (UPDATED)
? backend\MedRemind.API\database_scripts\AddOtpDeliveryTracking.sql (NEW)
```

---

**Last Updated:** 2024-02-04  
**Version:** 1.0  
**Status:** ? Complete & Production Ready
