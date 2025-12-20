# MedRemind Backend - Quick Setup Guide

## ?? Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or Visual Studio Code
- Android SDK (for Android development)
- Xcode (for iOS development, Mac only)

## ?? Project Structure

```
MedRemind/
??? src/
?   ??? MedRemind.Core/              # Core models, DTOs, interfaces
?   ?   ??? Models/                  # Entity models
?   ?   ??? DTOs/                    # Data transfer objects
?   ?   ??? Interfaces/              # Service interfaces
?   ?   ??? Data/                    # Database context
?   ?   ??? Repositories/            # Repository pattern implementation
?   ??? src/MedRemind.Services/      # Business logic services
?   ?   ??? Authentication/          # Auth & security services
?   ?   ??? AI/                      # OpenAI integration
?   ?   ??? Reminders/               # Reminder scheduling
?   ?   ??? Notifications/           # Notification management
?   ?   ??? Medications/             # Medication CRUD & adherence
?   ?   ??? Media/                   # Audio recording services
?   ??? src/MedRemind.Mobile/        # MAUI mobile app (pending UI)
?   ??? MedRemind.Tests/             # Unit tests
??? docs/                            # Project documentation
```

## ?? Setup Instructions

### 1. Clone the Repository

```bash
git clone https://github.com/rajibmahata/MedRemind.git
cd MedRemind
```

### 2. Restore NuGet Packages

```bash
dotnet restore src/MedRemind.sln
```

### 3. Build the Solution

```bash
dotnet build src/MedRemind.sln
```

### 4. Run Unit Tests

```bash
# Run all tests
dotnet test src/MedRemind.Tests/MedRemind.Tests.csproj

# Run with detailed output
dotnet test src/MedRemind.Tests/MedRemind.Tests.csproj --logger "console;verbosity=detailed"

# Run specific test class
dotnet test src/MedRemind.Tests/MedRemind.Tests.csproj --filter "FullyQualifiedName~MedicationServiceTests"
```

### 5. Configure API Keys

Create `appsettings.json` in the Mobile project:

```json
{
  "OpenAI": {
    "ApiKey": "sk-YOUR_OPENAI_API_KEY",
    "Model": "gpt-4o"
  },
  "TwoFactor": {
    "ApiKey": "YOUR_2FACTOR_API_KEY"
  },
  "Database": {
    "ConnectionString": "Data Source=medremind.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

?? **Important:** Add `appsettings.json` to `.gitignore` to protect API keys

### 6. Initialize Database

The database will be automatically created on first run. To manually initialize:

```csharp
// In your startup code
var context = serviceProvider.GetRequiredService<MedRemindDbContext>();
await context.InitializeDatabaseAsync();
```

## ?? Service Registration (Dependency Injection)

Add to `MauiProgram.cs`:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        });

    // Configuration
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .Build();
    builder.Configuration.AddConfiguration(configuration);

    // Database
    builder.Services.AddDbContext<MedRemindDbContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("Database")));

    // Repository & Unit of Work
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Authentication Services
    builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddSingleton<IBiometricService, BiometricService>(); // Platform-specific

    // AI Services
    builder.Services.AddHttpClient<IPrescriptionReaderService, OpenAIPrescriptionReaderService>();
    builder.Services.AddSingleton<IValidationAgentService, MedicineValidationAgent>();

    // Reminder & Notification Services
    builder.Services.AddSingleton<IReminderSchedulingService, ReminderSchedulingService>();
    builder.Services.AddSingleton<INotificationService, LocalNotificationService>();

    // Medication Services
    builder.Services.AddScoped<MedicationService>();
    builder.Services.AddScoped<AdherenceService>();

    // Media Services
    builder.Services.AddSingleton<IAudioService, AudioService>();

    return builder.Build();
}
```

## ?? Platform-Specific Setup

### Android (AndroidManifest.xml)

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.USE_BIOMETRIC" />
<uses-permission android:name="android.permission.USE_FINGERPRINT" />
```

### iOS (Info.plist)

```xml
<key>NSCameraUsageDescription</key>
<string>We need access to your camera to scan prescription images</string>
<key>NSMicrophoneUsageDescription</key>
<string>We need access to your microphone to record voice reminders</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>We need access to your photo library to upload prescriptions</string>
<key>NSFaceIDUsageDescription</key>
<string>We use Face ID to secure your medication data</string>
```

## ?? Testing

### Run Specific Test Categories

```bash
# Repository tests
dotnet test --filter "FullyQualifiedName~RepositoryTests"

# Service tests
dotnet test --filter "FullyQualifiedName~ServiceTests"

# Authentication tests
dotnet test --filter "FullyQualifiedName~AuthenticationServiceTests"
```

### Test Coverage

The solution includes 100+ unit tests covering:
- Repository CRUD operations
- Unit of Work transactions
- Authentication flow
- AI prescription reading & validation
- Reminder scheduling (15+ patterns)
- Medication management
- Adherence tracking

## ?? Database Entities

| Entity | Description | Key Fields |
|--------|-------------|------------|
| User | User profile & authentication | PhoneNumber, Name, BiometricEnabled |
| Medication | Medication details | Name, Dosage, Frequency, Duration |
| Reminder | Scheduled reminders | MedicationId, ReminderTime, VoiceRecordingId |
| Prescription | Uploaded prescriptions | ImagePath, AIResponse, ConfidenceScore |
| VoiceRecording | Voice messages | Name, FilePath, Duration |
| DoseLog | Adherence tracking | ScheduledTime, TakenTime, Status |
| AppSettings | User preferences | Theme, NotificationsEnabled, FontSize |

## ?? Common Use Cases

### 1. Create a New User

```csharp
var authService = serviceProvider.GetRequiredService<IAuthenticationService>();

// Send OTP
var sendResult = await authService.SendOtpAsync("9876543210");

// Verify OTP
var verifyResult = await authService.VerifyOtpAsync("9876543210", "123456");
if (verifyResult.Success)
{
    var token = verifyResult.Token; // Store securely
}
```

### 2. Process a Prescription

```csharp
var prescriptionReader = serviceProvider.GetRequiredService<IPrescriptionReaderService>();

// Read prescription image
var result = await prescriptionReader.ReadPrescriptionAsync("/path/to/image.jpg");

if (result.Success)
{
    foreach (var med in result.Medications)
    {
        Console.WriteLine($"{med.Name} - {med.Dosage} {med.Unit}");
    }
}
```

### 3. Create Medication with Reminders

```csharp
var medicationService = serviceProvider.GetRequiredService<MedicationService>();

var medicationData = new MedicationData
{
    Name = "Paracetamol",
    Dosage = "500",
    Unit = "mg",
    Frequency = "Twice daily",
    FrequencyCount = 2,
    DurationDays = 7
};

var medication = await medicationService.CreateMedicationAsync(userId, medicationData);
var reminders = await medicationService.CreateRemindersAsync(medication.Id, 2);
```

### 4. Track Adherence

```csharp
var adherenceService = serviceProvider.GetRequiredService<AdherenceService>();

// Get weekly adherence
var weeklyData = await adherenceService.GetWeeklyAdherenceAsync(userId);
Console.WriteLine($"Adherence: {weeklyData.OverallPercentage:F1}%");

// Get longest streak
var streak = await adherenceService.CalculateLongestStreakAsync(userId);
Console.WriteLine($"Longest streak: {streak} days");
```

## ?? Troubleshooting

### Build Errors

**Error:** "Package not found"
```bash
dotnet restore --force
dotnet build
```

**Error:** "Entity Framework migrations"
```bash
# If needed, create migration
dotnet ef migrations add InitialCreate --project src/MedRemind.Core

# Update database
dotnet ef database update --project src/MedRemind.Core
```

### Test Failures

**Error:** "Database locked"
- Tests use in-memory databases, should not lock
- Ensure proper disposal in test fixtures

**Error:** "OpenAI API key not found"
- Mock the HttpClient for OpenAI tests
- Or set test API key in environment variables

## ?? Additional Resources

- [Detailed Timeline](06-detailed-timeline.md) - Complete 2-week implementation plan
- [Technical Architecture](02-technical-architecture.md) - System design
- [Backend Implementation Summary](07-backend-implementation-summary.md) - What's been built

## ?? Support

For issues or questions:
1. Check existing documentation in `docs/` folder
2. Review unit tests for usage examples
3. Create an issue on GitHub

---

**Last Updated:** December 20, 2024  
**Version:** 1.0.0  
**Status:** Backend Complete ?
