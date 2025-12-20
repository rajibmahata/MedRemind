# Technical Architecture - MedRemind MVP

## System Architecture Overview

### Architecture Style
**Offline-First Mobile Application with AI Integration**

```
???????????????????????????????????????????????????????????????????
?                          Mobile App (.NET MAUI)                  ?
???????????????????????????????????????????????????????????????????
?  ??????????????  ????????????????  ??????????????              ?
?  ?   Views    ????  ViewModels  ????  Services  ?              ?
?  ?   (XAML)   ?  ?   (MVVM)     ?  ?            ?              ?
?  ??????????????  ????????????????  ??????????????              ?
?                          ?                 ?                      ?
?  ??????????????????????????????????????????????????             ?
?  ?         Repositories (Data Access Layer)        ?             ?
?  ??????????????????????????????????????????????????             ?
?                          ?                                        ?
?  ??????????????????????????????????????????????????             ?
?  ?        SQLite Database (Local Storage)          ?             ?
?  ??????????????????????????????????????????????????             ?
???????????????????????????????????????????????????????????????????
                           ? (Internet Required)
???????????????????????????????????????????????????????????????????
?                      External Services                           ?
???????????????????????????????????????????????????????????????????
?  ????????????????????  ????????????????????                     ?
?  ?  OpenAI API      ?  ?  2Factor.in API  ?                     ?
?  ?  (Prescription)  ?  ?  (SMS OTP)       ?                     ?
?  ????????????????????  ????????????????????                     ?
???????????????????????????????????????????????????????????????????
```

---

## Layer Architecture

### 1. Presentation Layer (Views + ViewModels)

**Technology**: XAML + C# with MVVM pattern

#### Key Components

**Views (XAML)**
```
Views/
??? Auth/
?   ??? LoginPage.xaml
?   ??? OtpVerificationPage.xaml
?   ??? BiometricSetupPage.xaml
?   ??? UserProfilePage.xaml
??? Prescription/
?   ??? PrescriptionUploadPage.xaml
?   ??? PrescriptionReviewPage.xaml
?   ??? PrescriptionListPage.xaml
??? Medication/
?   ??? MedicationListPage.xaml
?   ??? MedicationDetailPage.xaml
?   ??? AddEditMedicationPage.xaml
??? Reminder/
?   ??? ReminderSetupPage.xaml
?   ??? ReminderListPage.xaml
?   ??? VoiceRecordingPage.xaml
??? Dashboard/
?   ??? HomePage.xaml
?   ??? CalendarView.xaml
??? Settings/
    ??? SettingsPage.xaml
```

**ViewModels (C#)**
```csharp
ViewModels/
??? Auth/
?   ??? LoginViewModel.cs
?   ??? OtpVerificationViewModel.cs
?   ??? UserProfileViewModel.cs
??? Prescription/
?   ??? PrescriptionUploadViewModel.cs
?   ??? PrescriptionReviewViewModel.cs
??? Medication/
?   ??? MedicationListViewModel.cs
?   ??? MedicationDetailViewModel.cs
??? Reminder/
?   ??? ReminderSetupViewModel.cs
?   ??? VoiceRecordingViewModel.cs
??? Dashboard/
    ??? HomeViewModel.cs
```

**MVVM Pattern Implementation**
- **Property Change Notification**: INotifyPropertyChanged
- **Commands**: ICommand for user actions
- **Navigation**: Shell navigation with parameters
- **Dependency Injection**: Constructor injection for services
- **Messaging**: WeakReferenceMessenger for cross-VM communication

---

### 2. Business Logic Layer (Services)

**Purpose**: Encapsulate business logic, external API calls, and cross-cutting concerns

#### Service Categories

**Authentication Services**
```csharp
Services/Auth/
??? IAuthenticationService.cs
??? AuthenticationService.cs          // Phone + OTP flow
??? IBiometricService.cs
??? BiometricService.cs                // Fingerprint/Face ID
??? IOtpService.cs
??? TwoFactorOtpService.cs             // 2Factor.in integration
```

**AI Services**
```csharp
Services/AI/
??? IPrescriptionReaderService.cs
??? OpenAIPrescriptionReader.cs        // GPT-4 Vision integration
??? IValidationAgentService.cs
??? MedicineValidationAgent.cs         // Cross-checks AI results
```

**Medication Services**
```csharp
Services/Medication/
??? IMedicationService.cs
??? MedicationService.cs               // CRUD operations
??? IReminderSchedulingService.cs
??? ReminderSchedulingService.cs       // Calculate reminder times
```

**Notification Services**
```csharp
Services/Notification/
??? INotificationService.cs
??? LocalNotificationService.cs        // Push notifications
```

**Audio Services**
```csharp
Services/Audio/
??? IAudioService.cs
??? AudioRecordingService.cs           // Record voice
??? AudioPlaybackService.cs            // Play reminders
```

**Storage Services**
```csharp
Services/Storage/
??? IFileStorageService.cs
??? FileStorageService.cs              // Images, audio files
??? ISecureStorageService.cs
??? SecureStorageService.cs            // API keys, tokens
```

---

### 3. Data Access Layer (Repositories)

**Purpose**: Abstract database operations and provide clean API for services

```csharp
Repositories/
??? IRepository<T>.cs                  // Generic repository interface
??? BaseRepository<T>.cs               // Generic implementation
??? IUserRepository.cs
??? UserRepository.cs
??? IMedicationRepository.cs
??? MedicationRepository.cs
??? IReminderRepository.cs
??? ReminderRepository.cs
??? IPrescriptionRepository.cs
??? PrescriptionRepository.cs
??? IVoiceRecordingRepository.cs
??? VoiceRecordingRepository.cs
```

**Repository Pattern Benefits**
- Centralized data access logic
- Easy to mock for unit testing
- Database-agnostic (can swap SQLite for NoSQL later)
- Transaction management
- Caching layer (future)

---

### 4. Data Layer (Models + Database)

#### Domain Models

```csharp
Models/
??? User.cs
??? Medication.cs
??? Reminder.cs
??? Prescription.cs
??? VoiceRecording.cs
??? DoseLog.cs
??? AppSettings.cs
```

#### Database Schema (SQLite)

**Users Table**
```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PhoneNumber TEXT NOT NULL UNIQUE,
    Name TEXT,
    DateOfBirth TEXT,
    Gender TEXT,
    ProfilePhotoPath TEXT,
    IsBiometricEnabled INTEGER DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

CREATE INDEX idx_users_phone ON Users(PhoneNumber);
```

**Medications Table**
```sql
CREATE TABLE Medications (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    PrescriptionId INTEGER,
    MedicineName TEXT NOT NULL,
    GenericName TEXT,
    Dosage TEXT NOT NULL,              -- "500mg", "10ml"
    Unit TEXT NOT NULL,                 -- "tablet", "capsule", "ml"
    Frequency TEXT NOT NULL,            -- "twice daily", "every 8 hours"
    TimesPerDay INTEGER NOT NULL,       -- 1, 2, 3, etc.
    DurationDays INTEGER,               -- 7, 14, 30, etc.
    StartDate TEXT NOT NULL,
    EndDate TEXT,
    Instructions TEXT,                  -- "Take after meals"
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(Id)
);

CREATE INDEX idx_medications_user ON Medications(UserId);
CREATE INDEX idx_medications_active ON Medications(IsActive);
```

**Reminders Table**
```sql
CREATE TABLE Reminders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MedicationId INTEGER NOT NULL,
    VoiceRecordingId INTEGER,
    ScheduledTime TEXT NOT NULL,        -- "08:00", "14:00", "20:00"
    DaysOfWeek TEXT,                    -- "1,2,3,4,5,6,7" (Mon-Sun)
    IsEnabled INTEGER DEFAULT 1,
    IsVoiceEnabled INTEGER DEFAULT 0,
    NotificationTitle TEXT,
    NotificationMessage TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    
    FOREIGN KEY (MedicationId) REFERENCES Medications(Id) ON DELETE CASCADE,
    FOREIGN KEY (VoiceRecordingId) REFERENCES VoiceRecordings(Id)
);

CREATE INDEX idx_reminders_medication ON Reminders(MedicationId);
CREATE INDEX idx_reminders_time ON Reminders(ScheduledTime);
```

**Prescriptions Table**
```sql
CREATE TABLE Prescriptions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    ImagePath TEXT NOT NULL,
    DoctorName TEXT,
    HospitalName TEXT,
    PrescriptionDate TEXT,
    RawAIResponse TEXT,                 -- JSON from OpenAI
    ValidationStatus TEXT,              -- "pending", "validated", "failed"
    ValidationNotes TEXT,
    IsProcessed INTEGER DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX idx_prescriptions_user ON Prescriptions(UserId);
CREATE INDEX idx_prescriptions_status ON Prescriptions(ValidationStatus);
```

**VoiceRecordings Table**
```sql
CREATE TABLE VoiceRecordings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    RecordingName TEXT NOT NULL,        -- "Mom", "Dad", "Self"
    FilePath TEXT NOT NULL,
    DurationSeconds INTEGER NOT NULL,
    FileSize INTEGER NOT NULL,          -- in bytes
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX idx_voice_user ON VoiceRecordings(UserId);
```

**DoseLogs Table** (Track medication adherence)
```sql
CREATE TABLE DoseLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    MedicationId INTEGER NOT NULL,
    ReminderId INTEGER,
    ScheduledDateTime TEXT NOT NULL,
    ActualDateTime TEXT,
    Status TEXT NOT NULL,               -- "taken", "missed", "skipped"
    Notes TEXT,
    CreatedAt TEXT NOT NULL,
    
    FOREIGN KEY (MedicationId) REFERENCES Medications(Id) ON DELETE CASCADE,
    FOREIGN KEY (ReminderId) REFERENCES Reminders(Id)
);

CREATE INDEX idx_doselogs_medication ON DoseLogs(MedicationId);
CREATE INDEX idx_doselogs_scheduled ON DoseLogs(ScheduledDateTime);
CREATE INDEX idx_doselogs_status ON DoseLogs(Status);
```

**AppSettings Table** (Key-value configuration)
```sql
CREATE TABLE AppSettings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);

-- Default settings
INSERT INTO AppSettings (Key, Value, UpdatedAt) VALUES
    ('ReminderSoundEnabled', 'true', datetime('now')),
    ('VibrationEnabled', 'true', datetime('now')),
    ('NotificationLeadTime', '0', datetime('now')),
    ('Language', 'en', datetime('now')),
    ('ThemeMode', 'system', datetime('now'));
```

---

## External Service Integration

### 1. OpenAI GPT-4 Vision API

**Purpose**: Read and extract structured data from prescription images

#### Integration Architecture

```csharp
public interface IPrescriptionReaderService
{
    Task<PrescriptionReadResult> ReadPrescriptionAsync(string imagePath);
    Task<ValidationResult> ValidateMedicationAsync(MedicationInfo medication);
}

public class OpenAIPrescriptionReader : IPrescriptionReaderService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    
    // Two-agent approach
    public async Task<PrescriptionReadResult> ReadPrescriptionAsync(string imagePath)
    {
        // Agent 1: Primary Reader
        var primaryResult = await CallGPT4Vision(imagePath, PrimaryPrompt);
        
        // Agent 2: Validation Agent
        var validationResult = await ValidateExtractedData(primaryResult);
        
        return new PrescriptionReadResult
        {
            Medications = primaryResult.Medications,
            DoctorInfo = primaryResult.DoctorInfo,
            ValidationScore = validationResult.ConfidenceScore,
            Warnings = validationResult.Warnings
        };
    }
}
```

#### Primary Agent Prompt
```
You are a medical prescription OCR expert. Analyze this prescription image and extract:

1. Patient Information:
   - Patient name
   - Age/Date of birth
   - Gender

2. Doctor Information:
   - Doctor name
   - Specialization
   - Hospital/Clinic name
   - Prescription date

3. Medications (for each):
   - Medicine name (brand and generic if available)
   - Dosage (e.g., "500mg", "10ml")
   - Form (tablet, capsule, syrup, injection)
   - Frequency (e.g., "twice daily", "every 8 hours")
   - Timing (e.g., "after meals", "before breakfast")
   - Duration (e.g., "7 days", "2 weeks", "1 month")
   - Special instructions

Return structured JSON. If you cannot read something clearly, mark confidence as "low" and include it in the warnings.

Response format:
{
  "patient": {...},
  "doctor": {...},
  "medications": [
    {
      "name": "string",
      "genericName": "string",
      "dosage": "string",
      "unit": "string",
      "frequency": "string",
      "timesPerDay": number,
      "timing": "string",
      "duration": "string",
      "durationDays": number,
      "instructions": "string",
      "confidence": "high|medium|low"
    }
  ],
  "warnings": ["string"]
}
```

#### Validation Agent Prompt
```
You are a pharmaceutical validation expert. Review this extracted prescription data:

{extracted_data}

Validate:
1. Medicine names are valid (not OCR errors)
2. Dosages are appropriate for the medicine
3. Frequencies make medical sense
4. No dangerous drug interactions (if multiple medicines)
5. Durations are reasonable

Flag any:
- Unusually high dosages
- Suspicious medicine names (likely OCR errors)
- Dangerous combinations
- Missing critical information

Return validation result:
{
  "isValid": boolean,
  "confidenceScore": 0-100,
  "validatedMedications": [...],
  "warnings": [...],
  "criticalIssues": [...]
}
```

#### Error Handling Strategy

```csharp
public class ResilientPrescriptionReader : IPrescriptionReaderService
{
    private readonly IOpenAIPrescriptionReader _primaryReader;
    private readonly ICacheService _cache;
    private readonly ILogger _logger;
    
    public async Task<PrescriptionReadResult> ReadPrescriptionAsync(string imagePath)
    {
        try
        {
            // 1. Check cache first (avoid re-processing same image)
            var cacheKey = GetImageHash(imagePath);
            if (_cache.TryGet(cacheKey, out PrescriptionReadResult cached))
                return cached;
            
            // 2. Retry logic with exponential backoff
            var result = await Polly.Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"OpenAI API retry {retryCount} after {timeSpan}");
                    })
                .ExecuteAsync(() => _primaryReader.ReadPrescriptionAsync(imagePath));
            
            // 3. Cache result
            _cache.Set(cacheKey, result, TimeSpan.FromHours(24));
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read prescription");
            
            // Fallback: Allow manual entry
            return new PrescriptionReadResult
            {
                Success = false,
                ErrorMessage = "Unable to read prescription automatically. Please enter manually.",
                FallbackToManualEntry = true
            };
        }
    }
}
```

#### Cost Optimization

```csharp
public class CostOptimizedPrescriptionReader : IPrescriptionReaderService
{
    // Strategies:
    // 1. Image preprocessing (reduce size, enhance quality)
    // 2. Caching (don't re-process same prescription)
    // 3. Rate limiting (prevent abuse)
    // 4. Batch processing (future: process multiple prescriptions in one call)
    
    private async Task<byte[]> OptimizeImageAsync(string imagePath)
    {
        // Resize to max 1024x1024 (GPT-4V optimal size)
        // Compress to JPEG 85% quality
        // Enhance contrast for better OCR
        // Convert to base64
        
        // Estimated savings: 50-70% of original image size
        // Impact: $0.01 ? $0.003 per image
    }
}
```

---

### 2. 2Factor.in SMS OTP Service

**Purpose**: Send OTP for phone number verification

#### Integration Architecture

```csharp
public interface IOtpService
{
    Task<OtpSendResult> SendOtpAsync(string phoneNumber);
    Task<bool> VerifyOtpAsync(string sessionId, string otp);
}

public class TwoFactorOtpService : IOtpService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _senderId = "MEDRMD";
    
    public async Task<OtpSendResult> SendOtpAsync(string phoneNumber)
    {
        var url = $"https://2factor.in/API/V1/{_apiKey}/SMS/{phoneNumber}/AUTOGEN/{_senderId}";
        
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TwoFactorResponse>(content);
        
        return new OtpSendResult
        {
            Success = result.Status == "Success",
            SessionId = result.Details,
            Message = result.Status
        };
    }
    
    public async Task<bool> VerifyOtpAsync(string sessionId, string otp)
    {
        var url = $"https://2factor.in/API/V1/{_apiKey}/SMS/VERIFY/{sessionId}/{otp}";
        
        var response = await _httpClient.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TwoFactorResponse>(content);
        
        return result.Status == "Success";
    }
}
```

#### Cost Calculation

```
Cost per SMS: ?0.10 - ?0.15 (depends on volume)

Scenarios:
- 100 users/month: ?960 ($12) - 6 OTPs per user (login attempts)
- 1,000 users/month: ?8,320 ($100)
- 10,000 users/month: ?83,200 ($1,000)

Optimization:
- Increase OTP expiry to 10 minutes (reduce re-sends)
- Rate limit: Max 3 OTP requests per phone per hour
- Block disposable phone numbers
- Implement CAPTCHA to prevent bot abuse
```

---

### 3. Local Notification Service

**Purpose**: Schedule and deliver medication reminders

#### Integration Architecture

```csharp
public interface INotificationService
{
    Task ScheduleNotificationAsync(Reminder reminder);
    Task CancelNotificationAsync(int reminderId);
    Task UpdateNotificationAsync(Reminder reminder);
    Task PlayVoiceReminderAsync(int voiceRecordingId);
}

public class LocalNotificationService : INotificationService
{
    private readonly INotificationManager _notificationManager;
    private readonly IAudioPlaybackService _audioService;
    
    public async Task ScheduleNotificationAsync(Reminder reminder)
    {
        var notification = new NotificationRequest
        {
            NotificationId = reminder.Id,
            Title = reminder.NotificationTitle ?? "Medication Reminder",
            Description = reminder.NotificationMessage ?? $"Time to take {reminder.Medication.MedicineName}",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Today.Add(TimeSpan.Parse(reminder.ScheduledTime)),
                RepeatType = NotificationRepeat.Daily
            },
            Android = new AndroidOptions
            {
                ChannelId = "medication_reminders",
                Priority = NotificationPriority.High,
                VibrationPattern = new[] { 0, 500, 200, 500 },
                Sound = "reminder_sound.mp3"
            },
            iOS = new iOSOptions
            {
                Sound = "reminder_sound.aiff",
                Badge = 1
            }
        };
        
        await _notificationManager.Show(notification);
        
        // If voice reminder enabled, schedule voice playback
        if (reminder.IsVoiceEnabled && reminder.VoiceRecordingId.HasValue)
        {
            await ScheduleVoicePlaybackAsync(reminder);
        }
    }
    
    private async Task ScheduleVoicePlaybackAsync(Reminder reminder)
    {
        // Use platform-specific APIs to play audio when notification fires
        // iOS: UNNotificationSound with custom audio file
        // Android: Custom notification action to trigger audio playback
    }
}
```

---

## Security Architecture

### 1. Data Encryption

**At Rest**
```csharp
public class SecureStorageService : ISecureStorageService
{
    // Use platform-specific secure storage
    // iOS: Keychain
    // Android: EncryptedSharedPreferences
    
    public async Task SetAsync(string key, string value)
    {
        await SecureStorage.SetAsync(key, value);
    }
    
    public async Task<string> GetAsync(string key)
    {
        return await SecureStorage.GetAsync(key);
    }
}

// For SQLite encryption (future)
public class EncryptedDatabaseService
{
    // Use SQLCipher for encrypted database
    // Add password from biometric or user PIN
}
```

**In Transit**
- All external API calls use HTTPS
- Certificate pinning for OpenAI and 2Factor.in APIs (future)

### 2. Authentication Flow

```
User Journey: Login with Phone + OTP

1. User enters phone number
   ?
2. App validates format (10 digits, valid country code)
   ?
3. App calls 2Factor.in API to send OTP
   ?
4. User receives SMS with 6-digit OTP
   ?
5. User enters OTP in app
   ?
6. App verifies OTP with 2Factor.in
   ?
7. If valid:
   - Generate local session token (JWT)
   - Store in secure storage
   - Mark user as authenticated
   ?
8. On subsequent app opens:
   - Check for valid session token
   - If biometric enabled ? Show biometric prompt
   - If valid ? Auto-login
```

### 3. Biometric Authentication

```csharp
public class BiometricService : IBiometricService
{
    private readonly IFingerprint _fingerprint;
    
    public async Task<bool> AuthenticateAsync()
    {
        var availability = await _fingerprint.GetAvailabilityAsync();
        
        if (availability != FingerprintAvailability.Available)
            return false;
        
        var request = new AuthenticationRequestConfiguration(
            "Verify your identity",
            "Use your fingerprint or face to unlock MedRemind"
        );
        
        var result = await _fingerprint.AuthenticateAsync(request);
        
        return result.Authenticated;
    }
}
```

---

## Performance Optimization

### 1. Database Optimization

```csharp
// Indexing strategy (already in schema above)
CREATE INDEX idx_medications_user ON Medications(UserId);
CREATE INDEX idx_medications_active ON Medications(IsActive);
CREATE INDEX idx_reminders_medication ON Reminders(MedicationId);
CREATE INDEX idx_reminders_time ON Reminders(ScheduledTime);

// Query optimization
public async Task<List<Medication>> GetActiveMedicationsAsync(int userId)
{
    return await _database.Table<Medication>()
        .Where(m => m.UserId == userId && m.IsActive == 1)
        .OrderBy(m => m.MedicineName)
        .ToListAsync();
}

// Pagination for large datasets (future)
public async Task<PagedResult<DoseLog>> GetDoseLogsPagedAsync(int userId, int page, int pageSize)
{
    var skip = (page - 1) * pageSize;
    
    var logs = await _database.Table<DoseLog>()
        .Where(dl => dl.Medication.UserId == userId)
        .OrderByDescending(dl => dl.ScheduledDateTime)
        .Skip(skip)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<DoseLog>(logs, page, pageSize);
}
```

### 2. Image Optimization

```csharp
public class ImageOptimizationService
{
    public async Task<string> OptimizeAndSaveAsync(Stream imageStream)
    {
        // 1. Load image
        var image = await Image.LoadAsync(imageStream);
        
        // 2. Resize if too large (max 1024x1024 for GPT-4V)
        if (image.Width > 1024 || image.Height > 1024)
        {
            var ratio = Math.Min(1024.0 / image.Width, 1024.0 / image.Height);
            image.Mutate(x => x.Resize(
                (int)(image.Width * ratio),
                (int)(image.Height * ratio)
            ));
        }
        
        // 3. Compress (JPEG 85% quality)
        var encoder = new JpegEncoder { Quality = 85 };
        
        // 4. Save to app data folder
        var fileName = $"{Guid.NewGuid()}.jpg";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, "prescriptions", fileName);
        
        await image.SaveAsync(filePath, encoder);
        
        return filePath;
    }
}
```

### 3. Memory Management

```csharp
// Dispose pattern for ViewModels
public class BaseViewModel : INotifyPropertyChanged, IDisposable
{
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from events
            // Dispose services
            // Clear collections
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

// Weak event pattern for messaging
WeakReferenceMessenger.Default.Send(new MedicationAddedMessage(medication));
```

---

## Testing Strategy

### 1. Unit Testing (80% Coverage Target)

```csharp
// Example: Test MedicationService
[TestClass]
public class MedicationServiceTests
{
    private Mock<IMedicationRepository> _mockRepo;
    private MedicationService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _mockRepo = new Mock<IMedicationRepository>();
        _service = new MedicationService(_mockRepo.Object);
    }
    
    [TestMethod]
    public async Task AddMedication_ValidData_ReturnsMedication()
    {
        // Arrange
        var medication = new Medication { /* ... */ };
        _mockRepo.Setup(r => r.InsertAsync(It.IsAny<Medication>()))
                 .ReturnsAsync(medication);
        
        // Act
        var result = await _service.AddMedicationAsync(medication);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(medication.MedicineName, result.MedicineName);
        _mockRepo.Verify(r => r.InsertAsync(It.IsAny<Medication>()), Times.Once);
    }
}
```

### 2. Integration Testing

```csharp
// Test OpenAI integration with real API (in CI/CD)
[TestClass]
[TestCategory("Integration")]
public class OpenAIPrescriptionReaderTests
{
    [TestMethod]
    public async Task ReadPrescription_RealImage_ReturnsValidData()
    {
        var reader = new OpenAIPrescriptionReader(apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        var imagePath = "test_prescription_1.jpg";
        
        var result = await reader.ReadPrescriptionAsync(imagePath);
        
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.Medications.Count > 0);
        Assert.IsTrue(result.ValidationScore > 80);
    }
}
```

### 3. UI Testing (Manual + Automated)

```csharp
// MAUI UI Test (using Appium)
[TestClass]
public class LoginPageTests : BaseUITest
{
    [TestMethod]
    public void Login_ValidPhoneNumber_NavigatesToOtpPage()
    {
        // Arrange
        var phoneInput = Driver.FindElement(By.Id("PhoneNumberEntry"));
        var sendOtpButton = Driver.FindElement(By.Id("SendOtpButton"));
        
        // Act
        phoneInput.SendKeys("9876543210");
        sendOtpButton.Click();
        
        // Assert
        Assert.IsTrue(Driver.FindElement(By.Id("OtpEntry")).Displayed);
    }
}
```

---

## Deployment Architecture

### 1. Development Environment

```yaml
Development:
  IDE: Visual Studio 2022 (v17.8+)
  SDK: .NET 8.0
  Emulators:
    - Android: Pixel 7 API 34
    - iOS: iPhone 15 iOS 17.2
  Database: SQLite Browser
  API Testing: Postman
```

### 2. CI/CD Pipeline (GitHub Actions)

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: macos-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Install MAUI workloads
      run: dotnet workload install maui
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build Android
      run: dotnet build -f net8.0-android -c Release
    
    - name: Build iOS
      run: dotnet build -f net8.0-ios -c Release
    
    - name: Run Unit Tests
      run: dotnet test --no-build --verbosity normal
    
    - name: Upload Android APK
      uses: actions/upload-artifact@v3
      with:
        name: android-apk
        path: bin/Release/net8.0-android/*.apk
```

### 3. App Store Deployment

**Android (Google Play Store)**
```
1. Create release build (signed APK/AAB)
2. Generate screenshots (5.5" and 6.5" devices)
3. Prepare store listing:
   - Title: "MedRemind - Medication Reminders"
   - Short description: "Never miss your medication with AI-powered reminders"
   - Full description: [See marketing copy]
   - Screenshots: 8 images (mandatory)
   - Icon: 512x512 PNG
4. Fill out Content rating questionnaire
5. Set pricing: Free
6. Submit for review (typically 2-3 days)
```

**iOS (Apple App Store)**
```
1. Create app in App Store Connect
2. Generate release build (IPA with distribution certificate)
3. Upload via Xcode or Transporter
4. Prepare store listing:
   - Screenshots: 6.5" and 5.5" devices
   - Preview videos (optional but recommended)
   - App Privacy details (required)
   - Age rating
5. Submit for review (typically 24-48 hours)
```

---

## Migration Strategy (SQLite ? NoSQL)

**Phase 1: Current (MVP)**
- SQLite for simplicity and offline-first
- No backend server required

**Phase 2: Hybrid Approach (3-6 months)**
```csharp
public interface IDataService
{
    Task SyncAsync();  // Sync local SQLite ? Cloud
}

public class HybridDataService : IDataService
{
    private readonly ISQLiteService _local;
    private readonly ICosmosDbService _cloud;  // Azure Cosmos DB
    
    public async Task SyncAsync()
    {
        // 1. Get changes since last sync
        var localChanges = await _local.GetChangesSinceAsync(lastSyncTime);
        
        // 2. Upload to cloud
        await _cloud.UpsertAsync(localChanges);
        
        // 3. Download cloud changes
        var cloudChanges = await _cloud.GetChangesSinceAsync(lastSyncTime);
        
        // 4. Merge into local (conflict resolution)
        await _local.MergeAsync(cloudChanges);
    }
}
```

**Phase 3: Full Cloud (12+ months)**
- Move to Azure Cosmos DB or MongoDB
- Real-time sync across devices
- Family account sharing
- Backend API for advanced features

---

## Scalability Considerations

### Current Capacity (MVP)
- **Users**: Up to 10,000
- **Database Size**: ~50 MB per user (average)
- **API Calls**: 10,000 prescriptions/month
- **Cost**: $17-120/month

### Scale to 100K Users
1. **Backend API**: Introduce .NET 8 Web API for heavy operations
2. **Caching**: Redis for frequently accessed data
3. **CDN**: CloudFlare for prescription images
4. **Database**: Azure Cosmos DB with partitioning
5. **Cost**: ~$2,000-5,000/month

### Scale to 1M Users
1. **Microservices**: Separate services for auth, prescriptions, notifications
2. **Message Queue**: Azure Service Bus for async operations
3. **Load Balancer**: Azure Front Door
4. **AI Optimization**: Self-hosted OCR model (reduce OpenAI costs)
5. **Cost**: ~$15,000-25,000/month

---

## Monitoring and Analytics

### Application Insights (Future)

```csharp
public class TelemetryService
{
    private readonly TelemetryClient _telemetry;
    
    public void TrackEvent(string eventName, Dictionary<string, string> properties = null)
    {
        _telemetry.TrackEvent(eventName, properties);
    }
    
    public void TrackException(Exception exception)
    {
        _telemetry.TrackException(exception);
    }
    
    // Key metrics to track:
    // - User registrations
    // - Prescriptions uploaded
    // - AI reading success rate
    // - Reminder adherence rate
    // - App crashes
    // - API response times
}
```

---

## Documentation and Code Standards

### Code Style
- **Naming**: PascalCase for public members, camelCase for private
- **Comments**: XML documentation for public APIs
- **Async**: All I/O operations must be async
- **SOLID**: Follow SOLID principles
- **DRY**: Don't Repeat Yourself

### Git Workflow
```
main (protected)
  ??? develop (integration branch)
       ??? feature/auth-phone-login
       ??? feature/prescription-upload
       ??? feature/ai-reader
       ??? feature/reminders
```

---

## Summary

This architecture provides:
1. ? **Offline-first**: Works without internet (except AI reading)
2. ? **Scalable**: Easy migration to cloud when needed
3. ? **Testable**: Clear separation of concerns, mockable services
4. ? **Maintainable**: MVVM pattern, dependency injection
5. ? **Secure**: Biometric auth, encrypted storage, HTTPS
6. ? **Cost-effective**: Minimal infrastructure for MVP
7. ? **Extensible**: Easy to add new features

**Next Steps**:
1. Review and approve architecture
2. Set up project structure
3. Configure dependency injection
4. Create database migrations
5. Start Sprint 1 development

---

**Document Version**: 1.0  
**Last Updated**: December 18, 2024  
**Owner**: Rajib Mahata  
**Status**: Draft for Review
