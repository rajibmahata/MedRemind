# Quick Reference - Password Authentication

## Summary
Secure password authentication with PBKDF2 encryption, password login, forgot password, and password reset.

## New Features

### 1. Password Hashing
- **PBKDF2** - Industry standard
- **100,000 iterations** - OWASP recommended
- **SHA-256** - Secure algorithm
- **Random salt** - Per password

### 2. Password Requirements
- ? Min 8 characters
- ? Max 128 characters
- ? 1 uppercase letter
- ? 1 lowercase letter
- ? 1 number
- ? 1 special character

### 3. New API Endpoints
```
POST /api/auth/login              ? Login with password
POST /api/auth/forgot-password    ? Request reset email
POST /api/auth/reset-password     ? Reset with token
POST /api/auth/change-password    ? Change password (auth required)
```

## Quick Start

### 1. Run Migration
```powershell
.\run-database-migrations.ps1 -SingleScript "AddPasswordFieldsToUsers.sql"
```

### 2. Register with Password
```bash
POST /api/users/register
{
  "phoneNumber": "9876543210",
  "email": "john@example.com",
  "name": "John Doe",
  "password": "MyPass123!"  ? Optional
}
```

### 3. Login with Password
```bash
POST /api/auth/login
{
  "identifier": "john@example.com",  ? Email or phone
  "password": "MyPass123!"
}

# Response: JWT token + profile
```

### 4. Forgot Password
```bash
POST /api/auth/forgot-password
{
  "email": "john@example.com"
}

# Email sent with reset link + token
```

### 5. Reset Password
```bash
POST /api/auth/reset-password
{
  "email": "john@example.com",
  "resetToken": "token-from-email",
  "newPassword": "NewPass456!",
  "confirmPassword": "NewPass456!"
}
```

### 6. Change Password
```bash
POST /api/auth/change-password
Authorization: Bearer <your-jwt-token>
{
  "currentPassword": "MyPass123!",
  "newPassword": "NewPass456!",
  "confirmPassword": "NewPass456!"
}
```

## Authentication Methods

### Option 1: OTP Login (Existing)
```
1. Register without password
2. Request OTP (SMS or Email)
3. Verify OTP
4. Get JWT token
```

### Option 2: Password Login (New)
```
1. Register with password
2. Login with email/phone + password
3. Get JWT token immediately
```

### Option 3: Hybrid
```
1. Register with password
2. Can use OTP login OR password login
3. User choice
```

## Password Flow Diagrams

### Registration
```
Register with password
   ?
Validate password strength
   ?
Hash with PBKDF2 + salt
   ?
Save to PasswordHash
   ?
User created
```

### Login
```
Enter email/phone + password
   ?
Find user
   ?
Verify password vs PasswordHash
   ?
Generate JWT token
   ?
Login successful
```

### Forgot Password
```
Request reset
   ?
Generate secure token
   ?
Send email with token
   ?
User clicks link or enters token
   ?
Reset password
   ?
Token cleared
```

## Database Fields

```sql
-- New fields in Users table
PasswordHash              TEXT NULL  ? Hashed password
PasswordResetToken        TEXT NULL  ? Reset token
PasswordResetTokenExpiry  TEXT NULL  ? Token expiry (1 hour)
LastPasswordChangeAt      TEXT NULL  ? Last change timestamp
```

## Security Features

### ? Password Hashing
```
"MyPassword123!"
   ? PBKDF2
   ? 100,000 iterations
   ? Random salt
   ?
"aB3Kd9...xyz" (stored)
```

### ? Reset Tokens
- Cryptographically random
- 1-hour expiry
- Single-use only
- Cleared after use

### ? No User Enumeration
```
# User exists
POST /api/auth/forgot-password
Response: "If an account exists..."

# User doesn't exist
POST /api/auth/forgot-password
Response: "If an account exists..."  ? Same message
```

## Password Reset Email

Beautiful modern template:
```
??????????????????????????????
?  ?? MedRemind              ? ? Purple gradient
??????????????????????????????
? Hello John! ??             ?
?                            ?
? Reset your password:       ?
? ????????????????????       ?
? ? Reset Password   ?       ? ? Button
? ????????????????????       ?
?                            ?
? Or use token:              ?
? aB3Kd9...xyz               ?
?                            ?
? ? Valid for 1 hour        ?
? ?? Security tips           ?
??????????????????????????????
```

## Error Messages

```
? "Password must be at least 8 characters long"
? "Password must contain at least one uppercase letter"
? "Password must contain at least one number"
? "Password must contain at least one special character"
? "Passwords do not match"
? "Current password is incorrect"
? "Invalid or expired reset token"
? "No password set. Please use OTP login or reset your password."
```

## Testing Checklist

- [ ] Register with password
- [ ] Login with password
- [ ] Login with email
- [ ] Login with phone
- [ ] Forgot password (email sent)
- [ ] Reset password with token
- [ ] Reset password with link
- [ ] Change password (authenticated)
- [ ] Invalid password (validation)
- [ ] Expired reset token
- [ ] Wrong current password

## Build Status

? **Build Successful** - Ready to use

## Documentation

?? **Full Guide:** `PASSWORD_AUTHENTICATION_IMPLEMENTATION.md`

---

**Security:** PBKDF2 + SHA-256 + 100k iterations + random salt  
**Token Expiry:** 1 hour  
**Password Rules:** 8+ chars, uppercase, lowercase, number, special char
