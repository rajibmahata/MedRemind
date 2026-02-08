# ?? MedRemind - Run Both UI & APIs

## ? Quick Start (Choose One Method)

### Method 1: Double-Click Script (Easiest)
```bash
# Windows
run-medremind.bat

# PowerShell
.\run-medremind.ps1
```

### Method 2: VS Code Tasks
1. Press `Ctrl+Shift+B` (Run Build Task)
2. Select "Run Both (Backend + Frontend)"
3. Both services start automatically

### Method 3: Manual (Separate Terminals)

#### Terminal 1 - Backend API
```bash
cd backend\MedRemind.API
dotnet run --urls=http://localhost:5000
```

#### Terminal 2 - Frontend UI
```bash
cd web\MedRemind.Web
dotnet watch run
```

---

## ?? Access URLs

| Service | URL | Purpose |
|---------|-----|---------|
| **Backend API** | http://localhost:5000 | REST API endpoints |
| **Frontend UI** | http://localhost:5001 | Blazor WebAssembly app |
| **Swagger** | http://localhost:5000/swagger | API documentation |

---

## ? Verification Steps

### 1. Check Backend API
```bash
# Test health endpoint
curl http://localhost:5000/health

# Or open in browser
http://localhost:5000/swagger
```

### 2. Check Frontend UI
```bash
# Open in browser
http://localhost:5001
```

### 3. Test Communication
1. Open UI: http://localhost:5001
2. Click "Login"
3. Enter phone number
4. Should send OTP (API communication working)

---

## ?? Configuration

### Backend API Configuration
**File:** `backend/MedRemind.API/appsettings.Development.json`
```json
{
  "Urls": "http://localhost:5000",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MedRemind;..."
  }
}
```

### Frontend UI Configuration
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

## ?? Troubleshooting

### Issue: Port Already in Use
```bash
# Find process using port 5000
netstat -ano | findstr :5000

# Kill process (replace PID)
taskkill /PID <PID> /F
```

### Issue: CORS Error
**Solution:** Backend should already have CORS configured for localhost:5001

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

### Issue: Database Not Found
```bash
# Run database migrations
cd backend/MedRemind.API
dotnet ef database update
```

### Issue: API Returns 500 Error
1. Check API logs in terminal
2. Verify database connection
3. Check appsettings.Development.json

### Issue: UI Cannot Connect to API
1. Check API is running: http://localhost:5000/swagger
2. Check UI API settings: `web/MedRemind.Web/wwwroot/appsettings.json`
3. Verify CORS policy in backend

---

## ?? Development Workflow

### 1. Start Development Session
```bash
# Run both services
.\run-medremind.ps1
```

### 2. Make Changes
- Backend: Auto-reloads on save
- Frontend: `dotnet watch` auto-reloads

### 3. Test Changes
- API: http://localhost:5000/swagger
- UI: http://localhost:5001
- Check browser console for errors

### 4. Stop Services
- Press `Ctrl+C` in PowerShell window
- Or close terminal windows

---

## ?? Testing Checklist

### Backend API ?
- [ ] Swagger loads: http://localhost:5000/swagger
- [ ] Health endpoint: http://localhost:5000/health
- [ ] Auth endpoints work
- [ ] Database connected
- [ ] Logs show no errors

### Frontend UI ?
- [ ] Home page loads: http://localhost:5001
- [ ] Medical disclaimers visible
- [ ] Login page accessible
- [ ] No console errors
- [ ] API calls work

### Integration ?
- [ ] Login flow works (UI ? API)
- [ ] OTP sent successfully
- [ ] Upload prescription works
- [ ] Medications list loads
- [ ] No CORS errors

---

## ?? Project Structure

```
MedRemind/
??? backend/
?   ??? MedRemind.API/          ? Backend (Port 5000)
?       ??? Controllers/
?       ??? Program.cs
?       ??? appsettings.json
?
??? web/
?   ??? MedRemind.Web/          ? Frontend (Port 5001)
?       ??? Pages/
?       ??? Services/
?       ??? Program.cs
?       ??? wwwroot/
?           ??? appsettings.json
?
??? run-medremind.bat           ? Windows script
??? run-medremind.ps1           ? PowerShell script
??? .vscode/
    ??? tasks.json              ? VS Code tasks
```

---

## ?? Environment Variables (Optional)

### Set API Key (Backend)
```bash
# Windows
set OPENAI_API_KEY=your-key-here
set TWOFACTOR_API_KEY=your-key-here

# PowerShell
$env:OPENAI_API_KEY="your-key-here"
$env:TWOFACTOR_API_KEY="your-key-here"
```

### Set API URL (Frontend)
**File:** `web/MedRemind.Web/wwwroot/appsettings.json`
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

---

## ?? Performance Tips

### 1. Use `dotnet watch` (Frontend)
- Auto-reloads on file changes
- Faster development cycle

### 2. Use Swagger for API Testing
- Interactive API documentation
- Test endpoints without UI

### 3. Browser DevTools
- F12 ? Network tab
- Check API requests/responses
- View console errors

---

## ?? Success!

When both services are running, you should see:

### Backend Terminal
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Frontend Terminal
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Browser
- Open http://localhost:5001
- See MedRemind landing page
- Medical disclaimers visible
- Can click "Get Started" ? Login

---

## ?? Quick Commands

```bash
# Start both services
.\run-medremind.ps1

# Build backend
dotnet build backend/MedRemind.API/MedRemind.API.csproj

# Build frontend
dotnet build web/MedRemind.Web/MedRemind.Web.csproj

# Run tests
dotnet test backend/MedRemind.Tests/MedRemind.Tests.csproj

# Database migrations
cd backend/MedRemind.API
dotnet ef database update
```

---

## ?? Next Steps

1. ? Run both services
2. ? Test login flow
3. ? Upload prescription
4. ? View medications
5. ? Check all disclaimers
6. ?? Deploy!

---

**Status:** Ready to Run! ??  
**Services:** Backend API + Blazor UI  
**Ports:** 5000 (API) + 5001 (UI)  
**Documentation:** Complete  

Happy Coding! ??
