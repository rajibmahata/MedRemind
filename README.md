# 🏥 MedRemind - Never Miss Your Medication

[![.NET MAUI](https://img.shields.io/badge/.NET%20MAUI-8.0-blue)](https://dotnet.microsoft.com/apps/maui)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-iOS%20%7C%20Android-lightgrey)](https://github.com/rajibmahata/MedRemind)

**MedRemind** is an AI-powered medication reminder application that helps users never miss their medication by combining prescription scanning, voice reminders from loved ones, and smart scheduling.

## 🎯 Vision

To create a world where no one misses their medication by leveraging AI technology and the power of emotional connection through voice reminders.

---

## ✨ Key Features

### 📸 AI-Powered Prescription Reading
- Upload prescription photos via camera or gallery
- Automatic extraction of medication details using OpenAI Vision API
- Medicine name, dosage, frequency, and duration parsing
- User verification before saving

### 🔔 Smart Reminders
- Schedule reminders based on prescription instructions
- Voice message reminders from yourself or loved ones
- Local push notifications
- Customizable reminder times

### 🔐 Secure Authentication
- Phone number-based login with OTP via 2Factor.in
- Biometric authentication (fingerprint/Face ID)
- Secure local data storage

### 💊 Medication Management
- Track all active medications
- View medication history
- Edit or delete medications
- Mark doses as taken/skipped/missed
- Adherence tracking

### 🎤 Voice Recording
- Record custom voice reminders
- Tag recordings with names (Mom, Dad, Self)
- Play voice messages with notifications
- Up to 30-second recordings

---

## 🛠️ Technology Stack

### Frontend
- **.NET MAUI 8.0** - Cross-platform framework
- **C# 12** - Programming language
- **XAML** - UI markup
- **MVVM Pattern** - Architecture

### Database
- **SQLite** - Local database
- **SQLite-net-pcl** - ORM

### AI & APIs
- **OpenAI GPT-4 Vision API** - Prescription reading
- **2Factor.in API** - SMS OTP (₹0.10-0.15 per SMS)

### Plugins
- **Plugin.Maui.Camera** - Camera access
- **Plugin.Maui.Audio** - Voice recording/playback
- **Plugin.Fingerprint** - Biometric authentication
- **Plugin.LocalNotification** - Local notifications

---

## 📁 Project Structure

```
MedRemind/
├── docs/                           # Documentation
│   ├── 01-project-overview.md
│   ├── 02-architecture.md
│   ├── 03-database-schema.md
│   ├── 04-2factor-sms-integration.md
│   └── ...
├── src/                            # Source code
│   ├── MedRemind/                  # Main MAUI project
│   │   ├── Models/
│   │   ├── ViewModels/
│   │   ├── Views/
│   │   ├── Services/
│   │   └── Repositories/
│   └── MedRemind.Tests/            # Unit tests
├── .github/                        # GitHub Actions workflows
├── README.md
└── LICENSE
```

---

## 🚀 Getting Started

### Prerequisites

- **.NET 8 SDK** or later
- **Visual Studio 2022** (v17.8+) or **Visual Studio Code**
- **Android SDK** (for Android development)
- **Xcode** (for iOS development, macOS only)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/rajibmahata/MedRemind.git
   cd MedRemind
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure API keys**
   
   Create `appsettings.json` in the project root:
   ```json
   {
     "OpenAI": {
       "ApiKey": "your_openai_api_key"
     },
     "TwoFactor": {
       "ApiKey": "your_2factor_api_key",
       "SenderId": "MEDRMD"
     }
   }
   ```

4. **Build the project**
   ```bash
   dotnet build
   ```

5. **Run on Android**
   ```bash
   dotnet build -t:Run -f net8.0-android
   ```

6. **Run on iOS** (macOS only)
   ```bash
   dotnet build -t:Run -f net8.0-ios
   ```

---

## 📖 Documentation

Comprehensive documentation is available in the [`docs/`](docs/) folder:

1. [Project Overview](docs/01-project-overview.md) - Executive summary and vision
2. [Architecture](docs/02-architecture.md) - System architecture and design
3. [Database Schema](docs/03-database-schema.md) - Complete database design
4. [2Factor.in SMS Integration](docs/04-2factor-sms-integration.md) - SMS service setup

---

## 🎯 Roadmap

### MVP Phase (2 Weeks) ✅
- [x] User authentication (Phone + OTP)
- [x] Biometric login
- [x] Prescription upload
- [x] AI prescription reading
- [x] Voice recording
- [x] Reminder scheduling
- [x] Local notifications

### Phase 2 (Future)
- [ ] Cloud backup and sync
- [ ] Multi-device support
- [ ] Family member accounts
- [ ] Medication interaction warnings
- [ ] Pharmacy integration
- [ ] Doctor consultation booking
- [ ] Adherence reports

---

## 💰 Cost Structure (MVP)

### Development
- .NET MAUI: **FREE**
- Visual Studio Community: **FREE**
- SQLite: **FREE**

### Monthly Operations (100-1000 users)
| Service | Cost |
|---------|------|
| OpenAI API | $5-20/month |
| 2Factor.in SMS | ₹960-8,320/month ($12-100) |
| **Total** | **$17-120/month** |

---

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test
```

### Run on Physical Device

**Android:**
1. Enable Developer Options on your device
2. Enable USB Debugging
3. Connect device via USB
4. Run: `dotnet build -t:Run -f net8.0-android`

**iOS:**
1. Connect iPhone/iPad via USB
2. Trust computer on device
3. Run: `dotnet build -t:Run -f net8.0-ios`

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct.

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👥 Team

- **Project Lead**: Rajib Mahata ([@rajibmahata](https://github.com/rajibmahata))
- **Developers**: [Your Team Members]

---

## 📞 Support

- **Email**: support@medremind.app
- **GitHub Issues**: [Create an issue](https://github.com/rajibmahata/MedRemind/issues)
- **Documentation**: [Read the docs](docs/)

---

## 🙏 Acknowledgments

- OpenAI for GPT-4 Vision API
- 2Factor.in for affordable SMS service
- .NET MAUI team for the awesome framework
- Open source community

---

## 📊 Project Status

**Status**: 🚧 In Development  
**Target Launch**: January 1, 2026  
**Current Phase**: MVP Development (Week 1/2)

---

## ⭐ Show Your Support

If you find this project useful, please consider giving it a ⭐ on GitHub!

---

**Made with ❤️ for better health outcomes**