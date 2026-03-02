using MedRemind.Web.Models;

namespace MedRemind.Web.Services;

public interface IVoiceRecordingService
{
    Task<VoiceRecordingUploadResponse?> UploadRecordingAsync(CreateVoiceRecordingModel model);
    Task<List<VoiceRecordingModel>> GetUserRecordingsAsync();
    Task<VoiceRecordingModel?> GetRecordingByIdAsync(int id);
    Task<bool> UpdateRecordingNameAsync(int id, string name);
    Task<bool> DeleteRecordingAsync(int id);
    string GetPlaybackUrl(int id);
}
