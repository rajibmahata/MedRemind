# ? Postman Collection Created

Complete Postman collection for MedRemind API with all endpoints, authentication, and test scripts.

---

## ?? What's Been Created

### 1. Postman Collection JSON
**File:** `MedRemind_API_Postman_Collection.json` (28 KB)

**Includes:**
- ? All 22+ API endpoints organized by resource
- ? JWT Bearer token authentication
- ? Automatic token management
- ? Test scripts for validation
- ? Variable management (baseUrl, authToken, etc.)
- ? Example request bodies
- ? Query parameter examples
- ? File upload configuration

### 2. Postman Guide
**File:** `POSTMAN_COLLECTION_GUIDE.md` (10 KB)

**Includes:**
- Import instructions
- Setup guide
- Quick start tutorial
- Usage examples
- Troubleshooting
- Best practices
- Workflow examples

---

## ?? Collection Contents

### Organized by Resource

**1. Authentication (3 endpoints)**
- Send OTP
- Verify OTP (auto-saves token)
- Validate Token

**2. Medications (5 endpoints)**
- Get All Medications
- Get Medication by ID
- Create Medication
- Update Medication
- Delete Medication

**3. Prescriptions (4 endpoints)**
- Get All Prescriptions
- Get Prescription by ID
- Upload Prescription (with file)
- Delete Prescription

**4. Reminders (5 endpoints)**
- Get All Reminders
- Get Reminder by ID
- Create Reminder
- Update Reminder
- Delete Reminder

**5. Adherence (3 endpoints)**
- Get Adherence Statistics
- Log Medication Taken
- Get Adherence History

**Total: 20+ endpoints**

---

## ? Key Features

### Automatic Token Management
```javascript
// Token automatically saved after OTP verification
pm.collectionVariables.set("authToken", jsonData.token);

// All protected endpoints use the token
Authorization: Bearer {{authToken}}
```

### Test Scripts
Each request includes automated tests:
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Response has required fields", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.id).to.exist;
});
```

### Variable Management
Automatically saves IDs for related requests:
- `authToken` - JWT token
- `medicationId` - Last created medication
- `prescriptionId` - Last created prescription
- `reminderId` - Last created reminder

### Pre-configured Examples
All requests include example bodies and parameters:
```json
{
  "name": "Aspirin",
  "dosage": "500mg",
  "frequency": "Twice daily",
  "userId": 1
}
```

---

## ?? How to Use

### 1. Import Collection

**In Postman:**
1. Click **Import** button
2. Select **File** tab
3. Choose `MedRemind_API_Postman_Collection.json`
4. Click **Import**

### 2. Configure Base URL

The collection uses `http://localhost:5124` by default.

**To change:**
1. Click collection name
2. Go to **Variables** tab
3. Update `baseUrl` value
4. Click **Save**

### 3. Authenticate

**Run these requests in order:**
1. **Send OTP** - Get OTP on your phone
2. **Verify OTP** - Enter OTP, get JWT token (auto-saved)

### 4. Test Endpoints

All protected endpoints now work automatically with your token!

**Example flow:**
```
1. Send OTP
2. Verify OTP (token saved)
3. Create Medication ?
4. Get All Medications ?
5. Create Reminder ?
6. Log Medication Taken ?
7. Get Adherence Statistics ?
```

---

## ?? Request Examples

### Authentication Flow

```bash
# 1. Send OTP
POST /api/Auth/send-otp
{
  "phoneNumber": "+919876543210"
}

# 2. Verify OTP
POST /api/Auth/verify-otp
{
  "phoneNumber": "+919876543210",
  "otp": "123456"
}
# Response: { "token": "eyJhbG..." }
# Token automatically saved!

# 3. Use Protected Endpoint
GET /api/Medications
Authorization: Bearer {{authToken}}
# Token automatically included!
```

### Create & Update Flow

```bash
# 1. Create
POST /api/Medications
Authorization: Bearer {{authToken}}
{
  "name": "Aspirin",
  "dosage": "500mg",
  "userId": 1
}
# Response: { "id": 1, ... }
# ID automatically saved to medicationId!

# 2. Update
PUT /api/Medications/{{medicationId}}
Authorization: Bearer {{authToken}}
{
  "id": {{medicationId}},
  "name": "Aspirin",
  "dosage": "1000mg"
}
```

### File Upload

```bash
POST /api/Prescriptions/upload
Authorization: Bearer {{authToken}}
Content-Type: multipart/form-data

Form Data:
- file: [Select File]
- userId: 1
```

---

## ?? Testing Features

### Automated Tests

Each request validates:
- ? HTTP status codes
- ? Response structure
- ? Required fields present
- ? Data types correct

**View Results:**
After sending request, check **Test Results** tab (bottom panel)

### Example Test Output

```
? Status code is 200
? Response is array
? Response has required fields
? Token saved to variable

Tests Passed: 4/4
```

### Collection Runner

Run all requests in sequence:
1. Click collection name
2. Click **Run** button
3. Select requests
4. Click **Run MedRemind API**

---

## ?? Documentation Files

All documentation in **Docs** folder:

| Document | Purpose | Size |
|----------|---------|------|
| **MedRemind_API_Postman_Collection.json** | Postman collection | 28 KB |
| **POSTMAN_COLLECTION_GUIDE.md** | Complete usage guide | 10 KB |
| **JWT_AUTHENTICATION.md** | JWT auth guide | 13 KB |
| **JWT_IMPLEMENTATION_COMPLETE.md** | JWT summary | 8 KB |
| **API_REFERENCE.md** | Endpoint reference | 7 KB |
| **CURL_EXAMPLES.md** | cURL testing | 6 KB |
| **SWAGGER_SETUP.md** | API setup | 8 KB |
| **INDEX.md** | Documentation hub | 7 KB |

---

## ?? Variables Reference

### Collection Variables

| Variable | Type | Description |
|----------|------|-------------|
| `baseUrl` | string | API base URL (default: http://localhost:5124) |
| `authToken` | string | JWT bearer token (auto-saved) |
| `medicationId` | string | Last created medication ID |
| `prescriptionId` | string | Last created prescription ID |
| `reminderId` | string | Last created reminder ID |

### Using Variables

**In URL:**
```
{{baseUrl}}/api/Medications/{{medicationId}}
```

**In Body:**
```json
{
  "medicationId": {{medicationId}},
  "userId": 1
}
```

**In Headers:**
```
Authorization: Bearer {{authToken}}
```

---

## ?? Pro Tips

### 1. Create Environments

Set up different environments for different servers:
- Development: `http://localhost:5124`
- Staging: `https://staging-api.medremind.com`
- Production: `https://api.medremind.com`

### 2. Save Responses

Save successful responses as examples:
1. Send request
2. Click **Save Response**
3. Click **Save as Example**

### 3. Generate Code

Generate code for your app:
1. Open request
2. Click **Code** button (right sidebar)
3. Select language (C#, JavaScript, etc.)
4. Copy code

### 4. Use Mock Servers

Create mock server for offline testing:
1. Right-click collection
2. Select **Mock Collection**
3. Configure responses

### 5. Export Collection

Share with team:
1. Right-click collection
2. Select **Export**
3. Choose v2.1 format
4. Share JSON file

---

## ?? Common Issues

### Issue: Token Not Saved

**Solution:**
1. Check test script in "Verify OTP" request
2. Or manually copy token:
   - Copy token from response
   - Go to Variables tab
   - Paste into `authToken`
   - Save

### Issue: 401 Unauthorized

**Solution:**
1. Re-run "Verify OTP" to get fresh token
2. Check token is set in Variables
3. Verify token hasn't expired

### Issue: File Upload Fails

**Solution:**
1. Ensure Body type is **form-data**
2. Set file field type to **File** (not Text)
3. Click **Select Files** and choose image
4. Verify userId is set as Text

---

## ?? Next Steps

### For Testing
1. ? Import collection
2. ? Configure baseUrl
3. ?? Authenticate
4. ?? Test each resource
5. ?? Run Collection Runner

### For Development
1. ?? Use for manual testing
2. ?? Generate code snippets
3. ?? Save example responses
4. ?? Share with team

### For CI/CD
1. ?? Install Newman: `npm install -g newman`
2. ?? Run tests: `newman run MedRemind_API_Postman_Collection.json`
3. ?? Integrate in pipeline
4. ?? Generate reports

---

## ? What You Get

With this Postman collection:
- ? **22+ pre-configured requests**
- ? **Automatic authentication** (token management)
- ? **Test scripts** for validation
- ? **Variable management** for IDs
- ? **Example bodies** for all requests
- ? **File upload** configuration
- ? **Query parameters** examples
- ? **Organized folders** by resource
- ? **Comprehensive documentation**

---

## ?? Support

**Collection Issues:**
- See [POSTMAN_COLLECTION_GUIDE.md](POSTMAN_COLLECTION_GUIDE.md)

**API Issues:**
- See [JWT_AUTHENTICATION.md](JWT_AUTHENTICATION.md)
- See [API_REFERENCE.md](API_REFERENCE.md)

**General Help:**
- See [INDEX.md](INDEX.md)

---

## ?? Summary

**Postman collection is ready to use!**

**Quick Start:**
1. Import `MedRemind_API_Postman_Collection.json`
2. Run "Send OTP" ? "Verify OTP"
3. Test any endpoint!

**Documentation:**
- Complete guide: [POSTMAN_COLLECTION_GUIDE.md](POSTMAN_COLLECTION_GUIDE.md)
- API reference: [API_REFERENCE.md](API_REFERENCE.md)

**Happy Testing! ??**
