# Postman Collection Guide

Complete guide for using the MedRemind API Postman collection.

---

## ?? Import Collection

### Option 1: Import from File

1. Open Postman
2. Click **Import** button (top left)
3. Select **File** tab
4. Choose `MedRemind_API_Postman_Collection.json`
5. Click **Import**

### Option 2: Import from Link

If the collection is hosted online, you can import directly via URL.

---

## ?? Setup

### Configure Variables

The collection includes the following variables:

| Variable | Default Value | Description |
|----------|---------------|-------------|
| `baseUrl` | `http://localhost:5124` | API base URL |
| `authToken` | *auto-populated* | JWT authentication token |
| `medicationId` | *auto-populated* | Last created medication ID |
| `prescriptionId` | *auto-populated* | Last created prescription ID |
| `reminderId` | *auto-populated* | Last created reminder ID |

**To change baseUrl:**
1. Click on collection name
2. Go to **Variables** tab
3. Update `baseUrl` value
4. Click **Save**

---

## ?? Quick Start

### Step 1: Authenticate

#### 1.1 Send OTP
- Endpoint: `POST /api/Auth/send-otp`
- Body:
  ```json
  {
    "phoneNumber": "+919876543210"
  }
  ```
- Click **Send**
- Check your phone for OTP

#### 1.2 Verify OTP
- Endpoint: `POST /api/Auth/verify-otp`
- Body:
  ```json
  {
    "phoneNumber": "+919876543210",
    "otp": "123456"
  }
  ```
- Click **Send**
- **Token automatically saved** to `authToken` variable

### Step 2: Test Protected Endpoints

Now you can use any protected endpoint. The `authToken` will be automatically included in the Authorization header.

Example:
- Endpoint: `GET /api/Medications`
- Click **Send**
- View your medications

---

## ?? Collection Structure

### 1. Authentication (Public)
- ? Send OTP
- ? Verify OTP (saves token)
- ? Validate Token

### 2. Medications (Protected)
- Get All Medications
- Get Medication by ID
- Create Medication (saves ID)
- Update Medication
- Delete Medication

### 3. Prescriptions (Protected)
- Get All Prescriptions
- Get Prescription by ID
- Upload Prescription (saves ID)
- Delete Prescription

### 4. Reminders (Protected)
- Get All Reminders
- Get Reminder by ID
- Create Reminder (saves ID)
- Update Reminder
- Delete Reminder

### 5. Adherence (Protected)
- Get Adherence Statistics
- Log Medication Taken
- Get Adherence History

---

## ?? Authentication

### Auto-Authentication

Protected endpoints automatically use the `authToken` variable:

```
Authorization: Bearer {{authToken}}
```

### Manual Token

To use a different token:
1. Go to collection **Variables**
2. Update `authToken` value
3. Save

Or override per request:
1. Open request
2. Go to **Authorization** tab
3. Select **Bearer Token**
4. Enter token manually

---

## ?? Testing Features

### Automated Tests

Each request includes test scripts that:
- Verify response status codes
- Validate response structure
- Save IDs to variables for subsequent requests

**View test results:**
- After sending request
- Check **Test Results** tab (bottom panel)

### Example Test Results

```
? Status code is 200
? Response has token
? Token saved to variable
```

---

## ?? Usage Examples

### Complete Medication Flow

```
1. Send OTP ? Verify OTP (get token)
2. Create Medication (saves medication ID)
3. Get All Medications (view created medication)
4. Create Reminder for medication
5. Log Medication Taken
6. Get Adherence Statistics
```

### Prescription Upload Flow

```
1. Authenticate (Send OTP ? Verify OTP)
2. Upload Prescription
   - Select file in Body ? form-data
   - Choose image file
   - Set userId
3. Get Prescription by ID (view OCR results)
4. Get All Medications (see extracted medications)
```

---

## ?? Advanced Usage

### Environment Variables

Create different environments for different servers:

**Development Environment:**
```
baseUrl: http://localhost:5124
```

**Staging Environment:**
```
baseUrl: https://staging-api.medremind.com
```

**Production Environment:**
```
baseUrl: https://api.medremind.com
```

**To create environment:**
1. Click **Environments** (left sidebar)
2. Click **+** to create new
3. Name it (e.g., "Development")
4. Add variables
5. Select environment from dropdown (top right)

### Pre-request Scripts

Add scripts to run before each request:

```javascript
// Log request details
console.log('Sending request to:', pm.request.url);
console.log('Method:', pm.request.method);

// Generate timestamp
pm.collectionVariables.set('timestamp', new Date().toISOString());
```

### Test Scripts

Add custom test assertions:

```javascript
// Check response time
pm.test("Response time < 500ms", function () {
    pm.expect(pm.response.responseTime).to.be.below(500);
});

// Validate JSON schema
pm.test("Response has valid schema", function () {
    var schema = {
        type: "object",
        properties: {
            id: { type: "number" },
            name: { type: "string" }
        },
        required: ["id", "name"]
    };
    pm.response.to.have.jsonSchema(schema);
});
```

---

## ?? Request Examples

### GET Request with Query Parameters

```
GET /api/Medications?userId=1&isActive=true
```

**In Postman:**
1. Open request
2. Go to **Params** tab
3. Add key-value pairs:
   - `userId`: `1`
   - `isActive`: `true`

### POST Request with JSON Body

```
POST /api/Medications
Content-Type: application/json

{
  "name": "Aspirin",
  "dosage": "500mg",
  "frequency": "Twice daily"
}
```

**In Postman:**
1. Open request
2. Go to **Body** tab
3. Select **raw** and **JSON**
4. Paste JSON data

### File Upload (Multipart Form Data)

```
POST /api/Prescriptions/upload
Content-Type: multipart/form-data
```

**In Postman:**
1. Open request
2. Go to **Body** tab
3. Select **form-data**
4. Add fields:
   - `file`: Click **Select Files** and choose image
   - `userId`: Enter value as Text

---

## ?? Troubleshooting

### Issue: 401 Unauthorized

**Cause:** Missing or invalid token

**Solutions:**
1. Re-authenticate: Run `Verify OTP` request
2. Check `authToken` variable is set
3. Verify token hasn't expired (30 days default)

### Issue: Token Not Saved

**Cause:** Test script didn't run

**Solutions:**
1. Check **Test Results** tab for errors
2. Ensure test script is present in `Verify OTP` request
3. Manually copy token to `authToken` variable:
   - Copy token from response
   - Go to collection **Variables**
   - Paste into `authToken` current value
   - Save

### Issue: Request Fails

**Cause:** API not running or wrong URL

**Solutions:**
1. Verify API is running: `dotnet run`
2. Check `baseUrl` variable matches your API URL
3. Test with browser: Navigate to `{baseUrl}/swagger`

### Issue: File Upload Fails

**Cause:** Wrong content type or file not selected

**Solutions:**
1. Ensure **Body** type is **form-data**
2. Set `file` field type to **File** (not Text)
3. Click **Select Files** and choose an image
4. Verify file size isn't too large

### Issue: CORS Error

**Cause:** Browser security (if using Postman web)

**Solution:**
- Use Postman desktop app instead of web version
- API has CORS enabled, but browser may still block

---

## ?? Tips & Best Practices

### 1. Use Collection Runner

Run multiple requests in sequence:
1. Click collection name
2. Click **Run** button
3. Select requests to run
4. Click **Run [Collection Name]**

### 2. Save Responses as Examples

Save successful responses for reference:
1. Send request
2. Click **Save Response**
3. Click **Save as Example**
4. Name the example

### 3. Organize with Folders

The collection is already organized by resource:
- Authentication
- Medications
- Prescriptions
- Reminders
- Adherence

### 4. Use Mock Servers

Create mock server for testing:
1. Right-click collection
2. Select **Mock Collection**
3. Configure mock responses

### 5. Generate Code

Generate client code from requests:
1. Open request
2. Click **Code** button (right sidebar)
3. Select language (JavaScript, C#, etc.)
4. Copy generated code

---

## ?? Workflow Examples

### Daily Testing Workflow

```
1. Start API server
2. Open Postman
3. Select "Development" environment
4. Run "Authentication" folder
5. Test specific feature (e.g., Medications)
6. Review test results
7. Check API logs
```

### Integration Testing Workflow

```
1. Run "Send OTP" ? Get OTP from logs/phone
2. Run "Verify OTP" ? Token saved
3. Run "Create Medication" ? ID saved
4. Run "Create Reminder" ? Links to medication
5. Run "Log Medication Taken"
6. Run "Get Adherence Statistics" ? Verify data
```

### API Exploration Workflow

```
1. Authenticate
2. Open Swagger UI (baseUrl/swagger)
3. Compare Swagger vs Postman
4. Test edge cases in Postman
5. Document findings
```

---

## ?? Additional Resources

### Postman Documentation
- **Learning Center**: https://learning.postman.com/
- **API Testing**: https://learning.postman.com/docs/writing-scripts/test-scripts/
- **Variables**: https://learning.postman.com/docs/sending-requests/variables/

### MedRemind API Documentation
- **[JWT_AUTHENTICATION.md](JWT_AUTHENTICATION.md)** - Authentication guide
- **[API_REFERENCE.md](API_REFERENCE.md)** - Endpoint reference
- **[SWAGGER_SETUP.md](SWAGGER_SETUP.md)** - API setup guide

### Related Tools
- **Newman**: CLI tool to run Postman collections
  ```bash
  npm install -g newman
  newman run MedRemind_API_Postman_Collection.json
  ```

---

## ? Checklist

Before starting:
- [ ] API server is running
- [ ] Postman installed (desktop version recommended)
- [ ] Collection imported
- [ ] `baseUrl` variable configured
- [ ] Phone ready to receive OTP

For testing:
- [ ] Authenticate (get token)
- [ ] Test each endpoint group
- [ ] Review test results
- [ ] Check for errors in API logs
- [ ] Save successful responses as examples

---

## ?? Support

**Issues with Collection:**
- Check this guide first
- Review test scripts in requests
- Check Postman console for errors

**Issues with API:**
- Check API is running
- Review [JWT_AUTHENTICATION.md](JWT_AUTHENTICATION.md)
- Check API logs in terminal
- Test with Swagger UI

**GitHub:**
- Repository: https://github.com/rajibmahata/MedRemind
- Issues: https://github.com/rajibmahata/MedRemind/issues

---

## ?? Summary

You now have a complete Postman collection with:
- ? All API endpoints organized by resource
- ? Automatic token management
- ? Test scripts for validation
- ? Variable management for IDs
- ? Example requests with proper bodies
- ? Documentation for each endpoint

**Happy Testing! ??**

Start with: **Authentication ? Medications ? Test other resources**
