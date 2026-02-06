# MedRemind API - Complete Collection Summary

## ?? What You Get

3 files for complete API testing:

1. **`MedRemind_Complete_API_Collection.postman_collection.json`**
   - Ready-to-import Postman collection
   - 24 pre-configured API requests
   - Organized in 4 folders
   - Built-in authorization

2. **`MEDREMIND_CURL_COMMANDS.md`**
   - Individual cURL commands
   - Copy-paste ready
   - Terminal/command-line usage
   - Examples with actual data

3. **`QUICK_POSTMAN_IMPORT_GUIDE.md`**
   - Step-by-step setup
   - Environment configuration
   - Pro tips & tricks
   - Troubleshooting

---

## ?? Quick Start (3 Steps)

### Step 1: Import Collection (30 seconds)
```
Postman ? Import ? Upload Files
? Select: MedRemind_Complete_API_Collection.postman_collection.json
? Import
```

### Step 2: Create Environment (1 minute)
```
Postman ? Environments ? + New
Name: MedRemind Local
Variables:
  - base_url: http://localhost:5000
  - token: (empty)
  - user_id: 1
? Save ? Select environment
```

### Step 3: Test (30 seconds)
```
1. Run: "2.2 Register User (With Password)"
2. Run: "1.3 Login with Password"
3. Copy token from response
4. Set token in environment
5. Run: "2.3 Get Current User"
? Working!
```

---

## ?? API Endpoints Summary

### 1. Authentication (7 endpoints)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/send-otp` | Send OTP via SMS/Email | ? |
| POST | `/api/auth/verify-otp` | Verify OTP, get token | ? |
| POST | `/api/auth/login` | Password-based login | ? |
| POST | `/api/auth/forgot-password` | Request password reset | ? |
| POST | `/api/auth/reset-password` | Reset password with token | ? |
| POST | `/api/auth/change-password` | Change password | ? |
| POST | `/api/auth/validate-token` | Validate JWT token | ? |

### 2. Users (6 endpoints)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/users/register` | Register new user | ? |
| GET | `/api/users/me` | Get current user | ? |
| GET | `/api/users/{id}` | Get user by ID | ? |
| PUT | `/api/users/{id}` | Update user profile | ? |
| DELETE | `/api/users/{id}` | Delete user account | ? |

### 3. Medications (6 endpoints)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/medications` | Create medication | ? |
| GET | `/api/medications` | Get all medications | ? |
| GET | `/api/medications?isActive=true` | Get active medications | ? |
| GET | `/api/medications/{id}` | Get medication by ID | ? |
| PUT | `/api/medications/{id}` | Update medication | ? |
| DELETE | `/api/medications/{id}` | Delete medication | ? |

### 4. Prescriptions (5 endpoints)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/prescriptions/upload` | Upload prescription (OCR) | ? |
| GET | `/api/prescriptions` | Get all prescriptions | ? |
| GET | `/api/prescriptions/{id}` | Get prescription by ID | ? |
| GET | `/api/prescriptions/{id}?includeMedications=true` | Get with medications | ? |
| DELETE | `/api/prescriptions/{id}` | Delete prescription | ? |

**Total: 24 endpoints**

---

## ?? Authentication Methods

MedRemind supports **3 authentication methods:**

### Method 1: OTP (SMS/Email)
```bash
1. POST /api/auth/send-otp
   ? OTP sent to phone/email

2. POST /api/auth/verify-otp
   ? Returns JWT token
```

### Method 2: Password Login
```bash
POST /api/auth/login
{
  "identifier": "email or phone",
  "password": "password"
}
? Returns JWT token
```

### Method 3: Hybrid
```
Users can register with password
AND use OTP login
? Flexible authentication
```

---

## ?? Usage Examples

### Example 1: Complete User Journey
```bash
# 1. Register
POST /api/users/register
Body: { email, phone, name, password }

# 2. Login
POST /api/auth/login
Body: { identifier, password }
Response: { token, profile }

# 3. Upload Prescription
POST /api/prescriptions/upload
Auth: Bearer token
Body: FormData (file, userId, doctorName)

# 4. Get Auto-Extracted Medications
GET /api/medications?userId=1
Auth: Bearer token
Response: [ medications extracted from prescription ]

# 5. Get User Profile
GET /api/users/me
Auth: Bearer token
Response: { user profile with medications }
```

### Example 2: Password Reset Flow
```bash
# 1. Forgot Password
POST /api/auth/forgot-password
Body: { email }
? Reset email sent

# 2. User clicks link in email
# Opens: https://app.medremind.com/reset?token=xxx

# 3. Reset Password
POST /api/auth/reset-password
Body: { email, resetToken, newPassword, confirmPassword }
? Password updated

# 4. Login with New Password
POST /api/auth/login
Body: { identifier, password }
? Success!
```

### Example 3: Medication Management
```bash
# 1. Create Medication
POST /api/medications
Body: {
  userId, name, dosage, frequency,
  startDate, endDate, reminderTimes
}

# 2. Get Active Medications
GET /api/medications?userId=1&isActive=true

# 3. Update Medication
PUT /api/medications/1
Body: { updated fields }

# 4. Delete Medication
DELETE /api/medications/1
```

---

## ?? Collection Features

### ? Pre-configured Authorization
- Collection-level Bearer token
- Auto-applies to all requests
- Override per request if needed

### ? Environment Variables
- `{{base_url}}` - API base URL
- `{{token}}` - JWT authentication token
- `{{user_id}}` - Current user ID

### ? Example Request Bodies
- All requests include example JSON
- Ready to test immediately
- Modify as needed

### ? Organized Folders
- Logical grouping by feature
- Easy navigation
- Clear naming

---

## ?? Environment Setup

### Local Development
```json
{
  "base_url": "http://localhost:5000",
  "token": "",
  "user_id": "1"
}
```

### Staging
```json
{
  "base_url": "https://staging-api.medremind.com",
  "token": "",
  "user_id": "1"
}
```

### Production
```json
{
  "base_url": "https://api.medremind.com",
  "token": "",
  "user_id": "1"
}
```

---

## ?? Postman Scripts

### Auto-Save Token (Add to Login Request)
```javascript
// In Tests tab of Login request
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.environment.set("token", jsonData.token);
    pm.environment.set("user_id", jsonData.profile.id);
    console.log("? Token and User ID saved");
}
```

### Global Response Validator (Add to Collection)
```javascript
// In Collection ? Tests
pm.test("Status code is successful", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 201, 204]);
});

pm.test("Response time is acceptable", function () {
    pm.expect(pm.response.responseTime).to.be.below(2000);
});
```

### Log Request/Response (Add to Collection Pre-request)
```javascript
// In Collection ? Pre-request Scripts
console.log("??", pm.request.method, pm.request.url.toString());
console.log("?? Token:", pm.environment.get("token") ? "Set" : "Not Set");
```

---

## ?? Response Examples

### Success Response (200)
```json
{
  "success": true,
  "data": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com"
  }
}
```

### Login Response (200)
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "profile": {
    "id": 1,
    "email": "john@example.com",
    "name": "John Doe",
    "phoneNumber": "8420249020",
    "isEmailVerified": true,
    "authenticationMethod": "EmailOtp"
  }
}
```

### Error Response (400)
```json
{
  "success": false,
  "errorMessage": "Invalid credentials"
}
```

### Validation Error (400)
```json
{
  "success": false,
  "errorMessage": "Password must be at least 8 characters long"
}
```

---

## ?? Testing Checklist

### Authentication
- [ ] Register user without password
- [ ] Register user with password
- [ ] Login with password
- [ ] Send OTP
- [ ] Verify OTP
- [ ] Forgot password
- [ ] Reset password
- [ ] Change password
- [ ] Validate token

### Users
- [ ] Get current user profile
- [ ] Get user by ID
- [ ] Update user profile
- [ ] Delete user

### Medications
- [ ] Create medication
- [ ] Get all medications
- [ ] Get active medications only
- [ ] Get medication by ID
- [ ] Update medication
- [ ] Delete medication

### Prescriptions
- [ ] Upload prescription image
- [ ] Get all prescriptions
- [ ] Get prescription by ID
- [ ] Get prescription with medications
- [ ] Delete prescription

---

## ?? Common Issues

### Issue: 401 Unauthorized
```
Solution:
1. Login to get token
2. Copy token from response
3. Set in environment: token = your_token_here
4. Retry request
```

### Issue: 404 Not Found
```
Solution:
1. Check API is running: dotnet run
2. Verify base_url is correct
3. Check endpoint path spelling
```

### Issue: 400 Bad Request
```
Solution:
1. Check request body format
2. Validate required fields
3. Check data types match
4. Review error message in response
```

### Issue: 500 Internal Server Error
```
Solution:
1. Check API console for errors
2. Verify database connection
3. Check migration status
4. Review API logs
```

---

## ?? Additional Resources

### Documentation Files
- `PASSWORD_AUTHENTICATION_IMPLEMENTATION.md` - Password auth details
- `EMAIL_TEMPLATES_IMPLEMENTATION.md` - Email template system
- `AUTHENTICATION_TRACKING_IMPLEMENTATION.md` - Auth tracking
- `OTP_CODE_COLLECTION_COMPLETE.md` - OTP system details
- `USER_REGISTRATION_GUIDE.md` - User registration flow

### Database Scripts
- `AddPasswordFieldsToUsers.sql` - Password fields migration
- `AddAuthenticationTracking.sql` - Auth tracking migration
- `CreateOtpCodesTable.sql` - OTP table creation
- `AddEmailToUsers.sql` - Email field migration

### Configuration
- `appsettings.Development.json` - Local settings
- `appsettings.json` - Production settings

---

## ?? Learning Path

### Beginner
1. Import collection
2. Setup environment
3. Test authentication
4. Test basic CRUD

### Intermediate
5. Add test scripts
6. Chain requests
7. Use collection runner
8. Export results

### Advanced
9. Mock servers
10. CI/CD integration
11. Newman CLI
12. Automated testing

---

## ?? Updates & Maintenance

### Version History
- **v1.0** (2024-02-04) - Initial complete collection
  - 24 endpoints
  - 4 major categories
  - Full authentication support
  - Password reset flow
  - OTP authentication

### Future Enhancements
- [ ] Reminder management endpoints
- [ ] Adherence tracking endpoints
- [ ] Analytics endpoints
- [ ] Push notification endpoints
- [ ] Voice recording endpoints

---

## ?? Support

**Need Help?**
- Review documentation files
- Check API logs
- Test with cURL first
- Verify environment setup

**Found a Bug?**
- Document the issue
- Include request/response
- Note environment details
- Check API version

---

## ? Summary

**You now have:**
- ? Complete Postman collection (24 endpoints)
- ? cURL commands for all APIs
- ? Step-by-step import guide
- ? Environment configuration
- ? Test scripts examples
- ? Troubleshooting guide

**Ready to test all MedRemind APIs! ??**

---

**Created:** 2024-02-04  
**Last Updated:** 2024-02-04  
**Version:** 1.0  
**Total Endpoints:** 24  
**Collection Size:** ~50 KB
