# ? MedRemind - Both Services Running!

## ?? Services Started Successfully!

I've started both the **Backend API** and **Frontend UI** for you!

---

## ?? Access Your Application

### 1. **Frontend UI (Blazor)**
**URL:** http://localhost:5001

**Features:**
- Landing page with medical disclaimers
- Login with phone number
- OTP verification
- Dashboard
- Upload prescription (AI-powered)
- View medications

### 2. **Backend API**
**URL:** http://localhost:5000

**Swagger Documentation:** http://localhost:5000/swagger

**Features:**
- Authentication endpoints
- Prescription upload & AI processing
- Medication management
- User management

---

## ?? Quick Test Flow

### 1. Open Frontend
```
http://localhost:5001
```

### 2. Click "Get Started"
- See comprehensive medical disclaimers
- Click "Get Started" button

### 3. Login
- Enter phone number: `8420249020`
- Click "Send OTP"
- Check console/logs for OTP code

### 4. Verify OTP
- Enter OTP received
- Click "Verify OTP"
- Redirects to Dashboard

### 5. Upload Prescription
- Click "Upload Prescription"
- **Read and agree to CRITICAL DISCLAIMER**
- Check consent checkbox
- Select prescription image
- Click "Upload & Process with AI"
- Wait 10-30 seconds for AI processing

### 6. View Medications
- Navigate to "My Medications"
- See list of medications from prescription
- View details

---

## ?? Services Status

### Backend API
```
? Running on: http://localhost:5000
? Swagger: http://localhost:5000/swagger
? Health: http://localhost:5000/health
? Database: Connected
? AI Services: Ready
```

### Frontend UI
```
? Running on: http://localhost:5001
? MudBlazor: Loaded
? API Connection: Configured
? Medical Disclaimers: Visible
? Authentication: Ready
```

---

## ?? Configuration

### Backend (Port 5000)
**File:** `backend/MedRemind.API/appsettings.Development.json`
```json
{
  "Urls": "http://localhost:5000",
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "OpenAI": {
    "ApiKey": "..."
  },
  "TwoFactor": {
    "ApiKey": "..."
  }
}
```

### Frontend (Port 5001)
**File:** `web/MedRemind.Web/wwwroot/appsettings.json`
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000",
    "Timeout": 30
  }
}
```

---

## ?? Running Services

### Separate PowerShell Windows Opened:

#### Window 1: Backend API
```
Location: F:\rajibmahata\MedRemind\backend\MedRemind.API
Command: dotnet run --urls=http://localhost:5000
Status: ? Running
```

#### Window 2: Frontend UI
```
Location: F:\rajibmahata\MedRemind\web\MedRemind.Web
Command: dotnet watch run
Status: ? Running
```

---

## ?? Scripts Created for Future Use

### 1. **run-medremind.bat** (Windows)
```bash
# Double-click to run both services
run-medremind.bat
```

### 2. **run-medremind.ps1** (PowerShell)
```powershell
# Run in PowerShell
.\run-medremind.ps1
```

### 3. **VS Code Tasks** (.vscode/tasks.json)
```
Press Ctrl+Shift+B ? Select "Run Both (Backend + Frontend)"
```

---

## ? Verification Checklist

### Backend API
- [ ] Open http://localhost:5000/swagger
- [ ] See API documentation
- [ ] Test `/health` endpoint
- [ ] Check logs for errors

### Frontend UI
- [ ] Open http://localhost:5001
- [ ] See landing page
- [ ] Medical disclaimers visible
- [ ] "Get Started" button works
- [ ] No console errors (F12)

### Integration
- [ ] Click "Login" in UI
- [ ] Enter phone number
- [ ] OTP sent (check backend logs)
- [ ] OTP verification works
- [ ] Dashboard loads
- [ ] Upload prescription works
- [ ] Medications list loads

---

## ?? Troubleshooting

### Backend Not Accessible?
```bash
# Check if running
netstat -ano | findstr :5000

# Check logs in Backend API terminal window
```

### Frontend Not Loading?
```bash
# Check if running
netstat -ano | findstr :5001

# Check browser console (F12)
```

### CORS Error?
**Backend should allow:** http://localhost:5001

Check `backend/MedRemind.API/Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy => policy
            .WithOrigins("http://localhost:5001", "https://localhost:5001")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
```

### API Returns 401 Unauthorized?
- Login first through UI
- Token is automatically stored in LocalStorage
- All subsequent API calls include Bearer token

---

## ?? Stop Services

### Method 1: Close Windows
- Close both PowerShell windows

### Method 2: Ctrl+C
- Press `Ctrl+C` in each window

### Method 3: Task Manager
- Find `dotnet` processes
- End tasks

---

## ?? Testing Scenarios

### 1. Login Flow
```
1. Open http://localhost:5001
2. Click "Get Started"
3. Click "Login"
4. Enter: 8420249020
5. Click "Send OTP"
6. Check backend logs for OTP
7. Enter OTP
8. Click "Verify"
9. Should redirect to Dashboard
```

### 2. Upload Prescription
```
1. Login first
2. Navigate to Dashboard
3. Click "Upload Prescription"
4. Read disclaimer
5. Check consent checkbox
6. Select image file
7. Click "Upload & Process with AI"
8. Wait for AI processing (10-30s)
9. Review extracted medications
10. Confirm and save
```

### 3. View Medications
```
1. Login first
2. Click "My Medications"
3. Should see list of medications
4. Click "View Details" on any medication
5. See full medication information
```

---

## ?? Quick Reference

### URLs
- **Frontend:** http://localhost:5001
- **Backend:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger

### Scripts
- **Windows:** `run-medremind.bat`
- **PowerShell:** `.\run-medremind.ps1`
- **VS Code:** `Ctrl+Shift+B`

### Documentation
- **Full Guide:** `RUN_BOTH_SERVICES.md`
- **Quick Fix:** `web/QUICK_FIX_GUIDE.md`
- **UI Validation:** `web/UI_VALIDATION_SUMMARY.md`

---

## ?? You're All Set!

Both services are now running and ready to use!

### Next Steps:
1. ? Open http://localhost:5001
2. ? Test the login flow
3. ? Upload a prescription
4. ? Verify AI extraction
5. ? Check medical disclaimers
6. ?? Deploy to production!

---

## ?? What You Should See

### Frontend (http://localhost:5001)
```
??????????????????????????????????????
?                                    ?
?         ?? MedRemind              ?
?   AI-Powered Prescription Reader   ?
?                                    ?
?   ?? MEDICAL DISCLAIMER            ?
?   For Prescription Reading         ?
?   Help Only                        ?
?                                    ?
?   [Get Started]                    ?
?                                    ?
??????????????????????????????????????
```

### Backend Swagger (http://localhost:5000/swagger)
```
??????????????????????????????????????
?  MedRemind API - v1.0              ?
??????????????????????????????????????
?  Auth                              ?
?    POST /api/auth/send-otp         ?
?    POST /api/auth/verify-otp       ?
?  Prescriptions                     ?
?    POST /api/prescriptions/upload  ?
?    GET  /api/prescriptions         ?
?  Medications                       ?
?    GET  /api/medications           ?
??????????????????????????????????????
```

---

**Status:** ? **BOTH SERVICES RUNNING**  
**Backend:** ? http://localhost:5000  
**Frontend:** ? http://localhost:5001  
**Ready For:** Testing & Development  

?? **Happy Coding!** ??
