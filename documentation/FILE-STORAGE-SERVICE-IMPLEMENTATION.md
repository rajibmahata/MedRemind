# File Storage Service Implementation

## ? Complete File Storage Service with Configurable Settings

### **Overview**

Created a comprehensive file storage service that:
- ? Handles all file save operations (OCR logs, prescription images, text files)
- ? Supports both **private app storage** and **public storage** (Android Documents folder)
- ? **Fully configurable** via `appsettings.json`
- ? **Platform-aware** (Android-specific public storage with media scanning)
- ? **Auto-cleanup** of old files
- ? **Unique file naming** with timestamps

---

## ?? Files Created

### **1. Configuration**
```
backend\MedRemind.Core\Configuration\FileStorageConfiguration.cs
```

**Properties:**
- `OcrLogsFolderName` - Folder name for OCR logs (default: "MedRemind_OCR_Logs")
- `PrescriptionsFolderName` - Folder for prescription images (default: "MedRemind_Prescriptions")
- `EnableFileLogging` - Enable/disable file logging (default: true)
- `CopyToPublicStorage` - Copy to public Documents folder (default: false for production)
- `PublicStorageFolderName` - Public folder name (default: "MedRemind_Debug")
- `MaxLogFiles` - Maximum log files to keep (default: 100)
- `IncludeTimestampInFileName` - Add timestamps to filenames (default: true)
- `OcrFilePrefix` - File name prefix (default: "Prescription_OCR")

### **2. Interface**
```
backend\MedRemind.Core\Interfaces\IFileStorageService.cs
```

**Methods:**
- `SaveOcrResultsAsync()` - Save raw, normalized, and JSON files
- `SavePrescriptionImageAsync()` - Save prescription image
- `SaveTextFileAsync()` - Save any text file
- `CleanupOldFilesAsync()` - Delete old files
- `GetOcrLogsDirectory()` - Get OCR logs path
- `GetPrescriptionsDirectory()` - Get prescriptions path
- `IsFileLoggingEnabled()` - Check if logging is enabled

### **3. Implementation**
```
backend\MedRemind.Services\Storage\FileStorageService.cs
```

**Features:**
- ? **Cross-platform** storage (MAUI FileSystem API)
- ? **Android public storage** support (optional, with media scanning)
- ? **Auto directory creation**
- ? **Unique file naming** with date + Unix timestamp
- ? **Auto cleanup** of old files
- ? **Configurable** via Options pattern

---

## ?? Configuration in appsettings.json

### **Development Environment** (CopyToPublicStorage: Enabled)
```json
{
  "FileStorage": {
    "OcrLogsFolderName": "MedRemind_OCR_Logs",
    "PrescriptionsFolderName": "MedRemind_Prescriptions",
    "EnableFileLogging": true,
    "CopyToPublicStorage": true,  // ? Enabled for testing
    "PublicStorageFolderName": "MedRemind_Debug",
    "MaxLogFiles": 100,
    "IncludeTimestampInFileName": true,
    "OcrFilePrefix": "Prescription_OCR"
  }
}
```

### **Staging Environment** (CopyToPublicStorage: Enabled, more logs)
```json
{
  "FileStorage": {
    "OcrLogsFolderName": "MedRemind_OCR_Logs",
    "PrescriptionsFolderName": "MedRemind_Prescriptions",
    "EnableFileLogging": true,
    "CopyToPublicStorage": true,  // ? Enabled for staging
    "PublicStorageFolderName": "MedRemind_Staging_Debug",
    "MaxLogFiles": 200,  // More logs for staging
    "IncludeTimestampInFileName": true,
    "OcrFilePrefix": "Prescription_OCR"
  }
}
```

### **Production Environment** (CopyToPublicStorage: Disabled)
```json
{
  "FileStorage": {
    "OcrLogsFolderName": "MedRemind_OCR_Logs",
    "PrescriptionsFolderName": "MedRemind_Prescriptions",
    "EnableFileLogging": true,
    "CopyToPublicStorage": false,  // ? Disabled for security
    "PublicStorageFolderName": "MedRemind_Production",
    "MaxLogFiles": 500,  // Keep more logs in production
    "IncludeTimestampInFileName": true,
    "OcrFilePrefix": "Prescription_OCR"
  }
}
```

---

## ?? Registration in MauiProgram.cs

```csharp
// Configure FileStorageService with settings from appsettings.json
builder.Services.Configure<FileStorageConfiguration>(options =>
{
    var config = EmbeddedConfigurationLoader.GetActiveEnvironmentConfig();
    var fileStorageConfig = config.FileStorage;
    
    if (fileStorageConfig != null)
    {
        options.OcrLogsFolderName = fileStorageConfig.OcrLogsFolderName;
        options.PrescriptionsFolderName = fileStorageConfig.PrescriptionsFolderName;
        options.EnableFileLogging = fileStorageConfig.EnableFileLogging;
        options.CopyToPublicStorage = fileStorageConfig.CopyToPublicStorage;
        options.PublicStorageFolderName = fileStorageConfig.PublicStorageFolderName;
        options.MaxLogFiles = fileStorageConfig.MaxLogFiles;
        options.IncludeTimestampInFileName = fileStorageConfig.IncludeTimestampInFileName;
        options.OcrFilePrefix = fileStorageConfig.OcrFilePrefix;
    }
});

builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
```

---

## ?? Storage Locations

### **Private App Storage** (Always Used)
```
Android: /data/data/com.medremind.app/files/
  ?? MedRemind_OCR_Logs/
  ?  ?? Prescription_OCR_20260110_1736516340_raw.txt
  ?  ?? Prescription_OCR_20260110_1736516340_normalized.txt
  ?  ?? Prescription_OCR_20260110_1736516340_response.json
  ?? MedRemind_Prescriptions/
     ?? Prescription_Image_20260110_1736516350.jpg
```

**Characteristics:**
- ? **Secure** - Only accessible by your app
- ? **Persistent** - Remains until app uninstall
- ? **Backed up** - Included in Android backup
- ? **Not user-accessible** - Can't view in file manager

### **Public Storage** (Optional, Android Only)
```
Android: /storage/emulated/0/Documents/
  ?? MedRemind_Debug/  (or MedRemind_Staging_Debug/, MedRemind_Production/)
     ?? Prescription_OCR_20260110_1736516340_raw.txt
     ?? Prescription_OCR_20260110_1736516340_normalized.txt
     ?? Prescription_OCR_20260110_1736516340_response.json
     ?? Prescription_Image_20260110_1736516350.jpg
```

**Characteristics:**
- ? **User-accessible** - Visible in file manager
- ? **Survives app uninstall** - Files remain on device
- ? **Shareable** - Can be accessed by other apps
- ?? **Security risk** - Accessible to all apps/users
- ? **Media scanned** - Immediately visible in file manager

---

## ?? Usage in AzureDocumentIntelligenceService

### **Before** (Direct file operations)
```csharp
private static readonly string OCR_LOGS_DIRECTORY = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    "MedRemind_OCR_Logs");

private async Task SaveOcrResultsAsync(...)
{
    var rawTextPath = Path.Combine(OCR_LOGS_DIRECTORY, $"{baseFileName}_raw.txt");
    await File.WriteAllTextAsync(rawTextPath, extractedText);
    // ... more manual file operations
}
```

### **After** (Using FileStorageService)
```csharp
private readonly IFileStorageService _fileStorageService;

public AzureDocumentIntelligenceService(..., IFileStorageService fileStorageService)
{
    _fileStorageService = fileStorageService;
}

private async Task SaveOcrResultsAsync(...)
{
    // Serialize result to JSON
    var jsonContent = JsonSerializer.Serialize(result, jsonOptions);
    
    // Save all files at once
    var (rawPath, normalizedPath, jsonPath) = await _fileStorageService.SaveOcrResultsAsync(
        extractedText,
        normalizedText,
        jsonContent);
}
```

**Benefits:**
- ? **Centralized** file logic
- ? **Configurable** via appsettings.json
- ? **Consistent** naming and structure
- ? **Auto-cleanup** of old files
- ? **Public storage** support (optional)

---

## ?? File Naming Convention

### **Format:**
```
{Prefix}_{Date}_{UnixTimestamp}_{Suffix}.{Extension}
```

### **Examples:**
```
Prescription_OCR_20260110_1736516340_raw.txt
Prescription_OCR_20260110_1736516340_normalized.txt
Prescription_OCR_20260110_1736516340_response.json
Prescription_Image_20260110_1736516350.jpg
```

### **Components:**
- **Prefix:** Configurable (default: "Prescription_OCR")
- **Date:** `yyyyMMdd` format (e.g., 20260110)
- **Unix Timestamp:** Seconds since epoch (e.g., 1736516340)
- **Suffix:** Type identifier (_raw, _normalized, _response)
- **Extension:** File type (.txt, .json, .jpg)

---

## ?? Auto Cleanup

### **How It Works:**
1. After saving files, automatically calls `CleanupOldFilesAsync()`
2. Sorts files by creation time (newest first)
3. Keeps only `MaxLogFiles` (configured in appsettings.json)
4. Deletes excess files

### **Example:**
```
MaxLogFiles = 100

Current files: 105
Action: Delete 5 oldest files
Result: 100 files remaining
```

### **Configuration:**
```json
{
  "FileStorage": {
    "MaxLogFiles": 100  // 0 = unlimited, > 0 = limit
  }
}
```

---

## ?? Security Considerations

### **Private Storage** (Recommended for Production)
```json
{
  "CopyToPublicStorage": false  // ? Secure, app-only access
}
```

**Pros:**
- ? Secure - Only your app can access
- ? Complies with data privacy regulations
- ? No user intervention needed
- ? Auto-deleted on app uninstall

**Cons:**
- ? Can't view files in file manager
- ? Harder to debug issues
- ? Need adb or device backup to access

### **Public Storage** (Use Only for Testing)
```json
{
  "CopyToPublicStorage": true  // ?? For debugging only!
  "PublicStorageFolderName": "MedRemind_Debug"
}
```

**Pros:**
- ? Easy to inspect files
- ? Good for testing/debugging
- ? Visible in file manager immediately

**Cons:**
- ? **Security risk** - Files readable by any app
- ? **Privacy issue** - User data exposed
- ? Survives app uninstall (orphaned files)
- ? Requires MANAGE_EXTERNAL_STORAGE permission (Android 11+)

### **?? WARNING:**
**Never enable `CopyToPublicStorage` in production!** Prescription data is sensitive medical information (PHI/PII). Storing it in public storage violates privacy regulations (HIPAA, GDPR, etc.).

---

## ?? Android Permissions

### **Private Storage** (No Permissions Needed)
```xml
<!-- No special permissions required -->
```

### **Public Storage** (Requires Permissions)
```xml
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE"
                 android:maxSdkVersion="29" />
                 
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE"
                 android:maxSdkVersion="29" />

<!-- Android 11+ (API 30+) -->
<uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE"
                 tools:ignore="ScopedStorage" />
```

**Note:** With `CopyToPublicStorage = false`, no external storage permissions are needed.

---

## ?? Best Practices

### **1. Use Environment-Specific Settings**

```json
{
  "Development": {
    "FileStorage": {
      "CopyToPublicStorage": true,  // ? Debug locally
      "MaxLogFiles": 50
    }
  },
  "Production": {
    "FileStorage": {
      "CopyToPublicStorage": false,  // ? Security first
      "MaxLogFiles": 500
    }
  }
}
```

### **2. Clean Up Regularly**
```csharp
// Auto cleanup is called after each save
await _fileStorageService.SaveOcrResultsAsync(...);
// Cleanup happens automatically ?
```

### **3. Check If Logging Is Enabled**
```csharp
if (_fileStorageService.IsFileLoggingEnabled())
{
    await SaveFilesAsync();
}
```

### **4. Handle Errors Gracefully**
```csharp
try
{
    await _fileStorageService.SaveOcrResultsAsync(...);
}
catch (Exception ex)
{
    // Log error, but don't break main flow
    Debug.WriteLine($"?? Failed to save files: {ex.Message}");
}
```

---

## ?? Testing

### **Test 1: Verify Private Storage**
```sh
adb shell
cd /data/data/com.medremind.app/files/MedRemind_OCR_Logs
ls -la

# Expected output:
# Prescription_OCR_20260110_1736516340_raw.txt
# Prescription_OCR_20260110_1736516340_normalized.txt
# Prescription_OCR_20260110_1736516340_response.json
```

### **Test 2: Verify Public Storage** (If Enabled)
```sh
adb shell
cd /storage/emulated/0/Documents/MedRemind_Debug
ls -la

# Expected output:
# Same files as private storage
```

### **Test 3: Verify Cleanup**
```sh
# Upload 105 prescriptions (MaxLogFiles = 100)
# Check file count
adb shell ls -1 /data/data/com.medremind.app/files/MedRemind_OCR_Logs | wc -l

# Expected output: 300 (100 prescriptions × 3 files each)
```

### **Test 4: Pull Files for Inspection**
```sh
# Pull all OCR logs
adb pull /data/data/com.medremind.app/files/MedRemind_OCR_Logs ./local_logs/

# Or pull from public storage (if enabled)
adb pull /storage/emulated/0/Documents/MedRemind_Debug ./local_logs/
```

---

## ?? Storage Size Estimates

### **Per Prescription:**
- Raw OCR text: ~5-10 KB
- Normalized text: ~3-7 KB  
- JSON response: ~10-20 KB
- Prescription image: ~500 KB - 2 MB

**Total per prescription:** ~520 KB - 2.5 MB

### **With MaxLogFiles = 100:**
- Text files: 100 × 25 KB = 2.5 MB
- Images: 100 × 1 MB = 100 MB
- **Total:** ~102.5 MB

### **With MaxLogFiles = 500:**
- Text files: 500 × 25 KB = 12.5 MB
- Images: 500 × 1 MB = 500 MB
- **Total:** ~512.5 MB

---

## ? Summary

### **What Was Created:**

1. ? **FileStorageConfiguration** - Configuration model
2. ? **IFileStorageService** - Service interface
3. ? **FileStorageService** - Implementation with all features
4. ? **appsettings.json** - Configuration for all environments
5. ? **MauiProgram.cs** - Service registration
6. ? **AzureDocumentIntelligenceService** - Updated to use service
7. ? **EmbeddedConfigurationLoader** - Updated with FileStorage model

### **Features Implemented:**

? Private app storage (secure, app-only)  
? Public storage (optional, Android Documents)  
? Configurable via appsettings.json  
? Environment-specific settings (Dev/Staging/Prod)  
? Auto cleanup of old files  
? Unique file naming with timestamps  
? Media scanning (Android)  
? Cross-platform (MAUI FileSystem API)  
? Graceful error handling  
? Enable/disable file logging  

### **Security:**

? Production: `CopyToPublicStorage = false` (secure)  
? Development: `CopyToPublicStorage = true` (debugging)  
? No external permissions needed for private storage  
? Sensitive data protected  

---

**Status:** ? **COMPLETE**  
**Files Created:** **7**  
**Features:** **10+**  
**Tested:** **Ready for testing**  

**Your file storage is now fully centralized, configurable, and secure!** ?????
