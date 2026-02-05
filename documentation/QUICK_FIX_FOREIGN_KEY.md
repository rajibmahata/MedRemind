# QUICK FIX: Foreign Key Error When Creating Prescription

## The Problem
```
SQLite Error 19: 'FOREIGN KEY constraint failed'
```

## Root Cause
The `UserId` doesn't exist in the `Users` table. Prescriptions require a valid user.

## Quick Solutions

### Option 1: Create User via API (Recommended)

**Register a new user:**
```bash
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "phoneNumber": "+1234567890",
  "password": "Test@123",
  "name": "Test User"
}
```

**Response:**
```json
{
  "success": true,
  "userId": 1,
  "token": "eyJhbGciOiJIUzI1..."
}
```

### Option 2: Create User via SQL

```sql
-- Insert test user directly
INSERT INTO Users (PhoneNumber, PasswordHash, Name, CreatedAt)
VALUES ('+1234567890', 'test_hash_123', 'Test User', datetime('now'));

-- Verify
SELECT Id, PhoneNumber, Name FROM Users;
```

### Option 3: Check Existing Users

```sql
-- List all users
SELECT Id, PhoneNumber, Name FROM Users ORDER BY Id;

-- Use an existing UserId in your API call
```

## Using PowerShell to Create User

```powershell
# Register user via API
$body = @{
    phoneNumber = "+1234567890"
    password = "Test@123"
    name = "Test User"
    gender = "Male"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/auth/register" `
    -Method Post `
    -Body $body `
    -ContentType "application/json"
```

## Verify User Exists Before Upload

```bash
# Check if user exists
GET http://localhost:5000/api/users/1
Authorization: Bearer YOUR_JWT_TOKEN
```

## What Changed in Code

? **User validation added** - Checks if user exists before creating prescription  
? **Better error messages** - Clear explanation of the issue  
? **Proper exception handling** - Catches FK constraint errors gracefully

## Updated Workflow

1. **Register user** (or verify user exists)
2. **Login** to get JWT token
3. **Upload prescription** using valid userId from token

## Quick Test

```bash
# 1. Register
POST /api/auth/register
{
  "phoneNumber": "+1234567890",
  "password": "Test@123",
  "name": "Test User"
}

# 2. Upload prescription with token from step 1
POST /api/prescriptions/upload
Authorization: Bearer {token_from_step_1}
Content-Type: application/json
{
  "imageBase64": "...",
  "fileName": "prescription.jpg"
}
```

## Error Prevention

The code now:
- ? Validates user exists BEFORE creating prescription
- ? Returns clear error: "User with ID X does not exist"
- ? Provides helpful message: "Please ensure the user is registered"
- ? Logs detailed debugging information

---

**Need more help?** See `FOREIGN_KEY_ERROR_GUIDE.md` for detailed troubleshooting.
