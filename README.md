# MedRemind

**Never miss your medication** - AI-powered prescription reader and medication reminder app for iOS and Android built with .NET MAUI

## 🏥 Overview

MedRemind is a comprehensive medication management application that helps users track their medications, set reminders, and scan prescriptions using AI-powered OCR technology. Built with .NET MAUI, it provides a native experience on both iOS and Android platforms.

## ✨ Features

### 📋 Medication Management
- **Add & Track Medications**: Easily add medications with details including name, dosage, frequency, and instructions
- **Medication List**: View all your active medications in one place
- **Edit & Delete**: Update medication information or remove medications you no longer take
- **Medication History**: Keep track of start dates and duration

### 📸 AI-Powered Prescription Reader
- **Camera Integration**: Take photos of prescriptions directly in the app
- **Photo Gallery**: Select existing prescription images from your device
- **OCR Text Extraction**: Automatically extract medication information from prescription images
- **Prescription History**: Store and review past prescriptions

### ⏰ Medication Reminders
- **Scheduled Notifications**: Set reminders for when to take each medication
- **Multiple Reminders**: Configure different times for medications taken multiple times per day
- **Smart Scheduling**: Reminders based on medication frequency (once daily, twice daily, etc.)

### 💾 Local Data Storage
- **SQLite Database**: All data stored locally on your device for privacy
- **Offline Functionality**: Works without internet connection
- **Data Persistence**: Your medication data is always available

## 🚀 Technology Stack

- **.NET MAUI**: Cross-platform framework for iOS and Android
- **C# 10**: Modern C# with nullable reference types
- **MVVM Pattern**: Clean architecture with CommunityToolkit.Mvvm
- **SQLite**: Local database for medication and prescription storage
- **Microsoft.Maui.Essentials**: Camera, media picker, and file system APIs

## 📱 Supported Platforms

- Android 5.0 (API 21) and above
- iOS 15.0 and above (when built on macOS)

## 🏗️ Architecture

The app follows MVVM (Model-View-ViewModel) architecture:

- **Models**: Data entities (Medication, Prescription, Reminder)
- **Services**: Business logic (DatabaseService, OcrService, NotificationService)
- **ViewModels**: Presentation logic with data binding
- **Pages**: XAML-based UI views

## 🔧 Building the App

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension
- .NET MAUI workload installed

### Build Instructions

```bash
# Clone the repository
git clone https://github.com/rajibmahata/MedRemind.git
cd MedRemind

# Restore dependencies
dotnet restore

# Build for Android
dotnet build -c Release -f net10.0-android

# Build for iOS (requires macOS)
dotnet build -c Release -f net10.0-ios
```

## 📝 Future Enhancements

- Integration with real OCR services (Azure Computer Vision, Google Vision API, or ML Kit)
- Platform-specific notification implementations
- Medication interaction warnings
- Export medication history
- Cloud backup and sync
- Dosage tracking and adherence reporting
- Integration with pharmacy services

## 🔒 Privacy & Security

- All data stored locally on device
- No cloud storage or third-party data sharing
- Camera and notification permissions only used for intended features

## 📄 License

This project is open source and available under the MIT License.

## 👨‍💻 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 🐛 Issues

If you encounter any issues or have suggestions, please file an issue on the GitHub repository.

---

Made with ❤️ using .NET MAUI