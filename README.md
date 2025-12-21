# 💊 MedRemind - AI-Powered Medication Reminder App

> Never miss your medication again with AI prescription reading and voice reminders from loved ones

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Tests](https://img.shields.io/badge/tests-175%2B%20passing-brightgreen)]()
[![Coverage](https://img.shields.io/badge/coverage-85%25-green)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)]()
[![License](https://img.shields.io/badge/license-MIT-blue)]()

## 🌟 Features

### 📸 AI-Powered Prescription Reading
- Snap a photo of your prescription
- GPT-4 Vision automatically extracts medication details
- Validates medicine names and dosages
- Detects potential drug interactions

### ⏰ Smart Reminders
- Automatic scheduling based on doctor's instructions
- 15+ frequency patterns supported (daily, meals, bedtime, etc.)
- Customizable reminder times
- Never miss a dose

### 🎤 Voice Reminders
- Record personalized voice messages (Mom, Dad, yourself)
- Emotional connection increases adherence
- Play back with notifications
- Makes medicine-taking more personal

### 📊 Adherence Tracking
- Monitor your medication-taking habits
- View daily, weekly, and monthly stats
- Track longest streak
- Identify patterns and improve compliance

### 🔒 Privacy First
- All data stored locally on your device
- Biometric authentication (Face ID / Fingerprint)
- No cloud storage required
- Your health data stays private

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Visual Studio 2022 (17.12+) or Visual Studio Code
- Android Studio (for Android)
- Xcode (for iOS, Mac only)

### Installation

```bash
# Clone the repository
git clone https://github.com/rajibmahata/MedRemind.git
cd MedRemind

# Build backend
cd backend
dotnet restore
dotnet build MedRemind.Backend.sln

# Run tests
dotnet test
```

### Configuration

1. Create `appsettings.json` in the Backend project:

```json
{
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY"
  },
  "TwoFactor": {
    "ApiKey": "YOUR_2FACTOR_API_KEY"
  }
}
```

2. Add API keys to `.gitignore`
3. Run the app!

For detailed setup instructions, see [Backend Setup Guide](docs/08-backend-setup-guide.md).

## 📁 Project Structure

```
MedRemind/
├── backend/                    # Backend APIs (Complete ✅)
│   ├── MedRemind.Core/        # Models, DTOs, Interfaces
│   ├── MedRemind.Services/    # Business logic services
│   ├── MedRemind.Tests/       # 175+ unit tests
│   └── MedRemind.Backend.sln  # Backend solution
│
├── mobile/                     # Mobile app (In Progress 🚧)
│   └── MedRemind.Mobile/      # .NET MAUI app
│
├── docs/                       # Documentation
│   ├── 00-project-overview.md
│   ├── 01-executive-summary.md
│   ├── 02-technical-architecture.md
│   ├── 06-detailed-timeline.md
│   ├── 07-backend-implementation-summary.md
│   ├── 08-backend-setup-guide.md
│   ├── 09-backend-build-status.md
│   └── 10-backend-reorganization-summary.md
│
└── README.md                   # This file
```

## 🏗️ Architecture

MedRemind follows Clean Architecture principles with a layered approach:

```
┌─────────────────────────────────────────┐
│           Mobile App (MAUI)             │
│    Views, ViewModels, Platform Services │
├─────────────────────────────────────────┤
│          Business Services              │
│  Authentication, AI, Reminders, Media   │
├─────────────────────────────────────────┤
│         Data Access Layer               │
│  Repositories, Unit of Work, EF Core    │
├─────────────────────────────────────────┤
│              Core Models                │
│    Entities, DTOs, Interfaces           │
└─────────────────────────────────────────┘
```

### Tech Stack
- **Frontend:** .NET MAUI (iOS & Android)
- **Backend:** C# 13, .NET 10
- **Database:** SQLite with Entity Framework Core 10
- **AI:** OpenAI GPT-4 Vision API
- **Authentication:** 2Factor.in OTP service
- **Testing:** xUnit, Moq
- **Architecture:** Repository Pattern, Unit of Work, MVVM

## 📊 Project Status

| Component | Status | Progress |
|-----------|--------|----------|
| Core Models | ✅ Complete | 100% |
| Repository Layer | ✅ Complete | 100% |
| Authentication | ✅ Complete | 100% |
| AI Services | ✅ Complete | 100% |
| Reminder Services | ✅ Complete | 100% |
| Medication CRUD | ✅ Complete | 100% |
| Adherence Tracking | ✅ Complete | 100% |
| Unit Tests | ✅ Complete | 175+ tests |
| Mobile UI | 🚧 In Progress | 0% |
| Platform Services | 🚧 Planned | 0% |

**Current Phase:** Backend Complete ✅ - UI Development Next  
**Next Phase:** Mobile App UI Development

See [Detailed Timeline](docs/06-detailed-timeline.md) for full 2-week implementation plan.

## 🧪 Testing

The project includes comprehensive unit tests with 85%+ code coverage:

```bash
# Build backend
cd backend
dotnet build

# Run all tests
dotnet test

# Run specific test suite
dotnet test --filter "FullyQualifiedName~RepositoryTests"
```

### Test Coverage
- ✅ Repository layer: 17 tests
- ✅ Unit of Work: 7 tests
- ✅ Authentication: 15 tests
- ✅ AI Validation: 15 tests
- ✅ Reminder Scheduling: 20 tests
- ✅ Medication Service: 20 tests
- ✅ Adherence Tracking: 18 tests
- ✅ Additional Services: 63 tests

**Total: 175+ unit tests**

## 📖 Documentation

- [Executive Summary](docs/01-executive-summary.md) - Project overview and vision
- [Technical Architecture](docs/02-technical-architecture.md) - System design
- [Detailed Timeline](docs/06-detailed-timeline.md) - 2-week implementation plan
- [Backend Implementation Summary](docs/07-backend-implementation-summary.md) - What's been built
- [Backend Setup Guide](docs/08-backend-setup-guide.md) - Setup instructions
- [Backend Build Status](docs/09-backend-build-status.md) - Current status
- [Backend Reorganization](docs/10-backend-reorganization-summary.md) - Structure changes

## 🎯 Roadmap

### MVP (January 1, 2025)
- [x] Backend APIs with AI integration ✅
- [x] Database and repository layer ✅
- [x] Authentication and security ✅
- [x] Reminder scheduling algorithms ✅
- [x] 175+ unit tests ✅
- [ ] Mobile UI (Login, Upload, Reminders) 🚧
- [ ] Platform services (Biometric, Notifications, Audio) 🚧
- [ ] App Store submission

### Phase 2 (Post-Launch)
- [ ] Web dashboard for caregivers
- [ ] Multi-language support
- [ ] Pharmacy integration
- [ ] Health metrics integration (Apple Health, Google Fit)
- [ ] Offline OCR for prescriptions
- [ ] Medication refill reminders

## 🤝 Contributing

Contributions are welcome! Please read our contributing guidelines first.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- **OpenAI** for GPT-4 Vision API
- **2Factor.in** for OTP authentication
- **Microsoft** for .NET MAUI framework

## 📧 Contact

**Rajib Mahata**
- GitHub: [@rajibmahata](https://github.com/rajibmahata)

## 🌟 Show Your Support

Give a ⭐️ if this project helped you!

---

<p align="center">
  Made with ❤️ for better health
  <br>
  <strong>Never miss your medication again</strong>
</p>

---

**Last Updated**: December 21, 2024  
**Status**: Backend Complete ✅ - UI Development Next 🚧  
**Version**: 1.0.0-beta