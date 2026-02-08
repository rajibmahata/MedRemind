# Postman Collection - Ready to Import

## ?? Quick Start

### Import in 3 Steps:
1. Open Postman
2. Import `MedRemind_Complete_Collection_v2.json`
3. Import `MedRemind_Local_Environment.json`
4. Start testing!

---

## ?? Files

| File | Description | Size |
|------|-------------|------|
| `MedRemind_Complete_Collection_v2.json` | Complete API collection (31+ endpoints) | Main |
| `MedRemind_Local_Environment.json` | Environment variables | Small |
| `POSTMAN_IMPORT_GUIDE.md` | Complete import and usage guide | Guide |

---

## ?? What's Included

### 1. Authentication (9 endpoints)
- Send OTP
- Verify OTP  
- **Resend OTP** ? NEW
- **Check Resend Availability** ? NEW
- Login with Password
- Forgot/Reset/Change Password
- Validate Token

### 2. User Management (5 endpoints)
- Register, Get, Update, Delete User
- Profile management

### 3. Prescriptions (8 endpoints)
- Upload, Get, Update, Delete
- Reprocess, OCR Results

### 4. Medications (9 endpoints)
- CRUD operations
- Get active, Mark as taken

### 5. Test Flows (1 complete flow)
- Complete registration flow (4 steps)

---

## ?? Quick Test

```
1. Start API: dotnet run --project backend/MedRemind.API
2. Import collection & environment in Postman
3. Select "MedRemind - Local" environment
4. Run "1.1 Send OTP"
5. Done! ?
```

---

## ?? Full Guide

See `POSTMAN_IMPORT_GUIDE.md` for:
- Detailed import instructions
- All test scenarios
- Troubleshooting
- Tips & tricks

---

## ? Features

- ? 31+ endpoints organized in 5 folders
- ? Automated tests on all requests
- ? Auto-save tokens and IDs
- ? Environment variables included
- ? Runner-friendly structure
- ? Pre-request scripts
- ? Complete test flows

---

## ?? Related

- **cURL Examples:** `documentation/cURLs/complete-api-collection.curl`
- **API Docs:** `backend/MedRemind.API/Docs/`
- **Resend OTP:** `RESEND_OTP_START_HERE.md`

---

**Version:** 2.0  
**Status:** ? Ready to Import  
**Updated:** 2024-02-04
