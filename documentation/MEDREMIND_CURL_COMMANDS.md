# MedRemind API - cURL Commands Collection

## Base URL
```bash
BASE_URL="http://localhost:5000"
# or for production
# BASE_URL="https://api.medremind.com"
```

---

## 1. Authentication APIs

### 1.1 Send OTP (SMS/Email)
```bash
curl --location --request POST "${BASE_URL}/api/auth/send-otp" \
--header "Content-Type: application/json" \
--data-raw '{
    "phoneNumber": "8420249020"
}'
```

### 1.2 Verify OTP
```bash
curl --location --request POST "${BASE_URL}/api/auth/verify-otp" \
--header "Content-Type: application/json" \
--data-raw '{
    "phoneNumber": "8420249020",
    "otp": "123456"
}'
```

### 1.3 Login with Password
```bash
curl --location --request POST "${BASE_URL}/api/auth/login" \
--header "Content-Type: application/json" \
--data-raw '{
    "identifier": "john@example.com",
    "password": "MyPass123!"
}'
```

### 1.4 Forgot Password
```bash
curl --location --request POST "${BASE_URL}/api/auth/forgot-password" \
--header "Content-Type: application/json" \
--data-raw '{
    "email": "john@example.com"
}'
```

### 1.5 Reset Password
```bash
curl --location --request POST "${BASE_URL}/api/auth/reset-password" \
--header "Content-Type: application/json" \
--data-raw '{
    "email": "john@example.com",
    "resetToken": "your-reset-token-from-email",
    "newPassword": "NewPass123!",
    "confirmPassword": "NewPass123!"
}'
```

### 1.6 Change Password (Authenticated)
```bash
curl --location --request POST "${BASE_URL}/api/auth/change-password" \
--header "Content-Type: application/json" \
--header "Authorization: Bearer YOUR_JWT_TOKEN" \
--data-raw '{
    "currentPassword": "MyPass123!",
    "newPassword": "NewPass456!",
    "confirmPassword": "NewPass456!"
}'
```

### 1.7 Validate Token
```bash
curl --location --request POST "${BASE_URL}/api/auth/validate-token" \
--header "Content-Type: application/json" \
--data-raw '{
    "token": "YOUR_JWT_TOKEN"
}'
```

---

## 2. User APIs

### 2.1 Register User (Without Password)
```bash
curl --location --request POST "${BASE_URL}/api/users/register" \
--header "Content-Type: application/json" \
--data-raw '{
    "phoneNumber": "8420249020",
    "email": "john@example.com",
    "name": "John Doe",
    "dateOfBirth": "1990-01-15",
    "gender": "Male"
}'
```

### 2.2 Register User (With Password)
```bash
curl --location --request POST "${BASE_URL}/api/users/register" \
--header "Content-Type: application/json" \
--data-raw '{
    "phoneNumber": "8420249020",
    "email": "john@example.com",
    "name": "John Doe",
    "dateOfBirth": "1990-01-15",
    "gender": "Male",
    "password": "MyPass123!"
}'
```

### 2.3 Get Current User Profile
```bash
curl --location --request GET "${BASE_URL}/api/users/me" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 2.4 Get User by ID
```bash
curl --location --request GET "${BASE_URL}/api/users/1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 2.5 Update User Profile
```bash
curl --location --request PUT "${BASE_URL}/api/users/1" \
--header "Content-Type: application/json" \
--header "Authorization: Bearer YOUR_JWT_TOKEN" \
--data-raw '{
    "name": "John Updated",
    "email": "johnupdated@example.com",
    "dateOfBirth": "1990-01-15",
    "gender": "Male"
}'
```

### 2.6 Delete User
```bash
curl --location --request DELETE "${BASE_URL}/api/users/1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 3. Medication APIs

### 3.1 Create Medication
```bash
curl --location --request POST "${BASE_URL}/api/medications" \
--header "Content-Type: application/json" \
--header "Authorization: Bearer YOUR_JWT_TOKEN" \
--data-raw '{
    "userId": 1,
    "name": "Paracetamol",
    "dosage": "500mg",
    "frequency": "Twice daily",
    "startDate": "2024-01-15T00:00:00Z",
    "endDate": "2024-02-15T00:00:00Z",
    "reminderTimes": ["09:00", "21:00"],
    "notes": "Take after meals",
    "prescriptionId": 1
}'
```

### 3.2 Get All Medications for User
```bash
curl --location --request GET "${BASE_URL}/api/medications?userId=1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3.3 Get Active Medications
```bash
curl --location --request GET "${BASE_URL}/api/medications?userId=1&isActive=true" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3.4 Get Medication by ID
```bash
curl --location --request GET "${BASE_URL}/api/medications/1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3.5 Update Medication
```bash
curl --location --request PUT "${BASE_URL}/api/medications/1" \
--header "Content-Type: application/json" \
--header "Authorization: Bearer YOUR_JWT_TOKEN" \
--data-raw '{
    "name": "Paracetamol",
    "dosage": "650mg",
    "frequency": "Three times daily",
    "startDate": "2024-01-15T00:00:00Z",
    "endDate": "2024-02-15T00:00:00Z",
    "reminderTimes": ["08:00", "14:00", "20:00"],
    "notes": "Take after meals",
    "isActive": true
}'
```

### 3.6 Delete Medication
```bash
curl --location --request DELETE "${BASE_URL}/api/medications/1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3.7 Mark Medication as Taken
```bash
curl --location --request POST "${BASE_URL}/api/medications/1/log-dose" \
--header "Content-Type: application/json" \
--header "Authorization: Bearer YOUR_JWT_TOKEN" \
--data-raw '{
    "status": "Taken",
    "notes": "Taken on time"
}'
```

---

## 4. Prescription APIs

### 4.1 Upload Prescription (Multipart Form-Data)
```bash
curl --location --request POST "${BASE_URL}/api/prescriptions/upload" \
--header "Authorization: Bearer YOUR_JWT_TOKEN" \
--form "userId=1" \
--form "file=@/path/to/prescription.jpg" \
--form "doctorName=Dr. Smith" \
--form "hospitalName=City Hospital" \
--form "prescriptionDate=2024-01-15"
```

### 4.2 Get All Prescriptions for User
```bash
curl --location --request GET "${BASE_URL}/api/prescriptions?userId=1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 4.3 Get Prescription by ID
```bash
curl --location --request GET "${BASE_URL}/api/prescriptions/1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 4.4 Get Prescription with Medications
```bash
curl --location --request GET "${BASE_URL}/api/prescriptions/1?includeMedications=true" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 4.5 Delete Prescription
```bash
curl --location --request DELETE "${BASE_URL}/api/prescriptions/1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## 5. Reminder APIs (if implemented)

### 5.1 Get Reminders for Today
```bash
curl --location --request GET "${BASE_URL}/api/reminders?userId=1&date=2024-01-15" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 5.2 Snooze Reminder
```bash
curl --location --request POST "${BASE_URL}/api/reminders/1/snooze" \
--header "Content-Type: application/json" \
--header "Authorization: Bearer YOUR_JWT_TOKEN" \
--data-raw '{
    "snoozeMinutes": 15
}'
```

---

## 6. Adherence/Analytics APIs (if implemented)

### 6.1 Get Adherence Statistics
```bash
curl --location --request GET "${BASE_URL}/api/adherence?userId=1&startDate=2024-01-01&endDate=2024-01-31" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 6.2 Get Medication History
```bash
curl --location --request GET "${BASE_URL}/api/medications/1/history?startDate=2024-01-01&endDate=2024-01-31" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Example Workflow

### Complete User Journey
```bash
# 1. Register User
curl --location --request POST "${BASE_URL}/api/users/register" \
--header "Content-Type: application/json" \
--data-raw '{
    "phoneNumber": "8420249020",
    "email": "john@example.com",
    "name": "John Doe",
    "password": "MyPass123!"
}'

# 2. Login
curl --location --request POST "${BASE_URL}/api/auth/login" \
--header "Content-Type: application/json" \
--data-raw '{
    "identifier": "john@example.com",
    "password": "MyPass123!"
}'
# Save the JWT token from response

# 3. Upload Prescription
curl --location --request POST "${BASE_URL}/api/prescriptions/upload" \
--header "Authorization: Bearer YOUR_JWT_TOKEN" \
--form "userId=1" \
--form "file=@/path/to/prescription.jpg"

# 4. Get Medications (auto-extracted from prescription)
curl --location --request GET "${BASE_URL}/api/medications?userId=1" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"

# 5. Get User Profile
curl --location --request GET "${BASE_URL}/api/users/me" \
--header "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Testing Tips

### Set Base URL Variable
```bash
# For local development
export BASE_URL="http://localhost:5000"

# For production
export BASE_URL="https://api.medremind.com"
```

### Save JWT Token
```bash
# After login, save token
TOKEN="your-jwt-token-here"

# Use in subsequent requests
curl --location --request GET "${BASE_URL}/api/users/me" \
--header "Authorization: Bearer ${TOKEN}"
```

### Test with jq (JSON processor)
```bash
# Pretty print JSON response
curl --location --request GET "${BASE_URL}/api/medications?userId=1" \
--header "Authorization: Bearer ${TOKEN}" | jq '.'

# Extract specific field
curl --location --request POST "${BASE_URL}/api/auth/login" \
--header "Content-Type: application/json" \
--data-raw '{
    "identifier": "john@example.com",
    "password": "MyPass123!"
}' | jq -r '.token'
```

---

## Common Response Codes

- **200 OK** - Successful GET, PUT, DELETE
- **201 Created** - Successful POST (resource created)
- **400 Bad Request** - Invalid input
- **401 Unauthorized** - Missing or invalid token
- **404 Not Found** - Resource not found
- **500 Internal Server Error** - Server error

---

## Postman Import

To import into Postman:

1. **Method 1 - Import as cURL:**
   - Copy any cURL command above
   - In Postman, click "Import" ? "Raw text"
   - Paste the cURL command
   - Click "Import"

2. **Method 2 - Create Collection:**
   - Create a new collection in Postman
   - Add each endpoint manually
   - Set collection variables for BASE_URL and TOKEN

3. **Method 3 - Use Postman Collection (if available):**
   - See: `backend\MedRemind.API\Docs\MedRemind_API_Postman_Collection.json`
   - Import this file directly into Postman

---

## Environment Variables for Postman

Create environment with these variables:

```json
{
  "name": "MedRemind Local",
  "values": [
    {
      "key": "base_url",
      "value": "http://localhost:5000",
      "enabled": true
    },
    {
      "key": "token",
      "value": "",
      "enabled": true
    },
    {
      "key": "user_id",
      "value": "1",
      "enabled": true
    }
  ]
}
```

Then use in Postman:
- URL: `{{base_url}}/api/users/{{user_id}}`
- Header: `Authorization: Bearer {{token}}`

---

**Last Updated:** 2024-02-04  
**API Version:** 1.0
