# API Reference Quick Guide

Quick reference for all MedRemind API endpoints.

---

## ?? Base URL

```
http://localhost:5124/api
```

---

## ?? Authentication

All endpoints except `/api/Auth/*` require Bearer token authentication.

**Header:**
```
Authorization: Bearer {your-token}
```

---

## ?? Endpoints Overview

### ?? Authentication (`/api/Auth`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/send-otp` | Send OTP to phone number | ? |
| POST | `/verify-otp` | Verify OTP and get token | ? |
| POST | `/validate-token` | Validate session token | ? |

---

### ?? Medications (`/api/Medications`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get all medications | ? |
| GET | `/{id}` | Get medication by ID | ? |
| POST | `/` | Create new medication | ? |
| PUT | `/{id}` | Update medication | ? |
| DELETE | `/{id}` | Delete medication | ? |

**Query Parameters:**
- `userId` - Filter by user ID
- `isActive` - Filter by active status

---

### ?? Prescriptions (`/api/Prescriptions`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get all prescriptions | ? |
| GET | `/{id}` | Get prescription by ID | ? |
| POST | `/upload` | Upload prescription image | ? |
| DELETE | `/{id}` | Delete prescription | ? |

**Upload Format:**
- Content-Type: `multipart/form-data`
- File parameter: `file`
- Additional: `userId`

**Supported Formats:** JPG, PNG, PDF

---

### ? Reminders (`/api/Reminders`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/` | Get all reminders | ? |
| GET | `/{id}` | Get reminder by ID | ? |
| POST | `/` | Create new reminder | ? |
| PUT | `/{id}` | Update reminder | ? |
| DELETE | `/{id}` | Delete reminder | ? |

**Query Parameters:**
- `userId` - Filter by user ID
- `medicationId` - Filter by medication
- `isEnabled` - Filter by enabled status

---

### ?? Adherence (`/api/Adherence`)

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/stats` | Get adherence statistics | ? |
| POST | `/log` | Log medication taken | ? |
| GET | `/history` | Get adherence history | ? |

**Query Parameters for Stats:**
- `userId` - User ID (required)
- `startDate` - Start date (optional)
- `endDate` - End date (optional)

---

## ?? Request/Response Examples

### Authentication

#### Send OTP
```json
POST /api/Auth/send-otp

Request:
{
  "phoneNumber": "+919876543210"
}

Response (200):
{
  "message": "OTP sent successfully",
  "phoneNumber": "+919876543210"
}
```

#### Verify OTP
```json
POST /api/Auth/verify-otp

Request:
{
  "phoneNumber": "+919876543210",
  "otp": "123456"
}

Response (200):
{
  "message": "Login successful",
  "phoneNumber": "+919876543210",
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

---

### Medications

#### Create Medication
```json
POST /api/Medications
Authorization: Bearer {token}

Request:
{
  "name": "Aspirin",
  "dosage": "500mg",
  "frequency": "Twice daily",
  "instructions": "Take with food",
  "startDate": "2025-01-01T00:00:00",
  "endDate": "2025-01-31T23:59:59",
  "userId": 1
}

Response (201):
{
  "id": 1,
  "name": "Aspirin",
  "dosage": "500mg",
  "frequency": "Twice daily",
  "instructions": "Take with food",
  "startDate": "2025-01-01T00:00:00",
  "endDate": "2025-01-31T23:59:59",
  "userId": 1,
  "isActive": true,
  "createdAt": "2025-01-29T20:00:00"
}
```

#### Get All Medications
```json
GET /api/Medications?userId=1&isActive=true
Authorization: Bearer {token}

Response (200):
[
  {
    "id": 1,
    "name": "Aspirin",
    "dosage": "500mg",
    "frequency": "Twice daily",
    "instructions": "Take with food",
    "startDate": "2025-01-01T00:00:00",
    "endDate": "2025-01-31T23:59:59",
    "userId": 1,
    "isActive": true,
    "createdAt": "2025-01-29T20:00:00"
  }
]
```

---

### Prescriptions

#### Upload Prescription
```http
POST /api/Prescriptions/upload
Authorization: Bearer {token}
Content-Type: multipart/form-data

Form Data:
- file: [prescription.jpg]
- userId: 1

Response (200):
{
  "id": 1,
  "userId": 1,
  "fileName": "prescription.jpg",
  "uploadedAt": "2025-01-29T20:00:00",
  "status": "Processing",
  "medications": []
}
```

---

### Reminders

#### Create Reminder
```json
POST /api/Reminders
Authorization: Bearer {token}

Request:
{
  "medicationId": 1,
  "userId": 1,
  "time": "09:00:00",
  "days": ["Monday", "Wednesday", "Friday"],
  "enabled": true
}

Response (201):
{
  "id": 1,
  "medicationId": 1,
  "userId": 1,
  "time": "09:00:00",
  "days": ["Monday", "Wednesday", "Friday"],
  "enabled": true,
  "createdAt": "2025-01-29T20:00:00"
}
```

---

### Adherence

#### Get Statistics
```json
GET /api/Adherence/stats?userId=1
Authorization: Bearer {token}

Response (200):
{
  "userId": 1,
  "totalDoses": 30,
  "takenDoses": 27,
  "missedDoses": 3,
  "adherenceRate": 90.0,
  "period": {
    "startDate": "2025-01-01",
    "endDate": "2025-01-31"
  }
}
```

#### Log Medication Taken
```json
POST /api/Adherence/log
Authorization: Bearer {token}

Request:
{
  "userId": 1,
  "medicationId": 1,
  "takenAt": "2025-01-29T09:15:00",
  "status": "taken"
}

Response (200):
{
  "id": 1,
  "userId": 1,
  "medicationId": 1,
  "takenAt": "2025-01-29T09:15:00",
  "status": "taken",
  "loggedAt": "2025-01-29T09:15:30"
}
```

---

## ?? HTTP Status Codes

| Code | Meaning | When Used |
|------|---------|-----------|
| 200 | OK | Successful GET, PUT, DELETE |
| 201 | Created | Successful POST (resource created) |
| 400 | Bad Request | Invalid input data |
| 401 | Unauthorized | Missing or invalid token |
| 404 | Not Found | Resource doesn't exist |
| 500 | Internal Server Error | Server error |

---

## ?? Common Request Headers

```http
Content-Type: application/json
Authorization: Bearer {token}
Accept: application/json
```

---

## ?? Date/Time Format

All dates use ISO 8601 format:
```
2025-01-29T09:15:00
```

---

## ?? Related Documentation

- **[Complete Setup Guide](SWAGGER_SETUP.md)**
- **[cURL Examples](CURL_EXAMPLES.md)**
- **[Documentation Index](INDEX.md)**

---

## ?? Tips

1. **Test with Swagger UI first** before writing code
2. **Always include Authorization header** for protected endpoints
3. **Check response status codes** for error handling
4. **Use query parameters** to filter results
5. **Validate dates** before sending requests

---

**Need more details?** Check Swagger UI at: `http://localhost:5124/swagger`
