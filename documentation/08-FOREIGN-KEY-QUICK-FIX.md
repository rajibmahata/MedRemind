# ? Foreign Key Fix - Quick Action Guide

## ?? If Error Still Occurs

The fix is complete in the code, but **the running app needs to be restarted**.

---

## ?? Steps to Apply Fix

### **1. Stop the Running App**
```
In Visual Studio:
- Click "Stop Debugging" (Shift+F5)
- Or close the app on the device
```

### **2. Clean Solution**
```bash
dotnet clean
```

Or in Visual Studio:
```
Build ? Clean Solution
```

### **3. Rebuild**
```bash
dotnet build -c Debug
```

Or in Visual Studio:
```
Build ? Rebuild Solution
```

### **4. Redeploy**
```
Press F5 to run with debugging
Or
Ctrl+F5 to run without debugging
```

### **5. Login Again**
```
1. Open app
2. Enter phone number
3. Send OTP
4. Verify OTP
5. User created and user_id stored
```

### **6. Test Prescription Upload**
```
1. Navigate to Upload Prescription
2. Select/capture image
3. Process with AI
4. Save medications
5. ? Should work without errors
```

---

## ? What Was Fixed

### Code Changes Made

1. **BaseViewModel.cs** - Added helper method:
```csharp
protected async Task<int> GetCurrentUserIdAsync()
{
    var userIdString = await SecureStorage.GetAsync("user_id");
    // Validation and error handling
    return userId;
}
```

2. **PrescriptionUploadViewModel.cs** - Updated to use helper:
```csharp
// OLD
var userIdString = await SecureStorage.GetAsync("UserId"); // Wrong key
int userId = int.TryParse(userIdString, out var id) ? id : 1; // Default to 1

// NEW
var userId = await GetCurrentUserIdAsync(); // Correct key + validation
```

---

## ?? Quick Test

### Verify Fix is Applied

Run this test after restarting:

```
1. Login ? Should succeed
2. Check logs for "? User logged in: [user_id]"
3. Upload ? Select image
4. Process ? Should show processing indicator
5. Save ? Should succeed
6. Check logs: No "SQLite Error 19" messages
```

---

## ?? Expected Behavior

### Before Fix (Error)
```
? SQLite Error 19: 'FOREIGN KEY constraint failed'
? Prescription not saved
? App appears broken
```

### After Fix (Success)
```
? Prescription saved successfully
? Medications extracted and saved
? No database errors
? Full workflow functional
```

---

## ?? If Error Still Persists

### Check These:

1. **User Logged In?**
```
- Verify login completed successfully
- Check SecureStorage has "user_id" key
- Log: await SecureStorage.GetAsync("user_id")
```

2. **Database State?**
```
- User exists in database
- User ID matches SecureStorage value
- Foreign key constraints enabled
```

3. **Code Updated?**
```
- Verify latest code deployed
- Check file timestamps
- Rebuild from scratch if needed
```

---

## ?? Debug Logging

Add this to check user ID:

```csharp
// In ProcessPrescriptionAsync, before saving prescription
var userId = await GetCurrentUserIdAsync();
System.Diagnostics.Debug.WriteLine($"?? Saving prescription for user ID: {userId}");
```

Should see:
```
?? Saving prescription for user ID: 2
? Prescription saved successfully
```

---

## ? Verification Checklist

- [ ] App stopped completely
- [ ] Solution cleaned
- [ ] Solution rebuilt
- [ ] App redeployed
- [ ] User logged in (fresh login)
- [ ] user_id stored in SecureStorage
- [ ] Prescription upload tested
- [ ] No foreign key errors
- [ ] Medications save successfully

---

## ?? Status

**Fix**: ? Complete in code  
**Build**: ? Successful  
**Deployment**: ?? **Restart required**  

---

**Restart the app to apply the fix! ??**
