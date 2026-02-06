# Quick Reference - OTP Delivery Tracking

## Summary
Added flags to track email/SMS OTP delivery status and timestamps in User model.

## New Fields in User Model

```csharp
// OTP Delivery Tracking
public bool IsEmailOtpSent { get; set; } = false;
public bool IsSmsOtpSent { get; set; } = false;
public DateTime? LastEmailOtpSentAt { get; set; }
public DateTime? LastSmsOtpSentAt { get; set; }
```

## How It Works

### When OTP is Sent

```
OTP Sent via Email
   ?
IsEmailOtpSent = true
LastEmailOtpSentAt = DateTime.UtcNow

OTP Sent via SMS
   ?
IsSmsOtpSent = true
LastSmsOtpSentAt = DateTime.UtcNow

OTP Sent via Both
   ?
Both flags = true
Both timestamps updated
```

## Migration

```powershell
# Run migration
.\run-database-migrations.ps1 -SingleScript "AddOtpDeliveryTracking.sql"

# Or batch
.\run-database-migrations.bat AddOtpDeliveryTracking.sql
```

## Database Fields

```sql
-- New columns
IsEmailOtpSent        INTEGER NOT NULL DEFAULT 0
IsSmsOtpSent          INTEGER NOT NULL DEFAULT 0
LastEmailOtpSentAt    TEXT NULL
LastSmsOtpSentAt      TEXT NULL

-- New indexes
IX_Users_IsEmailOtpSent
IX_Users_IsSmsOtpSent
```

## API Response Example

```json
{
  "id": 1,
  "email": "john@example.com",
  "phoneNumber": "8420249020",
  "isEmailOtpSent": true,
  "isSmsOtpSent": true,
  "lastEmailOtpSentAt": "2024-02-04T10:30:00Z",
  "lastSmsOtpSentAt": "2024-02-04T10:30:00Z"
}
```

## Usage Examples

### Check OTP Delivery Status
```csharp
var user = await _userService.GetUserByIdAsync(userId);

if (user.IsEmailOtpSent)
    Console.WriteLine($"Email OTP sent at: {user.LastEmailOtpSentAt}");

if (user.IsSmsOtpSent)
    Console.WriteLine($"SMS OTP sent at: {user.LastSmsOtpSentAt}");
```

### Query Users with Email OTP
```csharp
var users = await _unitOfWork.Repository<User>()
    .FindAsync(u => u.IsEmailOtpSent == true);
```

### Query Recent OTP Deliveries
```csharp
var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
var recent = await _unitOfWork.Repository<User>()
    .FindAsync(u => u.LastEmailOtpSentAt >= fiveMinutesAgo);
```

## Monitoring Queries

### Email Delivery Rate
```sql
SELECT 
    COUNT(*) as Total,
    SUM(CASE WHEN IsEmailOtpSent = 1 THEN 1 ELSE 0 END) as Sent,
    (SUM(CASE WHEN IsEmailOtpSent = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)) as Rate
FROM Users;
```

### Users Without OTP
```sql
SELECT * FROM Users
WHERE IsEmailOtpSent = 0 AND IsSmsOtpSent = 0;
```

### Today's OTP Deliveries
```sql
SELECT * FROM Users
WHERE DATE(LastEmailOtpSentAt) = DATE('now')
   OR DATE(LastSmsOtpSentAt) = DATE('now');
```

## Testing

### Test Email OTP
```bash
# Send OTP (Email config)
POST /api/auth/send-otp
{
  "phoneNumber": "8420249020"
}

# Check profile
GET /api/users/me

# Expected:
{
  "isEmailOtpSent": true,
  "isSmsOtpSent": false,
  "lastEmailOtpSentAt": "2024-02-04T10:30:00Z"
}
```

### Test SMS OTP
```bash
# Send OTP (SMS config)
POST /api/auth/send-otp
{
  "phoneNumber": "8420249020"
}

# Expected:
{
  "isEmailOtpSent": false,
  "isSmsOtpSent": true,
  "lastSmsOtpSentAt": "2024-02-04T10:30:00Z"
}
```

### Test Both
```bash
# Send OTP (Both config)
POST /api/auth/send-otp
{
  "phoneNumber": "8420249020"
}

# Expected:
{
  "isEmailOtpSent": true,
  "isSmsOtpSent": true,
  "lastEmailOtpSentAt": "2024-02-04T10:30:00Z",
  "lastSmsOtpSentAt": "2024-02-04T10:30:00Z"
}
```

## Configuration

```json
{
  "OtpService": {
    "DeliveryMethod": "Both",  // "Email", "SMS", "Both"
    "ExpirationMinutes": 10
  }
}
```

## Benefits

? **Track Delivery** - Know when OTP was sent  
? **Monitor Methods** - See which method was used  
? **User Support** - Verify OTP delivery quickly  
? **Analytics** - Track usage patterns  
? **Debugging** - Troubleshoot delivery issues  

## Build Status

? **Build Successful** - Ready to use

---

**Full Documentation:** `OTP_DELIVERY_TRACKING_IMPLEMENTATION.md`
