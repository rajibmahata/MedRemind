namespace MedRemind.Web.Models;

/// <summary>
/// Complete application configuration
/// </summary>
public class AppConfiguration
{
    public ApiSettings ApiSettings { get; set; } = new();
    public EndpointsConfiguration Endpoints { get; set; } = new();
    public AppSettings AppSettings { get; set; } = new();
    public FeaturesConfiguration Features { get; set; } = new();
    public AuthenticationConfiguration Authentication { get; set; } = new();
    public StorageConfiguration Storage { get; set; } = new();
    public UIConfiguration UI { get; set; } = new();
}

/// <summary>
/// API connection settings
/// </summary>
public class ApiSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public int Timeout { get; set; } = 120;
    public bool EnableRetry { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 2;
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Get full URL for an endpoint
    /// </summary>
    public string GetFullUrl(string endpoint)
    {
        return $"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
    }
}

/// <summary>
/// All API endpoints organized by feature
/// </summary>
public class EndpointsConfiguration
{
    public AuthEndpoints Auth { get; set; } = new();
    public PrescriptionEndpoints Prescriptions { get; set; } = new();
    public MedicationEndpoints Medications { get; set; } = new();
    public ValidationEndpoints Validation { get; set; } = new();
    public VoiceRecordingEndpoints VoiceRecordings { get; set; } = new();
    public ReminderEndpoints Reminders { get; set; } = new();
    public UserEndpoints Users { get; set; } = new();
}

/// <summary>
/// Authentication endpoints
/// </summary>
public class AuthEndpoints
{
    public string Register { get; set; } = "/api/users/register";
    public string Login { get; set; } = "/api/auth/login";
    public string VerifyOtp { get; set; } = "/api/auth/verify-otp";
    public string ResendOtp { get; set; } = "/api/auth/resend-otp";
    public string CurrentUser { get; set; } = "/api/users/me";
}

/// <summary>
/// Prescription endpoints
/// </summary>
public class PrescriptionEndpoints
{
    public string Upload { get; set; } = "/api/prescriptions/upload-base64";
    public string List { get; set; } = "/api/prescriptions";
    public string Details { get; set; } = "/api/prescriptions/{id}";
    public string Image { get; set; } = "/api/prescriptions/{id}/image";
    public string Reprocess { get; set; } = "/api/prescriptions/{id}/reprocess";
    public string Delete { get; set; } = "/api/prescriptions/{id}";
    public string Statistics { get; set; } = "/api/prescriptions/statistics";

    public string GetDetails(int id) => Details.Replace("{id}", id.ToString());
    public string GetImage(int id) => Image.Replace("{id}", id.ToString());
    public string GetReprocess(int id) => Reprocess.Replace("{id}", id.ToString());
    public string GetDelete(int id) => Delete.Replace("{id}", id.ToString());
}

/// <summary>
/// Medication endpoints
/// </summary>
public class MedicationEndpoints
{
    public string List { get; set; } = "/api/medications";
    public string Details { get; set; } = "/api/medications/{id}";
    public string Search { get; set; } = "/api/medications/search";
    public string Update { get; set; } = "/api/medications/{id}";
    public string Delete { get; set; } = "/api/medications/{id}";
    public string ByPrescription { get; set; } = "/api/medications/prescription/{id}";

    public string GetDetails(int id) => Details.Replace("{id}", id.ToString());
    public string GetUpdate(int id) => Update.Replace("{id}", id.ToString());
    public string GetDelete(int id) => Delete.Replace("{id}", id.ToString());
    public string GetByPrescription(int prescriptionId) => ByPrescription.Replace("{id}", prescriptionId.ToString());
    public string GetSearch(string query) => $"{Search}?query={Uri.EscapeDataString(query)}";
}

/// <summary>
/// Validation workflow endpoints
/// </summary>
public class ValidationEndpoints
{
    public string GetWorkflow { get; set; } = "/api/validation/workflow/{id}";
    public string ConfirmMedication { get; set; } = "/api/validation/medication/{id}/confirm";
    public string CorrectMedication { get; set; } = "/api/validation/medication/{id}/correct";
    public string DeleteMedication { get; set; } = "/api/validation/medication/{id}";
    public string CompleteWorkflow { get; set; } = "/api/validation/workflow/{id}/complete";
    public string Progress { get; set; } = "/api/validation/workflow/{id}/progress";

    public string GetWorkflowUrl(int id) => GetWorkflow.Replace("{id}", id.ToString());
    public string GetConfirmMedicationUrl(int id) => ConfirmMedication.Replace("{id}", id.ToString());
    public string GetCorrectMedicationUrl(int id) => CorrectMedication.Replace("{id}", id.ToString());
    public string GetDeleteMedicationUrl(int id) => DeleteMedication.Replace("{id}", id.ToString());
    public string GetCompleteWorkflowUrl(int id) => CompleteWorkflow.Replace("{id}", id.ToString());
    public string GetProgressUrl(int id) => Progress.Replace("{id}", id.ToString());
}

/// <summary>
/// Voice recording endpoints
/// </summary>
public class VoiceRecordingEndpoints
{
    public string List { get; set; } = "/api/voice-recordings";
    public string Upload { get; set; } = "/api/voice-recordings/upload-base64";
    public string Details { get; set; } = "/api/voice-recordings/{id}";
    public string Play { get; set; } = "/api/voice-recordings/{id}/play";
    public string PlaybackUrl { get; set; } = "/api/voice-recordings/{id}/playback-url";
    public string Update { get; set; } = "/api/voice-recordings/{id}";
    public string Delete { get; set; } = "/api/voice-recordings/{id}";

    public string GetDetails(int id) => Details.Replace("{id}", id.ToString());
    public string GetPlay(int id) => Play.Replace("{id}", id.ToString());
    public string GetPlaybackUrl(int id) => PlaybackUrl.Replace("{id}", id.ToString());
    public string GetUpdate(int id) => Update.Replace("{id}", id.ToString());
    public string GetDelete(int id) => Delete.Replace("{id}", id.ToString());
}

/// <summary>
/// Reminder endpoints
/// </summary>
public class ReminderEndpoints
{
    public string List { get; set; } = "/api/reminders";
    public string Create { get; set; } = "/api/reminders";
    public string CreateBulk { get; set; } = "/api/reminders/bulk";
    public string Details { get; set; } = "/api/reminders/{id}";
    public string Update { get; set; } = "/api/reminders/{id}";
    public string Toggle { get; set; } = "/api/reminders/{id}/toggle";
    public string Delete { get; set; } = "/api/reminders/{id}";
    public string CalculateTimes { get; set; } = "/api/reminders/calculate-times";
    public string Upcoming { get; set; } = "/api/reminders/upcoming";
    public string ByMedication { get; set; } = "/api/reminders/medication/{id}";

    public string GetDetails(int id) => Details.Replace("{id}", id.ToString());
    public string GetUpdate(int id) => Update.Replace("{id}", id.ToString());
    public string GetToggle(int id) => Toggle.Replace("{id}", id.ToString());
    public string GetDelete(int id) => Delete.Replace("{id}", id.ToString());
    public string GetByMedication(int medicationId) => ByMedication.Replace("{id}", medicationId.ToString());
}

/// <summary>
/// User profile endpoints
/// </summary>
public class UserEndpoints
{
    public string Profile { get; set; } = "/api/users/{id}";
    public string UpdateProfile { get; set; } = "/api/users/{id}";
    public string DeleteAccount { get; set; } = "/api/users/{id}";
    public string CheckExists { get; set; } = "/api/users/exists/{phoneNumber}";

    public string GetProfile(int id) => Profile.Replace("{id}", id.ToString());
    public string GetUpdateProfile(int id) => UpdateProfile.Replace("{id}", id.ToString());
    public string GetDeleteAccount(int id) => DeleteAccount.Replace("{id}", id.ToString());
    public string GetCheckExists(string phoneNumber) => CheckExists.Replace("{phoneNumber}", phoneNumber);
}

/// <summary>
/// General application settings
/// </summary>
public class AppSettings
{
    public string AppName { get; set; } = "MedRemind";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
}

/// <summary>
/// Feature flags
/// </summary>
public class FeaturesConfiguration
{
    public bool EnableNotifications { get; set; } = true;
    public bool EnableVoiceRecordings { get; set; } = true;
    public bool EnableDarkMode { get; set; } = true;
    public bool EnableOfflineMode { get; set; } = false;
    public bool EnableAnalytics { get; set; } = false;
}

/// <summary>
/// Authentication configuration
/// </summary>
public class AuthenticationConfiguration
{
    public int OtpLength { get; set; } = 6;
    public int OtpExpiryMinutes { get; set; } = 5;
    public int TokenExpiryDays { get; set; } = 7;
    public bool RequireEmailVerification { get; set; } = true;
}

/// <summary>
/// Storage configuration
/// </summary>
public class StorageConfiguration
{
    public string LocalStorageKey { get; set; } = "medremind_auth";
    public int CacheDurationMinutes { get; set; } = 30;
    public int MaxCacheSizeMB { get; set; } = 50;
}

/// <summary>
/// UI configuration
/// </summary>
public class UIConfiguration
{
    public string DefaultTheme { get; set; } = "light";
    public bool EnableAnimations { get; set; } = true;
    public int ItemsPerPage { get; set; } = 10;
    public int MaxFileUploadSizeMB { get; set; } = 10;
}
