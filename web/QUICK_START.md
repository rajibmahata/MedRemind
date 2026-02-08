# ?? MedRemind Web - Quick Start Guide

## ? BUILD FIXED - READY TO RUN!

All build issues resolved. Zero warnings. Ready for development!

---

## ?? Quick Commands

### Run Application
```bash
cd F:\rajibmahata\MedRemind\web\MedRemind.Web
dotnet watch run
```
**Access:** http://localhost:5000 or https://localhost:5001

### Build Project
```bash
dotnet build
```

### Build Release
```bash
dotnet build --configuration Release
```

---

## ?? What's Working

? **All Services Implemented**
- AuthService (Login, OTP, Logout)
- PrescriptionService (Upload, Get)
- MedicationService (CRUD)
- LocalStorageService (Token storage)

? **API Configuration**
- Development: http://localhost:5000
- Production: https://api.medremind.com
- Configurable via appsettings.json

? **Build Status**
- Debug: ? Successful
- Release: ? Successful
- Warnings: 0
- Errors: 0

---

## ?? Configuration Files

### `wwwroot/appsettings.json`
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000",
    "Timeout": 30
  }
}
```

**Change API URL here** to point to your backend!

---

## ?? Project Structure

```
web/MedRemind.Web/
??? Services/         ? All implemented
?   ??? AuthService.cs
?   ??? PrescriptionService.cs
?   ??? MedicationService.cs
?   ??? LocalStorageService.cs
?
??? Models/
?   ??? AppSettings.cs
?
??? wwwroot/
?   ??? appsettings.json           ?? Configure here
?   ??? appsettings.Production.json
?
??? Program.cs        ? All services registered
```

---

## ?? Authentication Example

```csharp
@inject IAuthService AuthService

// Send OTP
var sent = await AuthService.SendOtpAsync("8420249020");

// Verify OTP
var (success, token, error) = await AuthService.VerifyOtpAsync("8420249020", "123456");

// Check if authenticated
var isAuth = AuthService.IsAuthenticated();

// Logout
await AuthService.LogoutAsync();
```

---

## ?? API Integration Example

```csharp
@inject IPrescriptionService PrescriptionService

// Upload prescription
var stream = file.OpenReadStream();
var result = await PrescriptionService.UploadPrescriptionAsync(stream, "prescription.jpg");

// Get all prescriptions
var prescriptions = await PrescriptionService.GetAllAsync();

// Get by ID
var prescription = await PrescriptionService.GetByIdAsync(1);
```

---

## ?? Next Steps

### 1. Update Imports (2 minutes)
Edit `_Imports.razor`:
```razor
@using MudBlazor
@using MedRemind.Web.Services
```

### 2. Add MudBlazor to HTML (3 minutes)
Edit `wwwroot/index.html`:
```html
<!-- In <head> -->
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />

<!-- Before </body> -->
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

### 3. Create DisclaimerBanner (5 minutes)
Create `Shared/DisclaimerBanner.razor`:
```razor
<MudAlert Severity="Severity.Warning">
    ?? MEDICAL DISCLAIMER: For reading help only, NOT medical advice.
</MudAlert>
```

### 4. Create Pages (2-3 hours)
- Login.razor
- Dashboard.razor
- UploadPrescription.razor
- Medications.razor

Full examples in: `IMPLEMENTATION_READY.md`

---

## ?? Common Issues

### "Cannot connect to API"
?? Check backend is running: `dotnet run --project backend/MedRemind.API`

### "Unauthorized"
?? Login first, token is auto-saved

### "CORS Error"
?? Backend must allow CORS from Blazor URL

---

## ?? Quick Links

- **Full Guide:** `BUILD_FIX_SUMMARY.md`
- **Implementation:** `IMPLEMENTATION_READY.md`
- **Architecture:** `MEDREMIND_WEB_GUIDE.md`
- **MudBlazor Docs:** https://mudblazor.com/

---

## ?? Status

? **Build:** Successful  
? **Services:** Complete  
? **Configuration:** Done  
? **Ready:** Yes!  

**You can now start building UI pages!** ??

---

**Last Updated:** 2025-02-07  
**Status:** Ready for Development  
**Next:** Create UI Pages
