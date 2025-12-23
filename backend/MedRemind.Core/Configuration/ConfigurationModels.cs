namespace MedRemind.Core.Configuration;

/// <summary>
/// Main application configuration
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// Application environment (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Enable analytics tracking
    /// </summary>
    public bool EnableAnalytics { get; set; } = false;

    /// <summary>
    /// Enable crash reporting
    /// </summary>
    public bool EnableCrashReporting { get; set; } = false;

    /// <summary>
    /// Maximum prescription cache size (number of prescriptions)
    /// </summary>
    public int MaxPrescriptionCacheSize { get; set; } = 50;

    /// <summary>
    /// Notification lead time in minutes
    /// </summary>
    public int NotificationLeadTime { get; set; } = 30;

    /// <summary>
    /// Enable biometric authentication by default
    /// </summary>
    public bool DefaultBiometricEnabled { get; set; } = true;

    /// <summary>
    /// Session timeout in minutes
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Auto-logout after X days of inactivity
    /// </summary>
    public int AutoLogoutDays { get; set; } = 30;
}

/// <summary>
/// API configuration for external services
/// </summary>
public class ApiConfiguration
{
    /// <summary>
    /// OpenAI API Key for prescription reading
    /// </summary>
    public string OpenAI_APIKey { get; set; } = string.Empty;

    /// <summary>
    /// 2Factor.in API Key for SMS OTP
    /// </summary>
    public string TwoFactor_APIKey { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI API timeout in seconds
    /// </summary>
    public int OpenAI_TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// OpenAI model to use
    /// </summary>
    public string OpenAI_Model { get; set; } = "gpt-4o";

    /// <summary>
    /// Maximum image size for processing (MB)
    /// </summary>
    public int MaxImageSizeMB { get; set; } = 10;

    /// <summary>
    /// Enable API call caching
    /// </summary>
    public bool EnableApiCaching { get; set; } = true;

    /// <summary>
    /// API cache duration in hours
    /// </summary>
    public int ApiCacheDurationHours { get; set; } = 24;
}

/// <summary>
/// Feature flags for enabling/disabling features
/// </summary>
public class FeatureConfiguration
{
    /// <summary>
    /// Enable prescription upload feature
    /// </summary>
    public bool EnablePrescriptionUpload { get; set; } = true;

    /// <summary>
    /// Enable voice reminders
    /// </summary>
    public bool EnableVoiceReminders { get; set; } = true;

    /// <summary>
    /// Enable adherence tracking
    /// </summary>
    public bool EnableAdherenceTracking { get; set; } = true;

    /// <summary>
    /// Enable medication interaction warnings
    /// </summary>
    public bool EnableInteractionWarnings { get; set; } = true;

    /// <summary>
    /// Enable offline mode
    /// </summary>
    public bool EnableOfflineMode { get; set; } = true;

    /// <summary>
    /// Enable dark mode
    /// </summary>
    public bool EnableDarkMode { get; set; } = false;
}

/// <summary>
/// Notification configuration
/// </summary>
public class NotificationConfiguration
{
    /// <summary>
    /// Enable sound for notifications
    /// </summary>
    public bool EnableSound { get; set; } = true;

    /// <summary>
    /// Enable vibration for notifications
    /// </summary>
    public bool EnableVibration { get; set; } = true;

    /// <summary>
    /// Enable LED notification
    /// </summary>
    public bool EnableLED { get; set; } = true;

    /// <summary>
    /// Snooze duration in minutes
    /// </summary>
    public int SnoozeDurationMinutes { get; set; } = 10;

    /// <summary>
    /// Max snooze attempts
    /// </summary>
    public int MaxSnoozeAttempts { get; set; } = 3;

    /// <summary>
    /// Enable persistent notifications
    /// </summary>
    public bool PersistentNotifications { get; set; } = true;
}

/// <summary>
/// Complete configuration bundle
/// </summary>
public class MedRemindConfiguration
{
    public AppConfiguration App { get; set; } = new();
    public ApiConfiguration Api { get; set; } = new();
    public FeatureConfiguration Features { get; set; } = new();
    public NotificationConfiguration Notifications { get; set; } = new();

    /// <summary>
    /// Configuration version for migration tracking
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
