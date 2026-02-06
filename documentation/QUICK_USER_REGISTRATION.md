# Quick Reference - User Registration & Management

## Database Migration

**Run this first:**
```powershell
.\run-database-migrations.ps1 -SingleScript "AddEmailToUsers.sql"
```

## API Endpoints

### 1. Register User
```http
POST /api/users/register
Content-Type: application/json

{
  "phoneNumber": "9876543210",
  "email": "user@example.com",
  "name": "John Doe",
  "dateOfBirth": "1990-01-15",
  "gender": "Male"
}
```

### 2. Get Profile
```http
GET /api/users/me
Authorization: Bearer YOUR_JWT_TOKEN
```

### 3. Update Profile
```http
PUT /api/users/{id}
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "name": "Updated Name",
  "email": "newemail@example.com"
}
```

### 4. Check if User Exists
```http
GET /api/users/exists/9876543210
```

## Key Features

? **Email field added** - Optional, validated, unique  
? **Phone validation** - 10 digits, numeric only  
? **JWT authentication** - Secure API access  
? **User can only access own data** - Authorization built-in  
? **Email uniqueness** - No duplicate emails  
? **Phone uniqueness** - No duplicate phones  

## Field Requirements

| Field | Required | Format | Notes |
|-------|----------|--------|-------|
| phoneNumber | ? Yes | 10 digits | Must be unique |
| email | ? Yes | Valid email | Must be unique |
| name | ? No | String | - |
| dateOfBirth | ? No | ISO 8601 | YYYY-MM-DD |
| gender | ? No | String | Male/Female/Other |

## Quick Test

**1. Register:**
```bash
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber":"9876543210","email":"test@test.com","name":"Test User"}'
```

**2. Check exists:**
```bash
curl http://localhost:5000/api/users/exists/9876543210
```

**3. Get profile (after login):**
```bash
curl -X GET http://localhost:5000/api/users/me \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Error Messages

| Error | Meaning | Solution |
|-------|---------|----------|
| "Email is required" | Email not provided | Provide valid email |
| "User with this phone number already exists" | Phone taken | Use different phone or login |
| "Invalid phone number format" | Phone invalid | Use 10 digits, numeric only |
| "Invalid email format" | Email invalid | Use format: user@domain.com |
| "Email already in use" | Email taken | Use different email |
| 403 Forbidden | Wrong user access | Access only your own profile |

## Code Files

- **Controller:** `backend\MedRemind.API\Controllers\UsersController.cs`
- **Service:** `backend\MedRemind.Services\Users\UserService.cs`
- **DTOs:** `backend\MedRemind.Core\DTOs\UserDTOs.cs`
- **Model:** `backend\MedRemind.Core\Models\User.cs`
- **Migration:** `backend\MedRemind.API\database_scripts\AddEmailToUsers.sql`

---

**Full Documentation:** See `USER_REGISTRATION_GUIDE.md`
