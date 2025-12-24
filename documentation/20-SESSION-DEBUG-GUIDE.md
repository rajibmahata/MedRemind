# ?? Session Debug Guide - "User not logged in" Issue

## Quick Diagnosis

When you see "User not logged in" error during prescription processing, check these logs:

### **1. On App Startup**
```
Expected:
?? AppShell: Checking authentication...
? Session found for user: 1
? Session valid
? Authentication complete - navigating to Home
```

### **2. On Prescription Upload Page Load**
```
Expected:
?? PrescriptionUpload: Page loaded
   User ID: 1
   Session Token: EXISTS
```

**If you see**:
```
? User ID: NULL
?? Warning: user_id is empty in SecureStorage!
```
**Then**: Session was cleared or never set properly during login

### **3. During AI Processing**
```
Expected:
?? ProcessPrescription: Starting...
   SecureStorage check - user_id: 1, session: EXISTS
?? Processing prescription for user ID: 1
?? Saving prescription for user 1
```

**If you see**:
```
? User not logged in exception caught!
   Final check - user_id: NULL, session: NULL
```
**Then**: Session was cleared between page load and processing

---

## Common Causes & Fixes

### **Cause 1: Login Not Saving Session**

**Check**: `AuthenticationService.VerifyOtpAsync()`
```csharp
// Must store BOTH values:
await _secureStorage.SetAsync("session_token", token);
await _secureStorage.SetAsync("user_id", user.Id.ToString());  // ? Critical!
```

**Fix**: Ensure both values are stored after successful OTP verification

---

### **Cause 2: Session Cleared by Another Process**

**Check**: Look for any code calling:
```csharp
SecureStorage.RemoveAll();
SecureStorage.Remove("user_id");
```

**Fix**: Only clear session on explicit logout or expired session

---

### **Cause 3: AppShell Navigation Timing**

**Check**: Is authentication completing before navigation?
```csharp
// AppShell should complete auth BEFORE allowing navigation
if (!_isAuthChecked) {
    _isAuthChecked = true;
    await CheckAuthenticationAsync();  // Must finish before navigation
}
```

**Fix**: Ensure `_isAuthChecked` flag prevents multiple auth checks

---

## Debug Steps

### **Step 1: Check Login Flow**
1. Login with OTP
2. Check Output window for:
```
? User logged in: 1
? Session token stored
? user_id stored: 1
```

3. If not present, fix `AuthenticationService`

### **Step 2: Check App Startup**
1. Close and reopen app
2. Check Output for:
```
?? AppShell: Checking authentication...
? Session found for user: 1
```

3. If shows `? No session found`, session wasn't persisted

### **Step 3: Check Page Load**
1. Navigate to Prescription Upload
2. Check Output for:
```
?? PrescriptionUpload: Page loaded
   User ID: 1
```

3. If shows `NULL`, session cleared between startup and navigation

### **Step 4: Check Processing**
1. Select/take photo
2. Tap "Process with AI"
3. Check Output for:
```
?? ProcessPrescription: Starting...
   SecureStorage check - user_id: 1, session: EXISTS
```

4. If shows `NULL`, session cleared during processing

---

## Quick Fix Commands

### **Clear and Reset**
```csharp
// In debug console or test code
SecureStorage.RemoveAll();  // Clear everything
// Then re-login to set fresh session
```

### **Manual Session Set** (Testing Only)
```csharp
await SecureStorage.SetAsync("user_id", "1");
await SecureStorage.SetAsync("session_token", "test_token");
await SecureStorage.SetAsync("phone_number", "1234567890");
```

---

## Build Status

? Build Successful  
? Comprehensive Logging Added  
? Ready for Debugging  

---

## Next Steps

1. **Run app** in debug mode
2. **Login** with OTP
3. **Watch Output** window for all logs
4. **Navigate** to Prescription Upload
5. **Check logs** at each step
6. **Report** which step shows `NULL` for user_id

This will pinpoint exactly where the session is being lost!
