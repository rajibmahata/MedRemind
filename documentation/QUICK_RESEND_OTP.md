# Quick Reference - Resend OTP

## API Endpoints

### Resend OTP
```bash
POST /api/auth/resend-otp
Content-Type: application/json

{
  "phoneNumber": "8420249020",
  "email": "user@example.com",  // Optional
  "purpose": "Registration"      // Registration, Login, PasswordReset
}
```

**Success (200):**
```json
{
  "success": true,
  "message": "OTP has been resent successfully.",
  "remainingAttempts": 3,
  "isEmailOtpSent": true,
  "isSmsOtpSent": false
}
```

**Rate Limited (429):**
```json
{
  "success": false,
  "errorMessage": "Please wait 45 seconds before requesting a new OTP.",
  "nextResendAvailableAt": "2024-02-04T10:45:30Z"
}
```

### Check Availability
```bash
GET /api/auth/resend-otp/availability?phoneNumber=8420249020&purpose=Registration
```

**Response:**
```json
{
  "success": true,
  "message": "Resend is available",
  "remainingAttempts": 4
}
```

---

## Rate Limits

| Limit Type | Value | Description |
|------------|-------|-------------|
| Time-based | 60 seconds | Minimum wait between requests |
| Daily limit | 5 requests | Maximum per 24 hours |

---

## Usage Flow

```
1. User registers ? OTP sent
2. User didn't receive OTP
3. User clicks "Resend OTP"
4. Check availability (optional)
5. Resend OTP request
6. Wait 60 seconds before next resend
7. Max 5 resends per day
```

---

## Error Codes

| Code | Meaning | Action |
|------|---------|--------|
| 200 | Success | OTP sent |
| 400 | Validation error | Check request |
| 429 | Rate limited | Wait before retry |
| 500 | Server error | Try again later |

---

## Testing

### cURL Examples

**Resend OTP:**
```bash
curl -X POST http://localhost:5000/api/auth/resend-otp \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "8420249020",
    "purpose": "Registration"
  }'
```

**Check Availability:**
```bash
curl "http://localhost:5000/api/auth/resend-otp/availability?phoneNumber=8420249020&purpose=Registration"
```

---

## Client-Side Example

```javascript
// Resend OTP
async function resendOtp(phoneNumber) {
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
    alert(`Wait ${getWaitTime(data.nextResendAvailableAt)} seconds`);
  } else if (data.success) {
    alert(`OTP sent! ${data.remainingAttempts} attempts left`);
  } else {
    alert(data.errorMessage);
  }
}

// Check availability
async function checkAvailability(phoneNumber) {
  const response = await fetch(
    `/api/auth/resend-otp/availability?phoneNumber=${phoneNumber}&purpose=Registration`
  );
  const data = await response.json();
  return data.success;
}
```

---

## Configuration

**appsettings.Development.json:**
```json
{
  "Features": {
    "EnableSmsOtp": false,
    "EnableEmailOtp": true
  },
  "SmtpSettings": {
    "SmtpHost": "mail.privateemail.com",
    "SmtpPort": 587,
    "FromEmail": "noreply@medremind.com"
  }
}
```

---

## Adjust Rate Limits

**File:** `OtpCodeService.cs`

```csharp
// Time limit (default: 60 seconds)
var minimumWaitTime = TimeSpan.FromSeconds(60);

// Daily limit (default: 5)
var maxDailyOtps = 5;
```

---

## Monitoring

**Recent OTP requests:**
```sql
SELECT PhoneNumber, Purpose, CreatedAt 
FROM OtpCodes 
WHERE PhoneNumber = '8420249020'
ORDER BY CreatedAt DESC
LIMIT 10;
```

**Daily counts:**
```sql
SELECT PhoneNumber, COUNT(*) as Total
FROM OtpCodes
WHERE CreatedAt >= datetime('now', '-24 hours')
GROUP BY PhoneNumber;
```

---

## Troubleshooting

### OTP Not Received
- Check email/SMS configuration
- Verify delivery method enabled
- Check spam folder

### Rate Limit Too Strict
- Adjust `minimumWaitTime` in code
- Increase `maxDailyOtps` if needed

### Daily Limit Reached
- Wait 24 hours since first request
- Or contact support to reset

---

## Full Documentation

See `RESEND_OTP_IMPLEMENTATION.md` for complete guide

---

**Status:** ? Ready to use
