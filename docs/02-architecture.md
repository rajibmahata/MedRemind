# 🏗️ MedRemind - System Architecture

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [System Architecture Diagram](#system-architecture-diagram)
3. [Application Layers](#application-layers)
4. [Technology Stack](#technology-stack)
5. [Design Patterns](#design-patterns)
6. [Component Architecture](#component-architecture)
7. [Data Flow](#data-flow)
8. [Security Architecture](#security-architecture)
9. [Scalability Considerations](#scalability-considerations)

---

## Architecture Overview

MedRemind follows a **clean architecture** approach with clear separation of concerns across multiple layers. The application is built using **.NET MAUI** for cross-platform mobile development with **MVVM (Model-View-ViewModel)** pattern.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│              (XAML Views + Code-Behind)                      │
├─────────────────────────────────────────────────────────────┤
│                     ViewModel Layer                          │
│           (Business Logic + State Management)                │
├─────────────────────────────────────────────────────────────┤
│                     Service Layer                            │
│        (Business Services + External Integrations)           │
├─────────────────────────────────────────────────────────────┤
│                     Repository Layer                         │
│              (Data Access Abstraction)                       │
├─────────────────────────────────────────────────────────────┤
│                     Data Layer                               │
│                  (SQLite Database)                           │
└─────────────────────────────────────────────────────────────┘
```

---

## System Architecture Diagram

### Complete System Architecture

```
┌───────────────────────────────────────────────────────────────────┐
│                         Mobile Device                              │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                    MedRemind App                             │ │
│  │  ┌───────────────────────────────────────────────────────┐  │ │
│  │  │              Presentation Layer                        │  │ │
│  │  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │  │ │
│  │  │  │Login Page│ │Camera    │ │Reminder  │ │Dashboard │  │  │ │
│  │  │  │          │ │Page      │ │Page      │ │          │  │  │ │
│  │  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │  │ │
│  │  └───────────────────────────────────────────────────────┘  │ │
│  │                            ↕                                  │ │
│  │  ┌───────────────────────────────────────────────────────┐  │ │
│  │  │              ViewModel Layer                           │  │ │
│  │  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │  │ │
│  │  │  │Login VM  │ │Upload VM │ │Reminder  │ │Dashboard │  │  │ │
│  │  │  │          │ │          │ │VM        │ │VM        │  │  │ │
│  │  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │  │ │
│  │  └───────────────────────────────────────────────────────┘  │ │
│  │                            ↕                                  │ │
│  │  ┌───────────────────────────────────────────────────────┐  │ │
│  │  │              Service Layer                             │  │ │
│  │  │  ┌────────────┐ ┌──────────┐ ┌─────────┐ ┌─────────┐  │  │ │
│  │  │  │Auth Service│ │AI Service│ │Reminder │ │Voice    │  │  │ │
│  │  │  │            │ │          │ │Service  │ │Service  │  │  │ │
│  │  │  └────────────┘ └──────────┘ └─────────┘ └─────────┘  │  │ │
│  │  └───────────────────────────────────────────────────────┘  │ │
│  │                            ↕                                  │ │
│  │  ┌───────────────────────────────────────────────────────┐  │ │
│  │  │              Repository Layer                          │  │ │
│  │  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │  │ │
│  │  │  │User Repo │ │Rx Repo   │ │Med Repo  │ │Voice Repo│  │  │ │
│  │  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │  │ │
│  │  └───────────────────────────────────────────────────────┘  │ │
│  │                            ↕                                  │ │
│  │  ┌───────────────────────────────────────────────────────┐  │ │
│  │  │              SQLite Database                           │  │ │
│  │  │  [Users] [Prescriptions] [Medications] [Reminders]    │  │ │
│  │  └───────────────────────────────────────────────────────┘  │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │              Platform-Specific Services                      │ │
│  │  [Camera] [Notifications] [Biometric] [FileSystem] [Audio]  │ │
│  └─────────────────────────────────────────────────────────────┘ │
└───────────────────────────────────────────────────────────────────┘
                              ↕ HTTPS
┌───────────────────────────────────────────────────────────────────┐
│                     External Services                              │
├───────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   OpenAI     │  │   Twilio     │  │Azure Computer│           │
│  │  Vision API  │  │   SMS API    │  │   Vision     │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└───────────────────────────────────────────────────────────────────┘
```

---

## Application Layers

### 1. Presentation Layer
**Responsibility:** User interface and user interaction

**Components:**
- XAML Views (Pages, Controls, Templates)
- Code-behind files
- Value Converters
- Behaviors
- Custom Controls

**Technology:**
- .NET MAUI XAML
- MAUI Community Toolkit
- Custom renderers (if needed)

**Key Files:**
```
Views/
├── Auth/
│   ├── LoginPage.xaml
│   ├── OTPVerificationPage.xaml
│   └── BiometricSetupPage.xaml
├── Prescription/
│   ├── UploadPage.xaml
│   ├── CameraPage.xaml
│   └── VerificationPage.xaml
├── Medication/
│   ├── ListPage.xaml
│   ├── DetailPage.xaml
│   └── EditPage.xaml
├── Reminder/
│   ├── SetupPage.xaml
│   └── VoiceRecordingPage.xaml
└── Dashboard/
    └── HomePage.xaml
```

---

### 2. ViewModel Layer
**Responsibility:** Presentation logic and state management

**Components:**
- ViewModels (Observable objects)
- Commands (RelayCommand)
- Property change notification
- Navigation logic
- Validation logic

**Technology:**
- CommunityToolkit.Mvvm
- INotifyPropertyChanged
- ObservableCollections

**Key Files:**
```
ViewModels/
├── Auth/
│   ├── LoginViewModel.cs
│   ├── OTPVerificationViewModel.cs
│   └── ProfileViewModel.cs
├── Prescription/
│   ├── UploadViewModel.cs
│   └── VerificationViewModel.cs
├── Medication/
│   ├── MedicationListViewModel.cs
│   └── MedicationDetailViewModel.cs
├── Reminder/
│   ├── ReminderSetupViewModel.cs
│   └── VoiceRecordingViewModel.cs
└── Dashboard/
    └── DashboardViewModel.cs
```

---

### 3. Service Layer
**Responsibility:** Business logic and external integrations

**Components:**
- Authentication services
- AI processing services
- Notification services
- Voice recording services
- Image processing services

**Technology:**
- HttpClient for API calls
- Dependency Injection
- Async/await patterns

**Key Services:**

```
Services/
├── Authentication/
│   ├── IAuthenticationService.cs
│   ├── AuthenticationService.cs
│   ├── IOTPService.cs
│   ├── OTPService.cs
│   ├── IBiometricService.cs
│   └── BiometricService.cs
├── Prescription/
│   ├── IAIProcessingService.cs
│   ├── AIProcessingService.cs
│   ├── IImageProcessingService.cs
│   └── ImageProcessingService.cs
├── Medication/
│   ├── IMedicationService.cs
│   └── MedicationService.cs
├── Reminder/
│   ├── IReminderService.cs
│   ├── ReminderService.cs
│   ├── INotificationService.cs
│   └── NotificationService.cs
├── Voice/
│   ├── IVoiceRecordingService.cs
│   └── VoiceRecordingService.cs
└── Common/
    ├── INavigationService.cs
    ├── NavigationService.cs
    ├── ISecureStorageService.cs
    └── SecureStorageService.cs
```

---

### 4. Repository Layer
**Responsibility:** Data access abstraction

**Components:**
- Repository interfaces
- Repository implementations
- CRUD operations
- Query methods

**Technology:**
- SQLite-net-pcl
- Entity Framework Core (optional)

**Key Repositories:**

```
Repositories/
├── IUserRepository.cs
├── UserRepository.cs
├── IPrescriptionRepository.cs
├── PrescriptionRepository.cs
├── IMedicationRepository.cs
├── MedicationRepository.cs
├── IReminderRepository.cs
├── ReminderRepository.cs
├── IVoiceRecordingRepository.cs
├── VoiceRecordingRepository.cs
└── DatabaseContext.cs
```

---

### 5. Data Layer
**Responsibility:** Data persistence

**Components:**
- SQLite database
- Entity models
- Database context
- Migration scripts

**Technology:**
- SQLite
- SQLite-net-pcl

**Database Tables:**
- Users
- Prescriptions
- Medications
- Reminders
- VoiceRecordings
- ReminderLogs
- OTPVerification

---

## Technology Stack

### Frontend Framework
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET MAUI | 8.0 | Cross-platform UI framework |
| C# | 12 | Programming language |
| XAML | - | UI markup |

### UI Components
| Library | Version | Purpose |
|---------|---------|---------|
| CommunityToolkit.Maui | Latest | Enhanced UI controls |
| CommunityToolkit.Mvvm | Latest | MVVM helpers |

### Platform Services
| Plugin | Purpose |
|--------|---------|
| Plugin.Maui.Camera | Camera access |
| Plugin.Maui.Audio | Audio recording/playback |
| Plugin.Fingerprint | Biometric authentication |
| Plugin.LocalNotification | Local notifications |

### Data & Storage
| Technology | Purpose |
|------------|---------|
| SQLite-net-pcl | Database access |
| SQLCipher | Database encryption |
| SecureStorage | Credential storage |

### External APIs
| Service | Purpose |
|---------|---------|
| OpenAI GPT-4 Vision | Prescription reading |
| Twilio | SMS OTP |
| Azure Computer Vision | OCR fallback |

---

## Design Patterns

### 1. MVVM (Model-View-ViewModel)
**Purpose:** Separation of UI from business logic

**Implementation:**
```csharp
// View (XAML)
<ContentPage x:DataType="vm:LoginViewModel">
    <Entry Text="{Binding PhoneNumber}" />
    <Button Command="{Binding LoginCommand}" />
</ContentPage>

// ViewModel
public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string phoneNumber;
    
    [RelayCommand]
    private async Task LoginAsync()
    {
        // Business logic
    }
}
```

---

### 2. Repository Pattern
**Purpose:** Abstract data access

**Implementation:**
```csharp
public interface IUserRepository
{
    Task<User> GetUserByIdAsync(string userId);
    Task<int> CreateUserAsync(User user);
}

public class UserRepository : IUserRepository
{
    private readonly SQLiteAsyncConnection _db;
    
    public async Task<User> GetUserByIdAsync(string userId)
    {
        return await _db.Table<User>()
            .Where(u => u.UserId == userId)
            .FirstOrDefaultAsync();
    }
}
```

---

### 3. Dependency Injection
**Purpose:** Loose coupling and testability

**Implementation:**
```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddTransient<LoginViewModel>();

// ViewModel Constructor
public LoginViewModel(IAuthenticationService authService)
{
    _authService = authService;
}
```

---

### 4. Factory Pattern
**Purpose:** Object creation

**Implementation:**
```csharp
public interface IViewModelFactory
{
    T Create<T>() where T : BaseViewModel;
}

public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;
    
    public T Create<T>() where T : BaseViewModel
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}
```

---

### 5. Observer Pattern
**Purpose:** Event-driven communication

**Implementation:**
```csharp
public class MessagingService
{
    public void Subscribe<T>(Action<T> callback)
    {
        WeakReferenceMessenger.Default.Register<T>(this, (r, m) => callback(m));
    }
    
    public void Publish<T>(T message)
    {
        WeakReferenceMessenger.Default.Send(message);
    }
}
```

---

## Component Architecture

### Authentication Component

```
┌────────────────────────────────────────┐
│      Authentication Module             │
├────────────────────────────────────────┤
│  ┌──────────────────────────────────┐  │
│  │  Phone Authentication            │  │
│  │  - OTP Generation                │  │
│  │  - OTP Verification              │  │
│  │  - Session Management            │  │
│  └──────────────────────────────────┘  │
│  ┌──────────────────────────────────┐  │
│  │  Biometric Authentication        │  │
│  │  - Fingerprint                   │  │
│  │  - Face ID                       │  │
│  │  - Device Verification           │  │
│  └──────────────────────────────────┘  │
│  ┌──────────────────────────────────┐  │
│  │  Token Management                │  │
│  │  - JWT Generation                │  │
│  │  - Token Validation              │  │
│  │  - Refresh Tokens                │  │
│  └──────────────────────────────────┘  │
└────────────────────────────────────────┘
```

---

### Prescription Processing Component

```
┌────────────────────────────────────────┐
│    Prescription Processing Module      │
├────────────────────────────────────────┤
│  ┌──────────────────────────────────┐  │
│  │  Image Capture                   │  │
│  │  - Camera Integration            │  │
│  │  - Gallery Selection             │  │
│  │  - Image Validation              │  │
│  └──────────────────────────────────┘  │
│                ↓                        │
│  ┌──────────────────────────────────┐  │
│  │  Image Processing                │  │
│  │  - Quality Check                 │  │
│  │  - Brightness/Contrast           │  │
│  │  - Image Enhancement             │  │
│  └──────────────────────────────────┘  │
│                ↓                        │
│  ┌──────────────────────────────────┐  │
│  │  AI Processing                   │  │
│  │  - OpenAI Vision API             │  │
│  │  - OCR Extraction                │  │
│  │  - Data Parsing                  │  │
│  └──────────────────────────────────┘  │
│                ↓                        │
│  ┌──────────────────────────────────┐  │
│  │  Validation & Verification       │  │
│  │  - Medicine Name Validation      │  │
│  │  - User Confirmation             │  │
│  │  - Error Handling                │  │
│  └──────────────────────────────────┘  │
└────────────────────────────────────────┘
```

---

### Reminder System Component

```
┌────────────────────────────────────────┐
│       Reminder System Module           │
├────────────────────────────────────────┤
│  ┌──────────────────────────────────┐  │
│  │  Reminder Scheduler              │  │
│  │  - Time Calculation              │  │
│  │  - Frequency Management          │  │
│  │  - Schedule Storage              │  │
│  └──────────────────────────────────┘  │
│                ↓                        │
│  ┌──────────────────────────────────┐  │
│  │  Notification Service            │  │
│  │  - Local Notifications           │  │
│  │  - Background Tasks              │  │
│  │  - Notification Actions          │  │
│  └──────────────────────────────────┘  │
│                ↓                        │
│  ┌──────────────────────────────────┐  │
│  │  Voice Playback                  │  │
│  │  - Audio File Access             │  │
│  │  - Playback Control              │  │
│  │  - Volume Management             │  │
│  └──────────────────────────────────┘  │
│                ↓                        │
│  ┌──────────────────────────────────┐  │
│  │  Adherence Tracking              │  │
│  │  - Log Recording                 │  │
│  │  - Status Tracking               │  │
│  │  - Analytics                     │  │
│  └──────────────────────────────────┘  │
└────────────────────────────────────────┘
```

---

## Data Flow

### User Registration Flow

```
User Action → View → ViewModel → Service → Repository → Database
                                    ↓
                                External API (Twilio)
                                    ↓
                                SMS Sent
```

**Detailed Steps:**
1. User enters phone number
2. LoginPage binds to LoginViewModel.PhoneNumber
3. User clicks "Send OTP" button
4. LoginCommand executes in LoginViewModel
5. LoginViewModel calls AuthenticationService.LoginWithPhoneNumberAsync()
6. AuthenticationService calls OTPService.GenerateAndSendOTPAsync()
7. OTPService generates random OTP
8. OTPRepository saves OTP to database
9. OTPService calls Twilio API
10. Twilio sends SMS to user
11. Result propagates back to UI

---

### Prescription Upload & Processing Flow

```
Camera → Image → ImageProcessingService → AIProcessingService → OpenAI API
                                                ↓
                            PrescriptionData ← JSON Response
                                                ↓
                            Verification Screen (User confirms)
                                                ↓
                            PrescriptionRepository.SaveAsync()
                                                ↓
                            Create Medications & Reminders
```

**Detailed Steps:**
1. User clicks "Upload Prescription"
2. Camera opens (Plugin.Maui.Camera)
3. User captures photo
4. Image saved to local storage
5. ImageProcessingService validates quality
6. If valid, AIProcessingService processes image
7. Image converted to base64
8. Request sent to OpenAI Vision API
9. API returns JSON with extracted data
10. Data parsed into PrescriptionData model
11. Verification screen displays data
12. User reviews and edits if needed
13. User clicks "Confirm"
14. Prescription saved to database
15. Medications created from prescription
16. Navigate to Reminder setup

---

### Notification Trigger Flow

```
Scheduled Time → OS Notification System → Local Notification
                                              ↓
                        User Taps Notification
                                              ↓
                        App Opens → Notification Handler
                                              ↓
                        Voice Recording Plays
                                              ↓
                        Log Reminder (Acknowledged/Missed)
```

---

## Security Architecture

### Security Layers

```
┌────────────────────────────────────────┐
│         Application Security           │
├────────────────────────────────────────┤
│  1. Input Validation                   │
│  2. Authentication & Authorization     │
│  3. Secure Storage (Credentials)       │
│  4. Data Encryption (SQLite)           │
└────────────────────────────────────────┘
            ↓
┌────────────────────────────────────────┐
│       Transport Security (HTTPS)       │
├────────────────────────────────────────┤
│  - TLS 1.3                             │
│  - Certificate Pinning                 │
│  - API Key Protection                  │
└────────────────────────────────────────┘
            ↓
┌────────────────────────────────────────┐
│         Platform Security              │
├────────────────────────────────────────┤
│  - iOS Keychain                        │
│  - Android Keystore                    │
│  - Biometric APIs                      │
└────────────────────────────────────────┘
```

### Authentication Flow

```
┌─────────┐
│  User   │
└────┬────┘
     │ 1. Enter Phone
     ↓
┌─────────────┐
│   Login     │
│   Screen    │
└────┬────────┘
     │ 2. Request OTP
     ↓
┌──────────────┐      ┌──────────────┐
│ Auth Service │─────→│ OTP Service  │
└──────────────┘      └──────┬───────┘
                             │ 3. Generate OTP
                             ↓
                      ┌──────────────┐
                      │   Database   │
                      └──────┬───────┘
                             │ 4. Save OTP
                             ↓
                      ┌──────────────┐
                      │ Twilio API   │
                      └──────┬───────┘
                             │ 5. Send SMS
                             ↓
                      ┌──────────────┐
                      │     User     │
                      └──────┬───────┘
                             │ 6. Enter OTP
                             ↓
                      ┌──────────────┐
                      │ Verify OTP   │
                      └──────┬───────┘
                             │ 7. If valid
                             ↓
                      ┌──────────────┐
                      │Generate JWT  │
                      └──────┬───────┘
                             │ 8. Store token
                             ↓
                      ┌──────────────┐
                      │SecureStorage │
                      └──────────────┘
```

---

## Scalability Considerations

### Current Architecture (MVP)
- Single device, local storage
- No cloud synchronization
- Direct API calls from mobile app

### Future Scalability Path

#### Phase 1: Backend API Introduction
```
Mobile App → Backend API → Database (Cloud)
                ↓
          External Services (OpenAI, Twilio)
```

**Benefits:**
- Centralized business logic
- Better API key management
- Usage analytics
- Cost optimization

#### Phase 2: Multi-Device Support
```
Device 1 ──┐
           ├──→ Backend API → Cloud Database
Device 2 ──┘
```

**Features:**
- Cloud sync
- Real-time updates
- Cross-device notifications

#### Phase 3: Microservices Architecture
```
Mobile App → API Gateway → ┌→ Auth Service
                           ├→ Prescription Service
                           ├→ Reminder Service
                           ├→ Notification Service
                           └→ Analytics Service
```

**Benefits:**
- Independent scaling
- Better fault isolation
- Technology flexibility

---

## Performance Optimization

### Image Processing
- Compress images before API calls
- Cache processed results
- Background processing

### Database
- Indexed queries
- Connection pooling
- Lazy loading

### API Calls
- Retry logic with exponential backoff
- Request caching
- Batch operations

### UI Rendering
- Virtual scrolling for large lists
- Image lazy loading
- Async operations

---

## Error Handling Architecture

```
┌────────────────────────────────────────┐
│           Error Boundary               │
├────────────────────────────────────────┤
│  Try-Catch at ViewModel level          │
└────────────────┬───────────────────────┘
                 ↓
┌────────────────────────────────────────┐
│         Logging Service                │
├────────────────────────────────────────┤
│  - Local logs                          │
│  - Remote logging (AppCenter/Sentry)   │
└────────────────┬───────────────────────┘
                 ↓
┌────────────────────────────────────────┐
│         User Notification              │
├────────────────────────────────────────┤
│  - Friendly error messages             │
│  - Retry options                       │
│  - Support contact                     │
└────────────────────────────────────────┘
```

---

## Testing Architecture

### Unit Tests
```
ViewModels → Mock Services → Assert Behavior
Services → Mock Repositories → Assert Logic
```

### Integration Tests
```
Service → Real Repository → SQLite In-Memory
Service → Mock External API → Assert Integration
```

### UI Tests
```
.NET MAUI Test Framework
├── Page Navigation Tests
├── Form Validation Tests
└── User Flow Tests
```

---

## Deployment Architecture

### iOS
```
Source Code → Xcode Build → IPA File → TestFlight → App Store
```

### Android
```
Source Code → Visual Studio Build → AAB File → Internal Testing → Play Store
```

### CI/CD Pipeline
```
GitHub Push → GitHub Actions → Build → Test → Deploy to TestFlight/Play Store
```

---

## Monitoring & Analytics

### Application Monitoring
- Crash reporting (AppCenter)
- Performance monitoring
- API latency tracking

### User Analytics
- Screen views
- Feature usage
- User flows
- Conversion funnels

### Business Metrics
- Active users
- Prescription uploads
- Reminder adherence rate
- User retention

---

**Document Version:** 1.0.0  
**Last Updated:** December 20, 2025  
**Status:** Active Development

---

[← Back to Project Overview](01-project-overview.md) | [Next: Database Schema →](03-database-schema.md)