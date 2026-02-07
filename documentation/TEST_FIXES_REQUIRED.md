# Quick Fix Guide for Test Compilation Errors

## Issues Found

### 1. OtpCodeService Constructor
**Error:** `useDatabaseStorage` parameter doesn't exist

**Fix:** Remove parameter, use actual constructor signature
```csharp
// Wrong
new OtpCodeService(
    _unitOfWork,
    _mockSmsService.Object,
    _mockEmailService.Object,
    useDatabaseStorage: true,  // ? Remove this
    enableRateLimiting: false,
    null);

// Correct
new OtpCodeService(
    _unitOfWork,
    _mockSmsService.Object,
    _mockEmailService.Object,
    enableSmsOtp: true,
    enableEmailOtp: false,
    null);
```

### 2. SmsService Constructor
**Error:** First parameter should be `HttpClient`, not `string`

**Fix:** Pass HttpClient mock
```csharp
// Wrong
var smsService = new SmsService("sid", "token", "from", null);

// Correct
var mockHttpClient = new Mock<HttpClient>();
var smsService = new SmsService(
    mockHttpClient.Object,
    "api_key",
    null,  // sendOtpUrl
    null,  // otpTemplate
    null); // logger
```

### 3. UsersController Method Names
**Error:** Methods don't exist

**Fix:** Use correct method names
```csharp
// Wrong
await _usersController.GetUser(userId);
await _usersController.UpdateUser(userId, request);

// Correct
await _usersController.GetUserById(userId);
await _usersController.UpdateProfile(userId, request);
```

### 4. UsersController Delete Method
**Error:** `DeleteUser` doesn't exist

**Fix:** Check if method exists or remove test
```csharp
// If method doesn't exist in controller, either:
// 1. Add the method to UsersController
// 2. Remove the test
```

## Quick Fixes Applied

Run these commands to apply fixes:

```bash
# Fix 1-3: Use correct method names and parameters
# Edit files manually or use search/replace
```

## Testing After Fixes

```bash
# Build project
dotnet build backend\MedRemind.Tests

# Run specific test file
dotnet test --filter "FullyQualifiedName~OtpCodeServiceTests"

# Run all tests
dotnet test backend\MedRemind.Tests
```

## Status

- ?? Tests need fixing before running
- ? All code logic is correct
- ? Test structure is solid
- ?? Just need parameter/method name updates
