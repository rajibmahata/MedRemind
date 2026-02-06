# User Registration System - Complete Guide

## Overview
A complete user registration and profile management system with email support has been implemented for the MedRemind API.

## What's New?

### 1. ? Email Support
- Added **mandatory** `Email` field to User model
- Email validation with regex
- Unique email constraint in database
- Email is required during registration

### 2. ? UserService
- Handles all user-related business logic
- Registration, profile management, CRUD operations
- Email and phone number validation
- Duplicate checking

### 3. ? UsersController
- RESTful API endpoints for user management
- JWT authentication integration
- Proper authorization (users can only access their own data)

## API Endpoints

### 1. Register User

**POST** `/api/users/register`

**Request Body:**
```json
{
  "phoneNumber": "9876543210",
  "email": "user@example.com",
  "name": "John Doe",
  "dateOfBirth": "1990-01-15",
  "gender": "Male"
}
```

**Response (Success):**
```json
{
  "success": true,
  "userId": 1,
  "token": null,
  "errorMessage": null,
  "profile": {
    "id": 1,
    "phoneNumber": "9876543210",
    "email": "user@example.com",
    "name": "John Doe",
    "dateOfBirth": "1990-01-15T00:00:00Z",
    "gender": "Male",
    "profilePhotoPath": null,
    "isBiometricEnabled": false,
    "createdAt": "2024-02-04T10:30:00Z",
    "lastLoginAt": "2024-02-04T10:30:00Z"
  }
}
```

**Response (Error):**
```json
{
  "success": false,
  "userId": null,
  "token": null,
  "errorMessage": "User with this phone number already exists",
  "profile": null
}
```

**Validation Rules:**
- ? Phone number: Required, must be 10 digits
- ? Email: **Required**, must be valid format
- ? Name: Optional
- ? Date of Birth: Optional
- ? Gender: Optional

---

### 2. Get User Profile

**GET** `/api/users/{id}`  
**Authorization:** Required (Bearer Token)

**Response:**
```json
{
  "id": 1,
  "phoneNumber": "9876543210",
  "email": "user@example.com",
  "name": "John Doe",
  "dateOfBirth": "1990-01-15T00:00:00Z",
  "gender": "Male",
  "profilePhotoPath": null,
  "isBiometricEnabled": false,
  "createdAt": "2024-02-04T10:30:00Z",
  "lastLoginAt": "2024-02-04T10:35:00Z"
}
```

**Authorization:**
- Users can only access their own profile
- Attempting to access another user's profile returns **403 Forbidden**

---

### 3. Get Current User Profile

**GET** `/api/users/me`  
**Authorization:** Required (Bearer Token)

**Response:** Same as Get User Profile

**Use Case:**
- Get profile of currently authenticated user
- No need to specify user ID
- Extracts user ID from JWT token

---

### 4. Update User Profile

**PUT** `/api/users/{id}`  
**Authorization:** Required (Bearer Token)

**Request Body:**
```json
{
  "name": "John Updated Doe",
  "email": "newemail@example.com",
  "dateOfBirth": "1990-01-15",
  "gender": "Male"
}
```

**Response (Success):**
```json
{
  "message": "Profile updated successfully"
}
```

**Response (Error):**
```json
{
  "message": "Email already in use by another user"
}
```

**Authorization:**
- Users can only update their own profile
- All fields are optional (only provided fields are updated)

---

### 5. Delete User Account

**DELETE** `/api/users/{id}`  
**Authorization:** Required (Bearer Token)

**Response (Success):**
```json
{
  "message": "User deleted successfully"
}
```

**Authorization:**
- Users can only delete their own account
- Cascade deletes all related data (prescriptions, medications, etc.)

---

### 6. Check User Exists

**GET** `/api/users/exists/{phoneNumber}`  
**No Authorization Required**

**Response:**
```json
{
  "exists": true
}
```

**Use Case:**
- Check if phone number is already registered
- Useful for registration form validation

---

## Database Changes

### User Model - New Field

```csharp
public class User
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; } // ? NEW - Optional email field
    public string? Name { get; set; }
    // ... other fields
}
```

### Migration Script

**File:** `backend\MedRemind.API\database_scripts\AddEmailToUsers.sql`

```sql
-- Add Email column (nullable)
ALTER TABLE Users
ADD Email TEXT NULL;

-- Add unique index for email
CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email 
ON Users(Email) 
WHERE Email IS NOT NULL;
```

**Run Migration:**
```powershell
.\run-database-migrations.ps1 -SingleScript "AddEmailToUsers.sql"
```

---

## Code Structure

### UserService.cs
**Location:** `backend\MedRemind.Services\Users\UserService.cs`

**Key Methods:**
- `RegisterUserAsync()` - Register new user
- `GetUserByIdAsync()` - Get user profile by ID
- `GetUserByPhoneNumberAsync()` - Get user by phone
- `UpdateUserProfileAsync()` - Update user profile
- `DeleteUserAsync()` - Delete user account
- `UserExistsAsync()` - Check if user exists

### UsersController.cs
**Location:** `backend\MedRemind.API\Controllers\UsersController.cs`

**Endpoints:**
- `POST /api/users/register`
- `GET /api/users/{id}`
- `GET /api/users/me`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`
- `GET /api/users/exists/{phoneNumber}`

### DTOs
**Location:** `backend\MedRemind.Core\DTOs\UserDTOs.cs`

- `UserRegistrationRequest`
- `UserRegistrationResponse`
- `UserProfileUpdateRequest`
- `UserProfileData`

---

## Usage Examples

### Example 1: Register User with Email

```bash
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "9876543210",
    "email": "john@example.com",
    "name": "John Doe",
    "dateOfBirth": "1990-01-15",
    "gender": "Male"
  }'
```

### Example 2: Register User without Email

```bash
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "9876543210",
    "name": "John Doe"
  }'
```

### Example 3: Get User Profile

```bash
curl -X GET http://localhost:5000/api/users/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Example 4: Update Profile with Email

```bash
curl -X PUT http://localhost:5000/api/users/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Updated",
    "email": "newemail@example.com"
  }'
```

### Example 5: Check if User Exists

```bash
curl -X GET http://localhost:5000/api/users/exists/9876543210
```

---

## Validation

### Phone Number Validation
- ? Must be exactly 10 digits
- ? Must contain only numbers
- ? Must be unique (no duplicate phone numbers)

### Email Validation
- ? **Required field** (cannot be null or empty)
- ? Must be valid email format
- ? Must be unique (no duplicate emails)
- ? Regex: `^[^@\s]+@[^@\s]+\.[^@\s]+$`

### Examples:
```
Valid Emails:
? user@example.com
? john.doe@company.co.uk

Invalid Emails:
? invalid@email
? @example.com
? user@.com
? (empty) - Email is required
```

---

## Integration with OTP Authentication

The registration system integrates seamlessly with the existing OTP authentication:

### Flow 1: OTP Login (Existing Users)
```
1. User enters phone number
2. OTP sent via SMS
3. User enters OTP
4. OTP verified ?
5. User logged in with JWT token
```

### Flow 2: OTP Login (New Users - Auto Registration)
```
1. User enters phone number (not registered)
2. OTP sent via SMS
3. User enters OTP
4. OTP verified ?
5. User automatically registered ?
6. User logged in with JWT token
```

### Flow 3: Registration with Email (New)
```
1. User fills registration form (phone + email + details)
2. POST /api/users/register ?
3. User receives success response
4. User can now login via OTP
5. After OTP login, user has full profile
```

---

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task RegisterUserAsync_WithValidData_ShouldSucceed()
{
    // Arrange
    var request = new UserRegistrationRequest
    {
        PhoneNumber = "9876543210",
        Email = "test@example.com",
        Name = "Test User"
    };
    
    // Act
    var result = await _userService.RegisterUserAsync(request);
    
    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.UserId);
    Assert.Equal("test@example.com", result.Profile?.Email);
}

[Fact]
public async Task RegisterUserAsync_WithDuplicatePhone_ShouldFail()
{
    // Arrange
    await RegisterUserAsync(new UserRegistrationRequest { PhoneNumber = "9876543210" });
    var duplicateRequest = new UserRegistrationRequest { PhoneNumber = "9876543210" };
    
    // Act
    var result = await _userService.RegisterUserAsync(duplicateRequest);
    
    // Assert
    Assert.False(result.Success);
    Assert.Contains("already exists", result.ErrorMessage);
}
```

---

## Security Considerations

### 1. JWT Authentication
- All profile operations require valid JWT token
- Token contains user ID in claims
- Tokens expire after configured period (default: 30 days)

### 2. Authorization
- Users can only access/modify their own data
- UserId extracted from JWT claims
- Attempting to access other users' data returns 403 Forbidden

### 3. Data Validation
- Phone numbers validated (10 digits, numeric)
- Emails validated (regex pattern)
- SQL injection protected (using Entity Framework)

### 4. Privacy
- Passwords not stored (OTP-based authentication)
- Session tokens used for authentication
- Personal data only accessible to user

---

## Error Handling

### Common Errors

**400 Bad Request**
```json
{
  "success": false,
  "errorMessage": "Invalid phone number format. Please enter a 10-digit phone number"
}
```

**401 Unauthorized**
```json
{
  "message": "Not authenticated"
}
```

**403 Forbidden**
```json
{
  "message": "Access denied"
}
```

**404 Not Found**
```json
{
  "message": "User not found"
}
```

**500 Internal Server Error**
```json
{
  "message": "Internal server error"
}
```

---

## Future Enhancements

Potential additions:

- [ ] Email verification via OTP
- [ ] Password-based login (in addition to phone OTP)
- [ ] Profile photo upload
- [ ] User preferences/settings
- [ ] Account recovery via email
- [ ] Two-factor authentication
- [ ] Social media login integration

---

## Troubleshooting

### Issue: "User with this phone number already exists"
**Solution:** Use the existing user or use OTP login instead of registration

### Issue: "Invalid email format"
**Solution:** Ensure email follows format: `user@domain.com`

### Issue: "Email already in use"
**Solution:** Use a different email address

### Issue: 403 Forbidden when accessing profile
**Solution:** Ensure you're using a valid JWT token and accessing your own profile

---

**Last Updated:** 2024-02-04  
**Version:** 1.0  
**Build Status:** ? Successful
