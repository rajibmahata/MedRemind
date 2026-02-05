# Foreign Key Constraint Error - Troubleshooting Guide

## Error Message
```
SQLite Error 19: 'FOREIGN KEY constraint failed'
```

## Root Cause
This error occurs when trying to create a `Prescription` record with a `UserId` that doesn't exist in the `Users` table.

The `Prescriptions` table has a foreign key constraint:
```csharp
entity.HasOne(e => e.User)
    .WithMany(u => u.Prescriptions)
    .HasForeignKey(e => e.UserId)
    .OnDelete(DeleteBehavior.Cascade);
```

## Solutions

### 1. ? Ensure User Exists Before Creating Prescription

The code now validates the user exists before attempting to create a prescription:

```csharp
// Validate UserId exists
if (_unitOfWork != null)
{
    var userRepo = _unitOfWork.Repository<User>();
    var userExists = await userRepo.GetByIdAsync(userId);
    if (userExists == null)
    {
        return new PrescriptionProcessingResult
        {
            Success = false,
            ErrorMessage = "User with ID {userId} does not exist. Please ensure the user is registered."
        };
    }
}
```

### 2. ?? Create Test User via API

**POST** `/api/auth/register`

```json
{
  "phoneNumber": "+1234567890",
  "password": "Test@123",
  "name": "Test User",
  "gender": "Male",
  "dateOfBirth": "1990-01-01"
}
```

**Response:**
```json
{
  "success": true,
  "userId": 1,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "message": "User registered successfully"
}
```

### 3. ?? Create Test User via SQL

```sql
-- Insert a test user
INSERT INTO Users (PhoneNumber, PasswordHash, Name, Gender, DateOfBirth, CreatedAt)
VALUES ('+1234567890', 'test_hash', 'Test User', 'Male', '1990-01-01', datetime('now'));

-- Verify user was created
SELECT * FROM Users WHERE PhoneNumber = '+1234567890';
```

### 4. ?? Debug: Check Existing Users

```sql
-- List all users
SELECT Id, PhoneNumber, Name FROM Users;

-- Check if specific user exists
SELECT * FROM Users WHERE Id = 1;
```

### 5. ?? API Request with Valid UserId

When calling prescription upload endpoint, ensure you use a valid userId:

```json
{
  "userId": 1,  // ? Must exist in Users table
  "imageBase64": "iVBORw0KGgoAAAANSUhEUgA...",
  "fileName": "prescription.jpg"
}
```

## Enhanced Error Messages

The code now provides detailed error messages:

### Before
```
Error 19: FOREIGN KEY constraint failed
```

### After
```
Database constraint error: User ID 1 does not exist or is invalid. 
Please ensure the user is registered.
```

## Testing Workflow

### Step 1: Verify User Exists
```bash
# GET user by ID
curl -X GET http://localhost:5000/api/users/1
```

### Step 2: If User Doesn't Exist, Create One
```bash
# Register new user
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+1234567890",
    "password": "Test@123",
    "name": "Test User"
  }'
```

### Step 3: Upload Prescription with Valid UserId
```bash
# Upload prescription
curl -X POST http://localhost:5000/api/prescriptions/upload \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "imageBase64": "...",
    "fileName": "prescription.jpg"
  }'
```

## Common Scenarios

### Scenario 1: API Call Without Authentication
**Problem:** JWT token contains userId that doesn't exist

**Solution:** Register user first, then use the returned JWT token

### Scenario 2: Testing with Postman
**Problem:** Hard-coded userId in request

**Solution:** 
1. Call Register endpoint first
2. Copy returned userId
3. Use that userId in prescription upload

### Scenario 3: Database Reset
**Problem:** Database was recreated, all users deleted

**Solution:** Re-run user seeding or re-register users

## Database Schema

### Users Table (Parent)
```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PhoneNumber TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Name TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

### Prescriptions Table (Child)
```sql
CREATE TABLE Prescriptions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,  -- FOREIGN KEY to Users.Id
    ImagePath TEXT NOT NULL,
    Status TEXT DEFAULT 'Processing',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

## Code Changes Made

### 1. User Validation Before Prescription Creation
```csharp
// Validate UserId exists
var userExists = await userRepo.GetByIdAsync(userId);
if (userExists == null)
{
    return error message;
}
```

### 2. Enhanced Error Handling
```csharp
try
{
    prescription = await _prescriptionService.AddPrescriptionAsync(prescription);
}
catch (DbUpdateException dbEx)
{
    if (dbEx.InnerException?.Message.Contains("FOREIGN KEY constraint failed"))
    {
        return "User ID does not exist...";
    }
}
```

## Prevention Checklist

- [ ] Always authenticate users before prescription upload
- [ ] Extract userId from JWT token, don't accept from request body
- [ ] Validate user exists before processing
- [ ] Use proper error handling for FK constraints
- [ ] Test with valid users in database
- [ ] Seed test users in development environment

## Quick Fix Commands

**SQLite:**
```sql
-- Check if user exists
SELECT COUNT(*) FROM Users WHERE Id = 1;

-- Create test user if doesn't exist
INSERT OR IGNORE INTO Users (Id, PhoneNumber, PasswordHash, Name, CreatedAt)
VALUES (1, '+9999999999', 'test_hash', 'Test User', datetime('now'));
```

**EF Core Migration (if needed):**
```bash
# Check current database state
dotnet ef database update

# Reset database (CAUTION: Deletes all data)
dotnet ef database drop --force
dotnet ef database update
```

---

**Last Updated:** 2024-02-04  
**Version:** 1.0
