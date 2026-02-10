using System.Net.Http.Json;
using System.Text.Json;

namespace MedRemind.Web.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private const string TOKEN_KEY = "authToken";
    private string? _cachedToken;
    
    public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }
    
    public async Task<bool> SendOtpAsync(string phoneNumber)
    {
        try
        {
            var request = new { PhoneNumber = phoneNumber };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> RegisterAsync(RegisterModel model)
    {
        try
        {
            var request = new
            {
                Name = model.Name,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Password = model.Password,
                DateOfBirth = model.DateOfBirth
            };
            
            var response = await _httpClient.PostAsJsonAsync("/api/users/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<RegistrationResponse>();
                return result?.Success ?? false;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<(bool Success, string? Token, string? ErrorMessage)> VerifyOtpAsync(string phoneNumber, string otp)
    {
        try
        {
            var request = new { PhoneNumber = phoneNumber, Otp = otp };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/verify-otp", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.Token != null)
                {
                    await _localStorage.SetItemAsync(TOKEN_KEY, result.Token);
                    _cachedToken = result.Token;
                    return (true, result.Token, null);
                }
            }
            
            return (false, null, "Invalid OTP");
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
    
    public async Task<bool> ResendOtpAsync(string phoneNumber, string purpose = "Registration")
    {
        try
        {
            var request = new { PhoneNumber = phoneNumber, Purpose = purpose };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/resend-otp", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(_cachedToken);
    }
    
    public string? GetToken()
    {
        return _cachedToken;
    }
    
    public async Task InitializeAsync()
    {
        _cachedToken = await _localStorage.GetItemAsync(TOKEN_KEY);
    }
    
    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync(TOKEN_KEY);
        _cachedToken = null;
    }
    
    public async Task<string?> GetTokenAsync()
    {
        if (_cachedToken == null)
        {
            _cachedToken = await _localStorage.GetItemAsync(TOKEN_KEY);
        }
        return _cachedToken;
    }
    
    public async Task<UserProfileData?> GetCurrentUserAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return null;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("/api/users/me");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserProfileData>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
    
    private class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserProfile? Profile { get; set; }
    }
    
    private class RegistrationResponse
    {
        public bool Success { get; set; }
        public int? UserId { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }
    
    private class UserProfile
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}

public class RegisterModel
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
}



