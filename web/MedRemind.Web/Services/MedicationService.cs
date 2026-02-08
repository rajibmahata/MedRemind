using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace MedRemind.Web.Services;

public class MedicationService : IMedicationService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    
    public MedicationService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }
    
    public async Task<List<MedicationDto>> GetAllAsync()
    {
        try
        {
            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            var medications = await _httpClient.GetFromJsonAsync<List<MedicationDto>>("/api/medications");
            return medications ?? new List<MedicationDto>();
        }
        catch
        {
            return new List<MedicationDto>();
        }
    }
    
    public async Task<MedicationDto?> GetByIdAsync(int id)
    {
        try
        {
            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            return await _httpClient.GetFromJsonAsync<MedicationDto>($"/api/medications/{id}");
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<bool> UpdateAsync(int id, MedicationDto medication)
    {
        try
        {
            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            var response = await _httpClient.PutAsJsonAsync($"/api/medications/{id}", medication);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var token = _authService.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            var response = await _httpClient.DeleteAsync($"/api/medications/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
