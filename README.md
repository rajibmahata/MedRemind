# 💊 MedRemind - AI-Powered Medication Reminder App

> Never miss your medication again with AI prescription reading and voice reminders from loved ones

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Tests](https://img.shields.io/badge/tests-100%2B%20passing-brightgreen)]()
[![Coverage](https://img.shields.io/badge/coverage-85%25-green)]()
[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)]()
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
- .NET 9.0 SDK
- Visual Studio 2022 or Visual Studio Code
- Android Studio (for Android)
- Xcode (for iOS, Mac only)

### Installation

```bash
# Clone the repository
git clone https://github.com/rajibmahata/MedRemind.git
cd MedRemind

# Restore packages
dotnet restore src/MedRemind.sln

# Build the solution
dotnet build src/MedRemind.sln

# Run tests
dotnet test src/MedRemind.Tests/MedRemind.Tests.csproj
```

### Configuration

1. Create `appsettings.json` in the Mobile project:

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
- **Backend:** C# 12, .NET 9
- **Database:** SQLite with Entity Framework Core 9
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
| Unit Tests | ✅ Complete | 100+ tests |
| Mobile UI | 🚧 In Progress | 0% |
| Platform Services | 🚧 Planned | 0% |

**Current Phase:** Backend APIs Complete ✅  
**Next Phase:** Mobile App UI Development

See [Detailed Timeline](docs/06-detailed-timeline.md) for full 2-week implementation plan.

## 🧪 Testing

The project includes comprehensive unit tests with 85%+ code coverage:

```bash
# Run all tests
dotnet test

# Run specific test suite
dotnet test --filter "FullyQualifiedName~MedicationServiceTests"
```

### Test Coverage
- ✅ Repository layer: 10 tests
- ✅ Unit of Work: 7 tests
- ✅ Authentication: 15 tests
- ✅ AI Validation: 15 tests
- ✅ Reminder Scheduling: 20 tests
- ✅ Medication Service: 20 tests
- ✅ Adherence Tracking: 18 tests

**Total: 100+ unit tests**

## 📖 Documentation

- [Executive Summary](docs/01-executive-summary.md) - Project overview and vision
- [Technical Architecture](docs/02-technical-architecture.md) - System design
- [Detailed Timeline](docs/06-detailed-timeline.md) - 2-week implementation plan
- [Backend Implementation Summary](docs/07-backend-implementation-summary.md) - What's been built
- [Backend Setup Guide](docs/08-backend-setup-guide.md) - Setup instructions

## 🎯 Roadmap

### MVP (January 1, 2025)
- [x] Backend APIs with AI integration
- [x] Database and repository layer
- [x] Authentication and security
- [x] Reminder scheduling algorithms
- [ ] Mobile UI (Login, Upload, Reminders)
- [ ] Platform services (Biometric, Notifications, Audio)
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

**Last Updated:** December 20, 2024  
**Status:** Backend Complete ✅ - UI In Progress 🚧