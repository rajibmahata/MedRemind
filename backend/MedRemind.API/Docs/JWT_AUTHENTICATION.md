# JWT Authentication Setup Guide

Complete guide for JWT authentication implementation in MedRemind API.

---

## ? What Was Implemented

### JWT Authentication System
- **JWT token generation** after successful OTP verification
- **Token validation** for protected endpoints
- **Bearer token authentication** with ASP.NET Core
- **Environment-based configuration** for JWT settings
- **Protected controllers** with [Authorize] attribute

---

## ?? How JWT Authentication Works

### Authentication Flow

```
1. User sends OTP request ? POST /api/Auth/send-otp
2. User verifies OTP ? POST /api/Auth/verify-otp
3. API returns JWT token in response
4. User includes token in subsequent requests
5. API validates token before processing request
```

### JWT Token Structure

```json
{
  "sub": "userId",
  "jti": "unique-token-id",
  "iat": "issued-at-timestamp",
  "nameidentifier": "userId",
  "exp": "expiration-timestamp"
}
```

---

## ?? Configuration

### JWT Settings in `appsettings.json`

Each environment (Development/Staging/Production) has JWT configuration:

```json
{
  "Environments": {
    "Development": {
      "Jwt": {
        "SecretKey": "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS",
        "Issuer": "MedRemind.API",
        "Audience": "MedRemind.Mobile",
        "ExpirationDays": 30
      }
    }
  }
}
```

### Configuration Properties

| Property | Description | Example |
|----------|-------------|---------|
| **SecretKey** | Secret key for signing tokens (min 32 chars) | `"your-secret-key-here"` |
| **Issuer** | Token issuer identifier | `"MedRemind.API"` |
| **Audience** | Intended token audience | `"MedRemind.Mobile"` |
| **ExpirationDays** | Token expiration period | `30` |

?? **Security Note**: Change the default `SecretKey` before deploying to production!

---

## ?? Implementation Details

### 1. Program.cs Configuration

```csharp
// JWT Authentication setup
var jwtSecretKey = environmentConfig["Jwt:SecretKey"] ?? "YOUR_SECRET_KEY_HERE_MINIMUM_32_CHARACTERS";
var jwtIssuer = environmentConfig["Jwt:Issuer"] ?? "MedRemind.API";
var jwtAudience = environmentConfig["Jwt:Audience"] ?? "MedRemind.Mobile";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// Enable authentication & authorization
app.UseAuthentication();
app.UseAuthorization();
```

### 2. AuthenticationService Updates

**Token Generation:**
```csharp
public async Task<string> GenerateSessionTokenAsync(int userId)
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _jwtIssuer,
        audience: _jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddDays(_jwtExpirationDays),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Token Validation:**
```csharp
public async Task<bool> ValidateSessionTokenAsync(string token)
{
    try
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecretKey);

        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _jwtIssuer,
            ValidateAudience = true,
            ValidAudience = _jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out SecurityToken validatedToken);

        return true;
    }
    catch
    {
        return false;
    }
}
```

### 3. Protected Controllers

All controllers except AuthController require JWT authentication:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requires JWT token
public class MedicationsController : ControllerBase
{
    // Endpoints require valid JWT token in Authorization header
}
```

**Protected Controllers:**
- ? MedicationsController
- ? PrescriptionsController
- ? RemindersController
- ? AdherenceController

**Public Endpoints:**
- ? AuthController (no authentication required)

---

## ?? Testing JWT Authentication

### 1. Get JWT Token

**Request:**
```bash
POST /api/Auth/verify-otp
Content-Type: application/json

{
  "phoneNumber": "+919876543210",
  "otp": "123456"
}
```

**Response:**
```json
{
  "message": "Login successful",
  "phoneNumber": "+919876543210",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwianRpIjoiZjg3YjM0ZDItNWM0Zi00ZTZhLWE5MDEtYzJkNWY2YTg3YmM4IiwiaWF0IjoiMTcwNjU1NjAwMCIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWVpZGVudGlmaWVyIjoiMSIsImV4cCI6MTcwOTE0ODAwMCwiaXNzIjoiTWVkUmVtaW5kLkFQSSIsImF1ZCI6Ik1lZFJlbWluZC5Nb2JpbGUifQ.8xKm8YBJGl5M_N3ZrJQp8TK4Wb5kLmZhW0xYj2kU3Gc"
}
```

### 2. Use Token in Requests

**With cURL:**
```bash
curl -X GET "http://localhost:5124/api/Medications" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**With HTTP Client:**
```http
GET /api/Medications HTTP/1.1
Host: localhost:5124
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**With JavaScript/Fetch:**
```javascript
fetch('http://localhost:5124/api/Medications', {
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  }
})
```

### 3. Testing with Swagger UI

**Note:** Swagger JWT authorization UI is currently simplified. To test with Swagger:

1. Get token from `/api/Auth/verify-otp`
2. Copy the token value
3. For each protected endpoint, add the Authorization header manually:
   - Header name: `Authorization`
   - Header value: `Bearer {your-token}`

---

## ?? Security Best Practices

### Production Checklist

- [ ] **Change default JWT secret key** to a strong, unique value (min 32 chars)
- [ ] **Use environment variables** for secret keys (don't commit to source control)
- [ ] **Enable HTTPS only** in production
- [ ] **Set appropriate token expiration** (balance security vs. user experience)
- [ ] **Implement token refresh** mechanism for long-lived sessions
- [ ] **Add rate limiting** to authentication endpoints
- [ ] **Log authentication failures** for security monitoring
- [ ] **Implement token revocation** if needed

### Secret Key Guidelines

**Good Examples:**
```
- ComplexSecretKey2024!@#MedRemindAPIProduction
- kJ8#mL9$pQ2&nR4*vT6^wX3@yZ1!aB5%cD7(
```

**Bad Examples:**
```
- secret (too short)
- YOUR_SECRET_KEY_HERE (default value)
- 12345678 (predictable)
```

### Token Lifetime Recommendations

| Environment | Recommended Expiration |
|-------------|----------------------|
| Development | 30 days |
| Staging | 7 days |
| Production | 24 hours (with refresh token) |

---

## ?? Troubleshooting

### Common Issues

#### 1. **401 Unauthorized Error**

**Problem:** API returns 401 even with token

**Solutions:**
- Verify token is included in `Authorization` header
- Ensure format is: `Bearer {token}` (with space after "Bearer")
- Check token hasn't expired
- Verify JWT secret key matches between API and token generation

#### 2. **Token Validation Failed**

**Problem:** `ValidateSessionTokenAsync` returns false

**Causes:**
- Token expired
- Secret key mismatch
- Issuer/Audience mismatch
- Token tampered with

**Debug:**
```csharp
try
{
    tokenHandler.ValidateToken(token, parameters, out SecurityToken validatedToken);
}
catch (SecurityTokenExpiredException)
{
    // Token expired
}
catch (SecurityTokenInvalidSignatureException)
{
    // Secret key mismatch or token tampered
}
catch (SecurityTokenInvalidIssuerException)
{
    // Issuer mismatch
}
```

#### 3. **Missing Authorization Header**

**Problem:** No token sent with request

**Solution:**
```bash
# Correct format
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# Incorrect formats
Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...  # Missing "Bearer"
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...        # Missing header name
```

#### 4. **ClockSkew Issues**

**Problem:** Token accepted slightly after expiration

**Solution:** Set `ClockSkew = TimeSpan.Zero` in token validation (already configured)

---

## ?? NuGet Packages Required

### API Project (MedRemind.API)
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.1" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
```

### Services Project (MedRemind.Services)
```xml
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.2.1" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
```

---

## ?? Integration with .NET MAUI Client

### 1. Store Token in SecureStorage

```csharp
// After successful login
await SecureStorage.SetAsync("jwt_token", token);
```

### 2. Include Token in API Calls

```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;

    public async Task<HttpResponseMessage> GetMedicationsAsync()
    {
        var token = await SecureStorage.GetAsync("jwt_token");
        
        var request = new HttpRequestMessage(HttpMethod.Get, "api/Medications");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        return await _httpClient.SendAsync(request);
    }
}
```

### 3. Handle Token Expiration

```csharp
if (response.StatusCode == HttpStatusCode.Unauthorized)
{
    // Token expired - redirect to login
    await Shell.Current.GoToAsync("//login");
}
```

---

## ?? Response Codes

| Status Code | Meaning | Action |
|-------------|---------|--------|
| **200 OK** | Request successful | Continue |
| **401 Unauthorized** | Token missing, invalid, or expired | Redirect to login |
| **403 Forbidden** | Token valid but insufficient permissions | Show error message |
| **500 Internal Server Error** | Server error | Retry or show error |

---

## ?? Next Steps

### Recommended Enhancements

1. **Implement Refresh Tokens**
   - Issue short-lived access tokens (1 hour)
   - Issue long-lived refresh tokens (30 days)
   - Refresh access token without re-authentication

2. **Add Claims-Based Authorization**
   - Add user roles to JWT claims
   - Implement role-based access control
   - Use `[Authorize(Roles = "Admin")]`

3. **Implement Token Revocation**
   - Store active tokens in database/cache
   - Check token against revocation list
   - Revoke token on logout

4. **Add Multi-Factor Authentication**
   - Require additional verification for sensitive operations
   - Use biometric authentication on mobile

5. **Implement Rate Limiting**
   - Limit authentication attempts
   - Prevent brute force attacks
   - Use middleware or API gateway

---

## ?? Additional Resources

- **JWT.io**: https://jwt.io/ - Decode and verify JWT tokens
- **Microsoft Docs**: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/
- **OWASP JWT Guidelines**: https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html

---

## ? Summary

JWT authentication is now fully implemented in MedRemind API:

- ? Token generation on successful OTP verification
- ? Token validation for protected endpoints
- ? Environment-based configuration
- ? All controllers protected (except Auth)
- ? Ready for .NET MAUI client integration

**Start testing:** Get a token from `/api/Auth/verify-otp` and use it in subsequent requests!
