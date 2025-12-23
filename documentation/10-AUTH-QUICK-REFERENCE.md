# ? Authentication System - Quick Reference

## ?? Quick Overview

Med Remind authentication:
- **Method**: Mobile number + OTP
- **Provider**: 2Factor API
- **Session**: 30 days
- **Biometric**: Framework ready (needs platform implementation)

---

## ?? User Flows

### **First Time User**
```
1. Enter phone (10 digits)
2. Tap "Send OTP"
3. Enter 6-digit OTP
4. Tap "Verify OTP"
5. Enable biometric (optional)
6. Done! ?
```

**Time**: 30 seconds

### **Returning User (Auto-login)**
```
1. Open app
2. Done! ?
```

**Time**: Instant

### **Returning User (Biometric)**
```
1. Open app
2. Tap "Login with Biometric"
3. Authenticate
4. Done! ?
```

**Time**: 5 seconds

---

## ?? Key Features

? **OTP-based authentication**  
? **Auto user registration**  
? **Session management (30 days)**  
? **Auto-login on startup**  
? **Biometric UI ready**  
? **Secure token storage**  

---

## ?? UI Components

### **Login Page**
- Phone number input (10 digits)
- OTP input (6 digits)
- Send OTP / Verify button
- Biometric login button (when available)
- Resend OTP link

### **Biometric Prompt**
- "Enable Biometric Login?" dialog
- Shown after first successful OTP login
- Optional (can skip)

---

## ?? Security

### **SecureStorage Keys**
```csharp
"user_id"          // User ID
"session_token"    // Session token
"phone_number"     // For biometric login
"biometric_enabled" // Biometric status
```

### **Token Format**
```
Base64( userId:timestamp:randomToken )
Expiry: 30 days
```

---

## ?? Testing

### **Quick Test**
```
1. Uninstall app
2. Install app
3. Open app ? Should show Login
4. Enter phone: 9876543210
5. Send OTP
6. Enter OTP from SMS
7. Verify ? Should navigate to Home
8. Close app
9. Open app ? Should auto-login to Home
```

### **Expected Results**
? Login page on first launch  
? OTP sent and received  
? User created in database  
? Home page after verification  
? Auto-login on subsequent launches  

---

## ?? Configuration

### **2Factor API**
```json
{
  "TwoFactor": {
    "ApiKey": "YOUR-API-KEY",
    "TimeoutSeconds": 10
  }
}
```

**Get API Key**: https://2factor.in

---

## ?? Quick Fixes

### **OTP not received?**
? Check 2Factor API key and credits

### **Login page every time?**
? Check if user_id and session_token stored

### **Biometric not showing?**
? Login with OTP first, then enable biometric

---

## ?? Status

| Component | Status |
|-----------|--------|
| **OTP Login** | ? Working |
| **Auto-Login** | ? Working |
| **Session** | ? Working |
| **Biometric UI** | ? Working |
| **Biometric Auth** | ?? Stub (needs implementation) |

---

## ?? Implementation Notes

### **Biometric**
Current implementation is a **stub**. Full biometric requires:
1. AndroidX.Biometric package (for Android)
2. Platform-specific code
3. Real device testing

### **Auto-Login**
App checks on startup:
```csharp
user_id + session_token exist ? Home
else ? Login
```

---

**Time to implement**: ? Complete  
**Time to test**: 5 minutes  
**Production ready**: ? Yes (OTP login)  
**Biometric ready**: ?? Framework only  

**Authentication system is ready for production use! ??**
