# ?? MedRemind API Testing Guide

Complete cURL commands and Postman collection for testing all MedRemind backend APIs.

---

## ?? **Files Created**

### 1. **cURL Collection**
- **File**: `documentation/cURLs/medremind-complete-api-collection.curl`
- **Format**: Plain text cURL commands
- **Use Case**: Command-line testing, quick API tests

### 2. **Postman Collection**
- **File**: `documentation/Postman/MedRemind_Complete_API_Collection.postman_collection.json`
- **Format**: Postman Collection v2.1
- **Use Case**: GUI testing, automated test suites

---

## ?? **Quick Start**

### **Import Postman Collection**

1. **Open Postman**
2. Click **Import** button
3. Select **File** ? Choose `MedRemind_Complete_API_Collection.postman_collection.json`
4. Collection will appear in sidebar

### **Configure Variables**

The collection uses variables that auto-update:
- `baseUrl` = `http://localhost:5000` (default)
- `jwtToken` = Auto-saved from login
- `userId`, `prescriptionId`, `medicationId` = Auto-saved from responses
- `voiceRecordingId`, `reminderId` = Auto-saved from responses

---

## ?? **API Categories**

### **1. Authentication & User Management**
- Register New User
- Verify OTP
- Login (Send OTP)
- Get Current User Profile

### **2. Prescription Management**
- Upload Prescription (Base64)
- Get All Prescriptions
- Get Prescription by ID
- Get Prescription Details

### **3. Validation Workflow**
- Get Validation Workflow
- Confirm Medication
- Correct Medication
- Complete Validation

### **4. Voice Recordings**
- Upload Voice Recording (Base64)
- Get All Voice Recordings
- Get Voice Recording by ID
- Play Voice Recording

### **5. Reminders**
- Create Multiple Reminders (Bulk)
- Get All Reminders
- Calculate Suggested Times
- Toggle Reminder
- Delete Reminder

### **6. Health & Diagnostics**
- Health Check
- API Version

---

## ?? **Complete Workflow Test**

Follow this sequence for full end-to-end testing:

### **Step 1: Register & Login**
```bash
# 1. Register User
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phoneNumber": "9876543210"
  }'

# 2. Check email for OTP (or use 123456 in dev mode)

# 3. Verify OTP
curl -X POST http://localhost:5000/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "9876543210",
    "otp": "123456"
  }'
# Save the JWT token from response
```

### **Step 2: Upload Prescription**
```bash
# Upload prescription (use actual base64 image)
curl -X POST http://localhost:5000/api/prescriptions/upload-base64 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "imageBase64": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
    "fileName": "prescription.jpg",
    "processImmediately": true
  }'
# Note the prescriptionId from response
```

### **Step 3: Review & Validate**
```bash
# Get validation workflow
curl -X GET http://localhost:5000/api/validation/workflow/1 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Confirm each medication
curl -X POST http://localhost:5000/api/validation/medication/1/confirm \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"isConfirmed": true}'

# Complete validation
curl -X POST http://localhost:5000/api/validation/workflow/1/complete \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "prescriptionId": 1,
    "consultedPharmacist": true,
    "pharmacistNotes": "All verified"
  }'
```

### **Step 4: Record Voice**
```bash
# Upload voice recording
curl -X POST http://localhost:5000/api/voice-recordings/upload-base64 \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "audioBase64": "data:audio/webm;base64,GkXfo59C...",
    "name": "Mom'\''s Voice",
    "fileName": "voice.webm",
    "durationSeconds": 8
  }'
# Note the voiceRecordingId
```

### **Step 5: Create Reminders**
```bash
# Create reminders with voice
curl -X POST http://localhost:5000/api/reminders/bulk \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "medicationId": 1,
    "voiceRecordingId": 1,
    "reminderTimes": ["08:00:00", "20:00:00"]
  }'
```

### **Step 6: View Reminders**
```bash
# Get all reminders
curl -X GET http://localhost:5000/api/reminders \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## ?? **Using cURL Collection**

### **Test Single Endpoint**
```bash
# Copy-paste from medremind-complete-api-collection.curl
# Replace {{JWT_TOKEN}}, {{PRESCRIPTION_ID}}, etc. with actual values
```

### **Run Multiple Commands**
```bash
# Save JWT token as variable
export JWT_TOKEN="eyJhbGc..."
export PRESCRIPTION_ID="1"

# Use in commands
curl -X GET http://localhost:5000/api/prescriptions/$PRESCRIPTION_ID \
  -H "Authorization: Bearer $JWT_TOKEN"
```

---

## ?? **Postman Features**

### **Auto-Variable Capture**
- Token auto-saved after login
- IDs auto-saved from responses
- No manual copy-paste needed

### **Test Scripts Included**
```javascript
// Example: Auto-save token after login
if (pm.response.code === 200) {
    var jsonData = pm.response.json();
    if (jsonData.token) {
        pm.collectionVariables.set('jwtToken', jsonData.token);
    }
}
```

### **Run Collection**
1. Click collection name
2. Click **Run** button
3. Select requests to run
4. Click **Run MedRemind Complete API**

---

## ?? **Testing Scenarios**

### **Scenario 1: New User Journey**
1. Register ? Verify OTP ? Upload Prescription
2. Review Medications ? Confirm All
3. Record Voice ? Create Reminders

### **Scenario 2: Multiple Medications**
1. Upload prescription with 3 medications
2. Correct 1 medication name
3. Confirm all 3 medications
4. Create different voices for each
5. Setup reminders with custom times

### **Scenario 3: Voice Management**
1. Upload 3 different voices
2. Assign to different medications
3. Preview/play each voice
4. Delete unused voice

### **Scenario 4: Reminder Management**
1. Create bulk reminders (morning/evening)
2. Toggle specific reminders off
3. Calculate suggested times
4. Update reminder times

---

## ?? **Common Issues**

### **401 Unauthorized**
**Solution**: Ensure JWT token is valid and in format `Bearer TOKEN`

### **404 Not Found**
**Solution**: Check if resource ID exists (use GET list endpoints first)

### **400 Bad Request**
**Solution**: Validate JSON body matches expected format

### **500 Internal Server Error**
**Solution**: Check server logs, database connection

---

## ?? **Response Examples**

### **Success Response**
```json
{
  "success": true,
  "data": {...},
  "message": "Operation successful"
}
```

### **Error Response**
```json
{
  "success": false,
  "message": "Error description",
  "errors": ["Detail 1", "Detail 2"]
}
```

### **Authentication Response**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    "phoneNumber": "9876543210"
  }
}
```

---

## ?? **Security Notes**

1. **JWT Tokens**: Expire after configured time (default: 7 days)
2. **OTP**: Valid for 5 minutes
3. **User Isolation**: APIs only return user's own data
4. **File Access**: Voice/image files require authentication

---

## ?? **Additional Resources**

- **Full API Documentation**: `backend/MedRemind.API/Docs/README.md`
- **Swagger UI**: `http://localhost:5000/swagger` (when server running)
- **Testing Guides**:
  - `TESTING_VOICE_RECORDING.md`
  - `TESTING_VOICE_REMINDER_FLOW.md`

---

## ? **Quick Verification**

Test that backend is running:
```bash
# Health check
curl http://localhost:5000/health

# Should return: {"status":"Healthy"}
```

---

## ?? **Next Steps**

1. ? Import Postman collection
2. ? Start backend: `cd backend/MedRemind.API && dotnet run`
3. ? Run "Register New User" request
4. ? Verify OTP and save token
5. ? Test other endpoints sequentially
6. ? Follow complete workflow

---

*Last Updated: 2024-02-09*
*Collection Version: 1.0.0*
*Backend API: http://localhost:5000*
