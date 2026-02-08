# Postman Collection Import Guide

## ?? What You'll Import

1. **MedRemind_Complete_Collection_v2.json** - Complete API collection (40+ endpoints)
2. **MedRemind_Local_Environment.json** - Environment variables for local testing

---

## ?? Quick Start (3 Steps)

### Step 1: Import Collection
1. Open Postman
2. Click **Import** button (top left)
3. Drag and drop `MedRemind_Complete_Collection_v2.json`
4. Click **Import**

### Step 2: Import Environment
1. Click **Import** again
2. Drag and drop `MedRemind_Local_Environment.json`
3. Click **Import**
4. Select "MedRemind - Local" from environment dropdown (top right)

### Step 3: Start Testing
1. Start your API: `dotnet run --project backend/MedRemind.API`
2. Open collection folder "1. Authentication"
3. Run "1.1 Send OTP"
4. ? Done!

---

## ?? Collection Structure

```
MedRemind API - Complete Collection
??? 1. Authentication (9 endpoints)
?   ??? 1.1 Send OTP
?   ??? 1.2 Verify OTP
?   ??? 1.3 Resend OTP ? NEW
?   ??? 1.4 Check Resend Availability ? NEW
?   ??? 1.5 Login with Password
?   ??? 1.6 Forgot Password
?   ??? 1.7 Reset Password
?   ??? 1.8 Change Password
?   ??? 1.9 Validate Token
?
??? 2. User Management (5 endpoints)
?   ??? 2.1 Register User
?   ??? 2.2 Get Current User Profile
?   ??? 2.3 Update User Profile
?   ??? 2.4 Get User by ID
?   ??? 2.5 Delete User
?
??? 3. Prescriptions (8 endpoints)
?   ??? 3.1 Upload Prescription
?   ??? 3.2 Get All Prescriptions
?   ??? 3.3 Get Prescription by ID
?   ??? 3.4 Get Prescriptions by User
?   ??? 3.5 Update Prescription Status
?   ??? 3.6 Delete Prescription
?   ??? 3.7 Reprocess Prescription
?   ??? 3.8 Get OCR Results
?
??? 4. Medications (9 endpoints)
?   ??? 4.1 Get All Medications
?   ??? 4.2 Get Medication by ID
?   ??? 4.3 Get Medications by Prescription
?   ??? 4.4 Get Medications by User
?   ??? 4.5 Create Medication
?   ??? 4.6 Update Medication
?   ??? 4.7 Delete Medication
?   ??? 4.8 Get Active Medications
?   ??? 4.9 Mark Medication as Taken
?
??? 5. Test Flows (1 flow with 4 steps)
    ??? 5.1 Complete Registration Flow
        ??? Step 1: Register
        ??? Step 2: Send OTP
        ??? Step 3: Verify OTP
        ??? Step 4: Get Profile
```

**Total:** 31+ endpoints organized in 5 folders

---

## ?? Environment Variables

The environment includes these variables:

| Variable | Default Value | Description |
|----------|---------------|-------------|
| `base_url` | `http://localhost:5000` | API base URL |
| `test_phone` | `8420249020` | Test phone number |
| `test_email` | `test@example.com` | Test email |
| `test_password` | `SecurePass123!` | Test password |
| `token` | (auto-set) | Auth token (set after login) |
| `test_user_id` | `1` | Test user ID |
| `prescription_id` | (auto-set) | Set after upload |
| `medication_id` | (auto-set) | Set after creation |

---

## ?? Quick Test Scenarios

### Scenario 1: Test Authentication
```
1. Run "1.1 Send OTP"
   ? OTP sent to phone/email

2. Run "1.2 Verify OTP"
   ? Token saved automatically
   
3. Run "2.2 Get Current User Profile"
   ? Profile retrieved with token
```

### Scenario 2: Test Resend OTP
```
1. Run "1.1 Send OTP"
   ? OTP sent

2. Run "1.4 Check Resend Availability"
   ? Shows if can resend

3. Run "1.3 Resend OTP"
   ? OTP resent (or rate limited)
   
4. Check response:
   - 200: Success, OTP resent
   - 429: Rate limited, wait time shown
```

### Scenario 3: Complete Registration
```
1. Go to folder "5. Test Flows"
2. Open "5.1 Complete Registration Flow"
3. Run all steps in order:
   ? Register ? Send OTP ? Verify ? Get Profile
```

### Scenario 4: Upload & Process Prescription
```
1. Run "3.1 Upload Prescription"
   ? prescription_id saved automatically

2. Run "3.8 Get OCR Results"
   ? View extracted text

3. Run "4.3 Get Medications by Prescription"
   ? View extracted medications
```

---

## ?? Automated Tests

The collection includes automatic tests:

### Response Time Test (All Requests)
```javascript
pm.test("Response time is less than 5000ms", function () {
    pm.expect(pm.response.responseTime).to.be.below(5000);
});
```

### Authentication Tests
```javascript
// After "1.2 Verify OTP"
pm.test("Response has token", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('token');
    pm.environment.set("token", jsonData.token); // Auto-save token
});
```

### Resend OTP Tests
```javascript
// After "1.3 Resend OTP"
pm.test("Status code is 200 or 429", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 429]);
});

if (pm.response.code === 200) {
    pm.test("Success response has remainingAttempts", function () {
        var jsonData = pm.response.json();
        pm.expect(jsonData.remainingAttempts).to.be.a('number');
    });
}

if (pm.response.code === 429) {
    pm.test("Rate limit response has nextResendAvailableAt", function () {
        var jsonData = pm.response.json();
        pm.expect(jsonData.nextResendAvailableAt).to.not.be.null;
    });
}
```

---

## ?? Running Collection with Runner

### Test All Endpoints
1. Click **Runner** button (top bar)
2. Select "MedRemind API - Complete Collection"
3. Select "MedRemind - Local" environment
4. Set iterations: 1
5. Set delay: 500ms
6. Click **Run**

### Test Specific Folder
1. Open Runner
2. Expand collection
3. Select specific folder (e.g., "1. Authentication")
4. Click **Run**

### Test Registration Flow
1. Open Runner
2. Select "5. Test Flows" ? "5.1 Complete Registration Flow"
3. Click **Run**
4. View step-by-step results

---

## ?? Customization

### Change Base URL
1. Click environment dropdown
2. Click eye icon (???) next to "MedRemind - Local"
3. Edit `base_url` to your server
4. Save

### Add New Environment (Staging/Production)
1. Click environment dropdown
2. Click **+** to create new
3. Name it "MedRemind - Staging"
4. Add variables:
   ```
   base_url: https://api-staging.medremind.com
   test_phone: 8420249020
   test_email: test@example.com
   ```
5. Save

### Modify Test Data
1. Edit environment variables:
   - `test_phone` - Your test phone
   - `test_email` - Your test email
   - `test_password` - Your test password

---

## ?? Tips & Tricks

### Tip 1: Auto-Save Tokens
The collection automatically saves tokens after login:
- Verify OTP ? saves `token`
- Upload Prescription ? saves `prescription_id`
- Create Medication ? saves `medication_id`

### Tip 2: View Variables
Click eye icon (???) next to environment to view all saved variables.

### Tip 3: Copy as cURL
Right-click any request ? Code ? cURL ? Copy

### Tip 4: Use Console
View ? Show Postman Console (Alt+Ctrl+C) to see:
- Request/response logs
- Test results
- Variable changes

### Tip 5: Pre-request Scripts
Collection includes pre-request scripts that:
- Log request info
- Add timestamps
- Can be customized

---

## ?? Troubleshooting

### Issue: "Could not get response"
**Solution:**
1. Check API is running: `dotnet run --project backend/MedRemind.API`
2. Verify `base_url` in environment
3. Check firewall settings

### Issue: "Unauthorized (401)"
**Solution:**
1. Run "1.2 Verify OTP" first
2. Check token is saved (eye icon)
3. Ensure "MedRemind - Local" environment is selected

### Issue: "Token expired"
**Solution:**
1. Re-run "1.2 Verify OTP" or "1.5 Login with Password"
2. Token will be auto-saved

### Issue: "Rate limited (429)"
**Solution:**
1. Wait 60 seconds
2. Check response for `nextResendAvailableAt`
3. Use "1.4 Check Resend Availability" first

### Issue: "Prescription not found"
**Solution:**
1. Run "3.1 Upload Prescription" first
2. Check `prescription_id` is saved
3. Use correct user token

---

## ?? Testing Checklist

### Quick Test (5 minutes)
- [ ] Import collection & environment
- [ ] Start API server
- [ ] Run "1.1 Send OTP"
- [ ] Run "1.2 Verify OTP"
- [ ] Run "2.2 Get Profile"

### Standard Test (20 minutes)
- [ ] Test authentication flow
- [ ] Test resend OTP
- [ ] Test user registration
- [ ] Test profile update
- [ ] Test password change

### Complete Test (1 hour)
- [ ] Run entire collection with Runner
- [ ] Test all folders individually
- [ ] Test complete registration flow
- [ ] Upload and process prescription
- [ ] Create and manage medications
- [ ] Test error scenarios

---

## ?? Related Files

### Documentation
- `documentation/cURLs/complete-api-collection.curl` - cURL examples
- `RESEND_OTP_START_HERE.md` - Resend OTP feature guide
- `POSTMAN_RESEND_OTP.md` - Detailed Postman guide

### Collection Files
- `MedRemind_Complete_Collection_v2.json` - Main collection (this one)
- `MedRemind_Local_Environment.json` - Local environment
- `backend/MedRemind.API/Docs/MedRemind_API_Postman_Collection.json` - Old collection

---

## ?? What's New in v2

### New Features
? **Resend OTP Endpoint** - With rate limiting tests  
? **Check Availability Endpoint** - Pre-check before resend  
? **Automated Tests** - All requests include tests  
? **Auto-Save Variables** - Token, IDs auto-saved  
? **Complete Flows** - Registration flow included  
? **Pre-request Scripts** - Logging and timestamps  

### Improvements
? Better organization (5 folders)  
? 31+ endpoints (vs 20+ in v1)  
? Complete test coverage  
? Environment variables included  
? Runner-friendly structure  

---

## ?? Support

### Need Help?
1. Check this guide
2. Review API documentation
3. Check console logs (Alt+Ctrl+C)
4. Verify environment variables

### Report Issues
1. Note which endpoint failed
2. Check request/response in console
3. Verify environment is selected
4. Check API logs

---

## ? Summary

### What You Have
? Complete Postman collection (31+ endpoints)  
? Local environment setup  
? Automated tests  
? Complete flows  
? Import-ready files  

### Quick Import Steps
1. Import `MedRemind_Complete_Collection_v2.json`
2. Import `MedRemind_Local_Environment.json`
3. Select environment
4. Start testing!

### File Locations
```
documentation/postman/
??? MedRemind_Complete_Collection_v2.json    ? Import this
??? MedRemind_Local_Environment.json         ? Import this
??? POSTMAN_IMPORT_GUIDE.md                 (This file)
```

---

**Created:** 2024-02-04  
**Version:** 2.0  
**Endpoints:** 31+  
**Status:** ? Ready to Import  
**Format:** Postman Collection v2.1
