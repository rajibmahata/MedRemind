# Quick Guide - Import APIs to Postman

## ?? Import Postman Collection (Easiest Method)

### Step 1: Import Collection File
1. Open Postman
2. Click **"Import"** button (top-left)
3. Select **"Upload Files"** tab
4. Choose: `MedRemind_Complete_API_Collection.postman_collection.json`
5. Click **"Import"**

? Done! All 25+ API endpoints imported with examples.

---

## ?? Setup Environment Variables

### Step 2: Create Environment
1. In Postman, click **"Environments"** (left sidebar)
2. Click **"+"** to create new environment
3. Name it: **"MedRemind Local"**
4. Add variables:

| Variable | Initial Value | Current Value |
|----------|--------------|---------------|
| `base_url` | `http://localhost:5000` | `http://localhost:5000` |
| `token` | _(leave empty)_ | _(leave empty)_ |
| `user_id` | `1` | `1` |

5. Click **"Save"**
6. Select **"MedRemind Local"** from environment dropdown

---

## ?? Quick Test Flow

### Step 3: Test Authentication

**1. Register User:**
```
POST {{base_url}}/api/users/register
Body:
{
  "phoneNumber": "8420249020",
  "email": "test@example.com",
  "name": "Test User",
  "password": "Test123!@#"
}
```

**2. Login:**
```
POST {{base_url}}/api/auth/login
Body:
{
  "identifier": "test@example.com",
  "password": "Test123!@#"
}
```

**3. Copy Token:**
- From login response, copy the `token` value
- In Postman environment, set `token` variable
- Or use Test script (see below)

**4. Get User Profile:**
```
GET {{base_url}}/api/users/me
Authorization: Bearer {{token}}
```

---

## ?? Auto-Save Token (Recommended)

### Add Test Script to Login Request

1. Open **"1.3 Login with Password"** request
2. Go to **"Tests"** tab
3. Add this script:

```javascript
// Save token to environment
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    if (jsonData.token) {
        pm.environment.set("token", jsonData.token);
        console.log("? Token saved:", jsonData.token);
    }
    if (jsonData.profile && jsonData.profile.id) {
        pm.environment.set("user_id", jsonData.profile.id);
        console.log("? User ID saved:", jsonData.profile.id);
    }
}
```

Now token auto-saves after login! ??

---

## ?? Alternative: Import Individual cURL Commands

### Method 1: Copy-Paste cURL
1. Copy any cURL command from `MEDREMIND_CURL_COMMANDS.md`
2. In Postman, click **"Import"**
3. Select **"Raw text"** tab
4. Paste cURL command
5. Click **"Import"**

### Example:
```bash
curl --location --request POST "http://localhost:5000/api/auth/login" \
--header "Content-Type: application/json" \
--data-raw '{
    "identifier": "john@example.com",
    "password": "MyPass123!"
}'
```

---

## ?? Switch Between Environments

### Create Multiple Environments:

**Local Development:**
```json
{
  "name": "MedRemind Local",
  "base_url": "http://localhost:5000"
}
```

**Staging:**
```json
{
  "name": "MedRemind Staging",
  "base_url": "https://staging-api.medremind.com"
}
```

**Production:**
```json
{
  "name": "MedRemind Production",
  "base_url": "https://api.medremind.com"
}
```

Switch using environment dropdown! ??

---

## ?? Request Organization

The collection is organized into folders:

```
MedRemind API
??? 1. Authentication (7 requests)
?   ??? Send OTP
?   ??? Verify OTP
?   ??? Login with Password
?   ??? Forgot Password
?   ??? Reset Password
?   ??? Change Password
?   ??? Validate Token
?
??? 2. Users (6 requests)
?   ??? Register User (No Password)
?   ??? Register User (With Password)
?   ??? Get Current User
?   ??? Get User by ID
?   ??? Update User Profile
?   ??? Delete User
?
??? 3. Medications (6 requests)
?   ??? Create Medication
?   ??? Get All Medications
?   ??? Get Active Medications
?   ??? Get Medication by ID
?   ??? Update Medication
?   ??? Delete Medication
?
??? 4. Prescriptions (5 requests)
    ??? Upload Prescription
    ??? Get All Prescriptions
    ??? Get Prescription by ID
    ??? Get Prescription with Medications
    ??? Delete Prescription
```

---

## ?? Common Issues & Solutions

### Issue 1: "Could not get response"
**Solution:** Ensure API is running:
```bash
cd backend\MedRemind.API
dotnet run
```

### Issue 2: "401 Unauthorized"
**Solution:** 
- Login first to get token
- Set `token` in environment variables
- Check Authorization header: `Bearer {{token}}`

### Issue 3: "404 Not Found"
**Solution:**
- Check `base_url` is correct
- Verify API endpoint path
- Check if API is running on correct port

### Issue 4: Token expired
**Solution:**
- Login again to get new token
- Token expires after 30 days (default)

---

## ?? Customize Collection

### Add Pre-request Script (Global)
1. Right-click collection ? **"Edit"**
2. Go to **"Pre-request Scripts"** tab
3. Add:

```javascript
// Log request details
console.log("?? Request:", pm.request.url.toString());
console.log("Method:", pm.request.method);
console.log("Token:", pm.environment.get("token"));
```

### Add Test Script (Global)
1. Right-click collection ? **"Edit"**
2. Go to **"Tests"** tab
3. Add:

```javascript
// Auto-test status code
pm.test("Status code is 200 or 201", function () {
    pm.expect(pm.response.code).to.be.oneOf([200, 201]);
});

// Log response time
console.log("?? Response time:", pm.response.responseTime + "ms");
```

---

## ?? Run Collection

### Run All Requests:
1. Right-click collection
2. Select **"Run collection"**
3. Select requests to run
4. Click **"Run MedRemind API"**

### View Results:
- ? Passed tests
- ? Failed tests
- Response times
- Iterations

---

## ?? Pro Tips

### Tip 1: Use Variables Everywhere
```
? Hard-coded: http://localhost:5000/api/users/1
? Variables: {{base_url}}/api/users/{{user_id}}
```

### Tip 2: Chain Requests
Save IDs from responses:
```javascript
// In Tests tab of Create Medication
var jsonData = pm.response.json();
pm.environment.set("medication_id", jsonData.id);

// Use in next request
GET {{base_url}}/api/medications/{{medication_id}}
```

### Tip 3: Organize with Folders
- Group related endpoints
- Add descriptions
- Use meaningful names

### Tip 4: Export & Share
1. Right-click collection ? **"Export"**
2. Choose format: **Collection v2.1**
3. Share JSON file with team

---

## ?? Mobile Testing

### Test from Mobile Device:
1. Find your computer's IP:
   ```bash
   ipconfig  # Windows
   ifconfig  # Mac/Linux
   ```
2. Update environment:
   ```
   base_url: http://192.168.1.100:5000
   ```
3. Run Postman on mobile or use browser

---

## ?? Security Notes

- Never commit environment files with real tokens
- Use `.gitignore` for Postman files
- Rotate tokens regularly
- Use separate environments for prod/staging

---

## ?? Resources

- **cURL Commands:** `MEDREMIND_CURL_COMMANDS.md`
- **Collection File:** `MedRemind_Complete_API_Collection.postman_collection.json`
- **API Documentation:** `backend\MedRemind.API\Docs\`

---

## ? You're All Set!

1. ? Collection imported
2. ? Environment configured
3. ? Token auto-save setup
4. ? Ready to test!

**Happy Testing! ??**

---

**Need Help?**
- Check `MEDREMIND_CURL_COMMANDS.md` for detailed examples
- See `PASSWORD_AUTHENTICATION_IMPLEMENTATION.md` for auth flow
- Review `EMAIL_TEMPLATES_IMPLEMENTATION.md` for email features
