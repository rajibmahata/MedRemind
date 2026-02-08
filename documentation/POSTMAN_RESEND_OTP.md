# Postman Collection - Resend OTP Endpoints

## New Endpoints to Add

### 1. Resend OTP

**Method:** POST  
**URL:** `{{base_url}}/api/auth/resend-otp`  
**Headers:**
```
Content-Type: application/json
```

**Body (raw JSON):**
```json
{
  "phoneNumber": "8420249020",
  "email": "john@example.com",
  "purpose": "Registration"
}
```

**Tests:**
```javascript
// Test 1: Check status code
pm.test("Status code is 200 or 429", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 429]);
});

// Test 2: Check response structure
pm.test("Response has required fields", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('success');
    pm.expect(jsonData).to.have.property('message').or.property('errorMessage');
});

// Test 3: Check success response
if (pm.response.code === 200) {
    pm.test("Success response has remainingAttempts", function () {
        var jsonData = pm.response.json();
        pm.expect(jsonData.remainingAttempts).to.be.a('number');
    });
}

// Test 4: Check rate limit response
if (pm.response.code === 429) {
    pm.test("Rate limit response has nextResendAvailableAt", function () {
        var jsonData = pm.response.json();
        pm.expect(jsonData.nextResendAvailableAt).to.not.be.null;
    });
}
```

---

### 2. Check Resend Availability

**Method:** GET  
**URL:** `{{base_url}}/api/auth/resend-otp/availability`  
**Params:**
```
phoneNumber: 8420249020
purpose: Registration
```

**Tests:**
```javascript
// Test 1: Check status code
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

// Test 2: Check response structure
pm.test("Response has success field", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('success');
});

// Test 3: Check availability info
pm.test("Has availability information", function () {
    var jsonData = pm.response.json();
    if (jsonData.success) {
        pm.expect(jsonData.remainingAttempts).to.be.a('number');
    } else {
        pm.expect(jsonData).to.have.property('errorMessage');
    }
});
```

---

## Complete Flow Example

### Resend OTP Flow (Add to Collection)

**Folder:** Authentication  
**Order:** After "1.2 Verify OTP"

```
1.2.1 Resend OTP (Rate Limited)
1.2.2 Check Resend Availability
```

---

## Example Requests

### Example 1: Resend OTP for Registration

```bash
POST {{base_url}}/api/auth/resend-otp
{
  "phoneNumber": "8420249020",
  "purpose": "Registration"
}

# Response:
{
  "success": true,
  "message": "OTP has been resent successfully.",
  "remainingAttempts": 4,
  "isEmailOtpSent": true,
  "isSmsOtpSent": false
}
```

### Example 2: Immediate Second Request (Rate Limited)

```bash
POST {{base_url}}/api/auth/resend-otp
{
  "phoneNumber": "8420249020",
  "purpose": "Registration"
}

# Response (429):
{
  "success": false,
  "errorMessage": "Please wait 58 seconds before requesting a new OTP.",
  "nextResendAvailableAt": "2024-02-04T10:46:00Z"
}
```

### Example 3: Check Availability

```bash
GET {{base_url}}/api/auth/resend-otp/availability?phoneNumber=8420249020&purpose=Registration

# Response (Can resend):
{
  "success": true,
  "message": "Resend is available",
  "remainingAttempts": 3
}

# Response (Must wait):
{
  "success": false,
  "errorMessage": "Please wait before requesting a new OTP.",
  "nextResendAvailableAt": "2024-02-04T10:46:00Z",
  "remainingAttempts": 3
}
```

---

## Pre-request Script (Optional)

Add to collection or folder level:

```javascript
// Log request
console.log("?? Resend OTP Request");
console.log("Phone:", pm.request.body.raw ? 
    JSON.parse(pm.request.body.raw).phoneNumber : 
    pm.request.url.query.get("phoneNumber"));
console.log("Timestamp:", new Date().toISOString());
```

---

## Environment Variables

Add to your environment:

```json
{
  "base_url": "http://localhost:5000",
  "phone_number": "8420249020",
  "test_email": "john@example.com"
}
```

Use in requests:
```json
{
  "phoneNumber": "{{phone_number}}",
  "email": "{{test_email}}",
  "purpose": "Registration"
}
```

---

## Collection Structure

```
MedRemind API
??? 1. Authentication
?   ??? 1.1 Send OTP
?   ??? 1.2 Verify OTP
?   ??? 1.2.1 Resend OTP              ? NEW
?   ??? 1.2.2 Check Resend Availability ? NEW
?   ??? 1.3 Login with Password
?   ??? 1.4 Forgot Password
?   ??? 1.5 Reset Password
?   ??? 1.6 Change Password
?   ??? 1.7 Validate Token
```

---

## Runner Configuration

To test rate limiting with Collection Runner:

1. Select folder "1. Authentication"
2. Include only:
   - 1.2.1 Resend OTP (run 3 times)
   - 1.2.2 Check Resend Availability
3. Set delay: 0ms (to trigger rate limit)
4. Set iterations: 3
5. Run

**Expected Result:**
- First request: 200 (Success)
- Second request: 429 (Rate limited)
- Third request: 429 (Rate limited)
- Availability check: Wait required

---

## Documentation in Postman

Add to request description:

### Resend OTP
```
Resend OTP with rate limiting and validation.

**Rate Limits:**
- Minimum 60 seconds between requests
- Maximum 5 requests per 24 hours

**Response Codes:**
- 200: OTP resent successfully
- 400: Validation error
- 429: Rate limit exceeded
- 500: Server error

**Response Fields:**
- success: boolean
- message: success message
- errorMessage: error details
- remainingAttempts: attempts left today
- nextResendAvailableAt: when resend available
- isEmailOtpSent: email OTP sent flag
- isSmsOtpSent: SMS OTP sent flag
```

### Check Availability
```
Check if resend OTP is currently available.

Use this endpoint before showing "Resend" button to user.

**Query Parameters:**
- phoneNumber (required): User's phone number
- purpose (optional): Registration, Login, PasswordReset

**Response:**
- success: true if can resend now
- remainingAttempts: attempts left today
- nextResendAvailableAt: ISO datetime when available
```

---

## Export for Sharing

1. Right-click collection
2. Select "Export"
3. Choose "Collection v2.1"
4. Save as `MedRemind_API_With_Resend_OTP.json`
5. Share with team

---

## Tips

### Tip 1: Auto-wait for Rate Limit
```javascript
// In test script
if (pm.response.code === 429) {
    var jsonData = pm.response.json();
    var waitMs = new Date(jsonData.nextResendAvailableAt) - new Date();
    console.log(`? Waiting ${Math.ceil(waitMs/1000)} seconds...`);
    setTimeout(() => {
        pm.execution.setNextRequest("1.2.1 Resend OTP");
    }, waitMs);
}
```

### Tip 2: Save Remaining Attempts
```javascript
// After resend OTP
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    pm.environment.set("remaining_attempts", jsonData.remainingAttempts);
    console.log(`? ${jsonData.remainingAttempts} attempts remaining`);
}
```

### Tip 3: Track OTP Requests
```javascript
// Pre-request
var count = pm.environment.get("otp_request_count") || 0;
pm.environment.set("otp_request_count", count + 1);
console.log(`?? Request #${count + 1}`);
```

---

**Status:** Ready to import ?
