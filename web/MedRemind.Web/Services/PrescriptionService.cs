using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace MedRemind.Web.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    
    public PrescriptionService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }
    
    public async Task<UploadResponse> UploadPrescriptionAsync(Stream fileStream, string fileName)
    {
        try
        {
            // Add auth token
            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            // Create multipart form content
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(streamContent, "file", fileName);
            
            // Upload
            var response = await _httpClient.PostAsync("/api/prescriptions/upload", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadResponse>();
                return result ?? new UploadResponse { Success = false, Message = "Invalid response" };
            }
            
            return new UploadResponse 
            { 
                Success = false, 
                Message = $"Upload failed: {response.ReasonPhrase}" 
            };
        }
        catch (Exception ex)
        {
            return new UploadResponse 
            { 
                Success = false, 
                Message = $"Error: {ex.Message}" 
            };
        }
    }
    
    public async Task<List<PrescriptionDto>> GetAllAsync()
    {
        try
        {
            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            var prescriptions = await _httpClient.GetFromJsonAsync<List<PrescriptionDto>>("/api/prescriptions");
            return prescriptions ?? new List<PrescriptionDto>();
        }
        catch
        {
            return new List<PrescriptionDto>();
        }
    }
    
    public async Task<PrescriptionDto?> GetByIdAsync(int id)
    {
        try
        {
            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            return await _httpClient.GetFromJsonAsync<PrescriptionDto>($"/api/prescriptions/{id}");
        }
        catch
        {
            return null;
        }
    }
}
