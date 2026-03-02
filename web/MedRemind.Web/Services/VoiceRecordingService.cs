using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MedRemind.Web.Models;

namespace MedRemind.Web.Services;

public class VoiceRecordingService : IVoiceRecordingService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly string _apiUrl;

    public VoiceRecordingService(HttpClient httpClient, ILocalStorageService localStorage, IConfiguration configuration)
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

    public async Task<VoiceRecordingUploadResponse?> UploadRecordingAsync(CreateVoiceRecordingModel model)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_apiUrl}/api/voicerecordings/upload", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Upload failed: {errorContent}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<VoiceRecordingUploadResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading voice recording: {ex.Message}");
            return null;
        }
    }

    public async Task<List<VoiceRecordingModel>> GetUserRecordingsAsync()
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"{_apiUrl}/api/voicerecordings");

            if (!response.IsSuccessStatusCode)
                return new List<VoiceRecordingModel>();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<VoiceRecordingModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<VoiceRecordingModel>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting voice recordings: {ex.Message}");
            return new List<VoiceRecordingModel>();
        }
    }

    public async Task<VoiceRecordingModel?> GetRecordingByIdAsync(int id)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.GetAsync($"{_apiUrl}/api/voicerecordings/{id}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<VoiceRecordingModel>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting voice recording {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateRecordingNameAsync(int id, string name)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var model = new UpdateVoiceRecordingModel { Id = id, Name = name };
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"{_apiUrl}/api/voicerecordings/{id}", content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating voice recording {id}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteRecordingAsync(int id)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync();
            var response = await client.DeleteAsync($"{_apiUrl}/api/voicerecordings/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting voice recording {id}: {ex.Message}");
            return false;
        }
    }

    public string GetPlaybackUrl(int id)
    {
        return $"{_apiUrl}/api/voicerecordings/{id}/play";
    }
}
