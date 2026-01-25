# MedRemind API - cURL Examples

## Test the API with these cURL commands

### Base URL
```bash
BASE_URL=https://localhost:7073
# or
BASE_URL=http://localhost:5000
```

---

## ?? Authentication

### 1. Send OTP
```bash
curl -X POST "${BASE_URL}/api/Auth/send-otp" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+919876543210"
  }'
```

**Response:**
```json
{
  "message": "OTP sent successfully",
  "phoneNumber": "+919876543210"
}
```

### 2. Verify OTP and Login
```bash
curl -X POST "${BASE_URL}/api/Auth/verify-otp" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+919876543210",
    "otp": "123456"
  }'
```

**Response:**
```json
{
  "message": "Login successful",
  "phoneNumber": "+919876543210",
  "token": "your-session-token-here"
}
```

### 3. Validate Token
```bash
curl -X POST "${BASE_URL}/api/Auth/validate-token" \
  -H "Content-Type: application/json" \
  -d '{
    "token": "your-session-token-here"
  }'
```

---

## ?? Medications

**Note:** Replace `{TOKEN}` with your actual authentication token from step 2.

### Get All Medications
```bash
curl -X GET "${BASE_URL}/api/Medications" \
  -H "Authorization: Bearer {TOKEN}"
```

### Get Medication by ID
```bash
curl -X GET "${BASE_URL}/api/Medications/1" \
  -H "Authorization: Bearer {TOKEN}"
```

### Create Medication
```bash
curl -X POST "${BASE_URL}/api/Medications" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Aspirin",
    "dosage": "500mg",
    "frequency": "Twice daily",
    "instructions": "Take with food",
    "startDate": "2025-01-01T00:00:00",
    "endDate": "2025-01-31T23:59:59"
  }'
```

### Update Medication
```bash
curl -X PUT "${BASE_URL}/api/Medications/1" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "name": "Aspirin",
    "dosage": "500mg",
    "frequency": "Once daily",
    "instructions": "Take after dinner",
    "startDate": "2025-01-01T00:00:00",
    "endDate": "2025-02-28T23:59:59"
  }'
```

### Delete Medication
```bash
curl -X DELETE "${BASE_URL}/api/Medications/1" \
  -H "Authorization: Bearer {TOKEN}"
```

---

## ?? Prescriptions

### Get All Prescriptions
```bash
curl -X GET "${BASE_URL}/api/Prescriptions" \
  -H "Authorization: Bearer {TOKEN}"
```

### Get Prescription by ID
```bash
curl -X GET "${BASE_URL}/api/Prescriptions/1" \
  -H "Authorization: Bearer {TOKEN}"
```

### Upload Prescription Image
```bash
curl -X POST "${BASE_URL}/api/Prescriptions/upload" \
  -H "Authorization: Bearer {TOKEN}" \
  -F "file=@/path/to/prescription.jpg" \
  -F "userId=1"
```

**Note:** Replace `/path/to/prescription.jpg` with actual file path.

### Delete Prescription
```bash
curl -X DELETE "${BASE_URL}/api/Prescriptions/1" \
  -H "Authorization: Bearer {TOKEN}"
```

---

## ? Reminders

### Get All Reminders
```bash
curl -X GET "${BASE_URL}/api/Reminders" \
  -H "Authorization: Bearer {TOKEN}"
```

### Get Reminder by ID
```bash
curl -X GET "${BASE_URL}/api/Reminders/1" \
  -H "Authorization: Bearer {TOKEN}"
```

### Create Reminder
```bash
curl -X POST "${BASE_URL}/api/Reminders" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "medicationId": 1,
    "userId": 1,
    "time": "09:00:00",
    "days": ["Monday", "Wednesday", "Friday"],
    "enabled": true
  }'
```

### Update Reminder
```bash
curl -X PUT "${BASE_URL}/api/Reminders/1" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "medicationId": 1,
    "userId": 1,
    "time": "08:00:00",
    "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
    "enabled": true
  }'
```

### Delete Reminder
```bash
curl -X DELETE "${BASE_URL}/api/Reminders/1" \
  -H "Authorization: Bearer {TOKEN}"
```

---

## ?? Adherence

### Get Adherence Statistics
```bash
curl -X GET "${BASE_URL}/api/Adherence/stats?userId=1" \
  -H "Authorization: Bearer {TOKEN}"
```

### Log Medication Taken
```bash
curl -X POST "${BASE_URL}/api/Adherence/log" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "medicationId": 1,
    "takenAt": "2025-01-29T09:15:00",
    "status": "taken"
  }'
```

### Get Adherence History
```bash
curl -X GET "${BASE_URL}/api/Adherence/history?userId=1&startDate=2025-01-01&endDate=2025-01-31" \
  -H "Authorization: Bearer {TOKEN}"
```

---

## ?? Testing Tips

### For Windows PowerShell:
Replace `\` with `` ` `` (backtick) for line continuation:

```powershell
curl -X POST "${BASE_URL}/api/Auth/send-otp" `
  -H "Content-Type: application/json" `
  -d '{
    "phoneNumber": "+919876543210"
  }'
```

### Save Token to Variable:
```bash
# Linux/Mac
TOKEN=$(curl -X POST "${BASE_URL}/api/Auth/verify-otp" \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "+919876543210", "otp": "123456"}' \
  | jq -r '.token')

# Then use: -H "Authorization: Bearer ${TOKEN}"
```

### Pretty Print JSON Response:
```bash
# Linux/Mac
curl ... | jq .

# Windows (install jq or use Python)
curl ... | python -m json.tool
```

---

## ?? Notes

1. **HTTPS Certificate Warning**: When testing locally with HTTPS, you may need to use `-k` or `--insecure` flag:
   ```bash
   curl -k -X GET "https://localhost:7073/api/Medications"
   ```

2. **Date Format**: Use ISO 8601 format for dates: `2025-01-29T09:15:00`

3. **File Upload**: For prescription upload, ensure the image is a valid JPG/PNG file

4. **Token Expiration**: Tokens may expire based on JWT configuration (default: 30 days)

5. **Phone Number Format**: Use international format with country code: `+919876543210`

---

## ?? Useful Links

- **Swagger UI**: `${BASE_URL}/swagger`
- **OpenAPI Spec**: `${BASE_URL}/swagger/v1/swagger.json`
- **Health Check**: `${BASE_URL}/health` (if implemented)

---

**Happy Testing! ??**

For more details, visit Swagger UI at `https://localhost:7073/swagger`
