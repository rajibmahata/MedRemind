using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MedRemind.Web.Models;

namespace MedRemind.Web.Services;

public class ValidationService : IValidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly string _apiUrl;

    public ValidationService(HttpClient httpClient, ILocalStorageService localStorage, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _apiUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var token = await _localStorage.GetItemAsync("sessionToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return _httpClient;
    }

    public async Task<ValidationWorkflowResponse?> GetValidationWorkflowAsync(int prescriptionId)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"{_apiUrl}/api/validation/prescription/{prescriptionId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ValidationWorkflowResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting validation workflow: {ex.Message}");
            return null;
        }
    }

    public async Task<MedicationValidationResponse?> ConfirmMedicationAsync(int medicationId, bool acknowledgeWarnings, string? notes = null, bool requiresPharmacist = false)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var request = new
            {
                medicationId,
                acknowledgeWarnings,
                validationNotes = notes,
                requiresPharmacistConsultation = requiresPharmacist
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}/api/validation/medication/confirm", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MedicationValidationResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error confirming medication: {ex.Message}");
            return null;
        }
    }

    public async Task<MedicationValidationResponse?> CorrectMedicationAsync(CorrectMedicationRequest request)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{_apiUrl}/api/validation/medication/correct", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MedicationValidationResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error correcting medication: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteMedicationAsync(int medicationId, string reason)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var request = new { reason };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{_apiUrl}/api/validation/medication/{medicationId}")
            {
                Content = content
            };

            var response = await client.SendAsync(requestMessage);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting medication: {ex.Message}");
            return false;
        }
    }

    public async Task<ValidationWorkflowResponse?> CompleteValidationAsync(int prescriptionId, bool consultedPharmacist = false, string? pharmacistNotes = null)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var request = new
            {
                prescriptionId,
                userConsultedPharmacist = consultedPharmacist,
                pharmacistNotes
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}/api/validation/complete", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ValidationWorkflowResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error completing validation: {ex.Message}");
            return null;
        }
    }
}
