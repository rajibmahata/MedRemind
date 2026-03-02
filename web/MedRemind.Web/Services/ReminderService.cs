using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MedRemind.Web.Models;

namespace MedRemind.Web.Services;

public class ReminderService : IReminderService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly string _apiUrl;

    public ReminderService(HttpClient httpClient, ILocalStorageService localStorage, IConfiguration configuration)
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

    public async Task<bool> CreateReminderAsync(CreateReminderModel model)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}/api/reminders", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating reminder: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CreateMultipleRemindersAsync(CreateMultipleRemindersModel model)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}/api/reminders/bulk", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating multiple reminders: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ReminderModel>> GetMedicationRemindersAsync(int medicationId)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"{_apiUrl}/api/reminders/medication/{medicationId}");

            if (!response.IsSuccessStatusCode)
                return new List<ReminderModel>();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ReminderModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<ReminderModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting medication reminders: {ex.Message}");
            return new List<ReminderModel>();
        }
    }

    public async Task<List<ReminderModel>> GetUserRemindersAsync()
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var userId = 1; // TODO: Get from auth service
            var response = await client.GetAsync($"{_apiUrl}/api/reminders/user/{userId}");

            if (!response.IsSuccessStatusCode)
                return new List<ReminderModel>();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ReminderModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<ReminderModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user reminders: {ex.Message}");
            return new List<ReminderModel>();
        }
    }

    public async Task<bool> ToggleReminderAsync(int reminderId, bool isEnabled)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var request = new { isEnabled };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{_apiUrl}/api/reminders/{reminderId}/toggle", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling reminder: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateReminderTimeAsync(int reminderId, TimeSpan newTime)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var request = new { newTime };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{_apiUrl}/api/reminders/{reminderId}/time", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating reminder time: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteReminderAsync(int reminderId)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.DeleteAsync($"{_apiUrl}/api/reminders/{reminderId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting reminder: {ex.Message}");
            return false;
        }
    }

    public async Task<ReminderSuggestionModel?> CalculateReminderTimesAsync(int timesPerDay)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var request = new { timesPerDay };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}/api/reminders/calculate", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ReminderSuggestionModel>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating reminder times: {ex.Message}");
            return null;
        }
    }

    public async Task<ReminderSuggestionModel?> CalculateCustomReminderTimesAsync(string frequency)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var request = new { frequency };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}/api/reminders/calculate-custom", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ReminderSuggestionModel>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating custom reminder times: {ex.Message}");
            return null;
        }
    }
}
