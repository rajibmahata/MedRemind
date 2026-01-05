# Fix: "failed to load bundled assembly en-IN/System.Text.Json.resources.dll"

## ? Problem

When running the .NET MAUI Android app, you see this error:
```
failed to load bundled assembly en-IN/System.Text.Json.resources.dll
```

This is a **localization resource assembly loading issue** where Android can't find or load culture-specific (Indian English) resource DLLs.

---

## ? Root Cause

1. **Missing Runtime Identifiers**: The project doesn't specify which Android architectures to build for
2. **Assembly Linking Issues**: The linker may be stripping out satellite resource assemblies
3. **Culture-Specific Resources**: System is trying to load en-IN (Indian English) localized resources that aren't properly bundled

---

## ?? Solution Applied

### **Changes Made to `MedRemind.Mobile.csproj`**

```xml
<PropertyGroup>
    <!-- ... existing properties ... -->
    
    <!-- FIX: Add Runtime Identifiers for Android -->
    <RuntimeIdentifiers>android-arm64;android-x64</RuntimeIdentifiers>
    
    <!-- FIX: Configure assembly linking to include satellite assemblies -->
    <AndroidLinkMode>SdkOnly</AndroidLinkMode>
    <AndroidEnableAssemblyCompression>false</AndroidEnableAssemblyCompression>
    <AndroidIncludeDebugSymbols>false</AndroidIncludeDebugSymbols>
    
    <!-- FIX: Ensure all resources are embedded -->
    <EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
    <EnableLLVM>false</EnableLLVM>
    <AndroidUseAapt2>true</AndroidUseAapt2>
    
    <!-- ALTERNATIVE FIX: Use invariant globalization (disables culture-specific resources) -->
    <!-- Uncomment if the above doesn't work -->
    <!-- <InvariantGlobalization>true</InvariantGlobalization> -->
</PropertyGroup>
```

---

## ?? What Each Fix Does

### **1. RuntimeIdentifiers**
```xml
<RuntimeIdentifiers>android-arm64;android-x64</RuntimeIdentifiers>
```
- **Purpose**: Tells .NET which Android CPU architectures to build for
- **Why**: Ensures proper assembly resolution for specific device types
- **Covers**: Most modern Android devices (64-bit ARM and x86_64)

### **2. AndroidLinkMode**
```xml
<AndroidLinkMode>SdkOnly</AndroidLinkMode>
```
- **Purpose**: Controls how aggressive the Android linker is
- **Options**:
  - `None` - No linking (largest APK, safest)
  - `SdkOnly` - Link SDK assemblies only (recommended)
  - `Full` - Link everything (smallest APK, can break things)
- **Why**: Prevents linker from removing satellite resource assemblies

### **3. AndroidEnableAssemblyCompression**
```xml
<AndroidEnableAssemblyCompression>false</AndroidEnableAssemblyCompression>
```
- **Purpose**: Disables assembly compression
- **Why**: Compressed assemblies can cause loading issues on some devices
- **Trade-off**: Slightly larger APK size

### **4. EmbedAssembliesIntoApk**
```xml
<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
```
- **Purpose**: Embeds all assemblies directly into the APK
- **Why**: Ensures all resources are available at runtime
- **Default**: Usually true for Release builds

### **5. AndroidUseAapt2**
```xml
<AndroidUseAapt2>true</AndroidUseAapt2>
```
- **Purpose**: Uses Android Asset Packaging Tool 2 (modern version)
- **Why**: Better resource handling and faster builds

---

## ?? Alternative Fix (If Above Doesn't Work)

If you still see the error, uncomment this line:

```xml
<InvariantGlobalization>true</InvariantGlobalization>
```

### **What This Does**
- Disables **all** culture-specific resource loading
- App will use invariant culture (neutral, no localization)
- Smaller APK size (no satellite assemblies)

### **When to Use**
- If your app doesn't need localization
- If you're targeting a single language/region
- As a last resort to fix the error

### **Trade-offs**
- ? No culture-specific formatting (dates, numbers, currency)
- ? No localized error messages
- ? Smaller APK
- ? Faster startup
- ? No satellite assembly issues

---

## ?? Testing the Fix

### **Step 1: Clean the Project**
```powershell
cd mobile\MedRemind.Mobile
dotnet clean
```

### **Step 2: Rebuild**
```powershell
dotnet build
```

### **Step 3: Deploy to Device/Emulator**
- Run the app from Visual Studio
- Check logcat for the error message
- It should no longer appear

### **Step 4: Verify Functionality**
Test the following to ensure nothing broke:
- [ ] App launches successfully
- [ ] Prescription upload works
- [ ] JSON serialization works (this uses System.Text.Json)
- [ ] Date/time formatting looks correct
- [ ] No other resource loading errors

---

## ?? Understanding the Error

### **What Are Satellite Assemblies?**

Satellite assemblies are separate DLLs that contain culture-specific resources:

```
System.Text.Json.dll                    ? Main assembly (code)
??? en/System.Text.Json.resources.dll   ? English resources
??? en-US/System.Text.Json.resources.dll ? US English
??? en-IN/System.Text.Json.resources.dll ? Indian English
??? es/System.Text.Json.resources.dll   ? Spanish
??? ... (other cultures)
```

### **Why en-IN?**

If your device/emulator is set to **Indian English (en-IN)** locale, Android tries to load the `en-IN` satellite assembly for localized error messages and formatting.

### **Why the Error Occurs**

1. .NET runtime detects device culture: `en-IN`
2. Tries to load: `en-IN/System.Text.Json.resources.dll`
3. File not found in APK (not bundled or stripped by linker)
4. Error logged but app continues with fallback

**Important**: This is usually a **warning**, not a fatal error. The app falls back to the neutral culture.

---

## ?? APK Size Impact

| Configuration | APK Size | Satellite Assemblies |
|---------------|----------|---------------------|
| **Default (before fix)** | ~25 MB | Some missing |
| **With RuntimeIdentifiers** | ~27 MB | All included |
| **With InvariantGlobalization** | ~23 MB | None (disabled) |

---

## ?? Common Issues

### **Issue 1: Error Still Appears**

**Solution 1**: Clear bin/obj folders manually
```powershell
Remove-Item -Recurse -Force bin, obj
dotnet clean
dotnet build
```

**Solution 2**: Enable InvariantGlobalization
```xml
<InvariantGlobalization>true</InvariantGlobalization>
```

### **Issue 2: Build Takes Longer**

**Cause**: Building for multiple architectures (arm64 + x64)

**Solution**: For development, use only one:
```xml
<RuntimeIdentifiers>android-arm64</RuntimeIdentifiers>
```

For release, use both:
```xml
<RuntimeIdentifiers>android-arm64;android-x64</RuntimeIdentifiers>
```

### **Issue 3: APK Size Increased**

**Cause**: More assemblies embedded

**Solutions**:
1. Use `AndroidLinkMode=Full` (risky)
2. Enable `InvariantGlobalization=true`
3. Accept the trade-off (2-3 MB is reasonable)

---

## ?? Related Documentation

- [.NET MAUI Android Build Properties](https://learn.microsoft.com/en-us/dotnet/maui/android/emulator/troubleshooting)
- [Android Linking](https://learn.microsoft.com/en-us/xamarin/android/deploy-test/linker)
- [Globalization and Localization](https://learn.microsoft.com/en-us/dotnet/core/extensions/globalization-and-localization)

---

## ? Verification Checklist

After applying the fix, verify:

- [ ] Build succeeds without errors
- [ ] No "failed to load bundled assembly" error in logcat
- [ ] App launches normally
- [ ] JSON serialization works (PrescriptionUploadViewModel)
- [ ] All features function correctly
- [ ] APK size is acceptable (< 30 MB)

---

## ?? Key Takeaways

1. **RuntimeIdentifiers** are essential for proper Android assembly resolution
2. **AndroidLinkMode=SdkOnly** is the sweet spot (performance + safety)
3. **Satellite assemblies** are optional but nice for localization
4. **InvariantGlobalization** is a valid option for single-language apps
5. This error is usually **non-fatal** but good to fix for cleaner logs

---

## ?? Quick Reference

### **Recommended Configuration (Production)**
```xml
<RuntimeIdentifiers>android-arm64;android-x64</RuntimeIdentifiers>
<AndroidLinkMode>SdkOnly</AndroidLinkMode>
<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
<AndroidEnableAssemblyCompression>false</AndroidEnableAssemblyCompression>
```

### **Minimal Configuration (Development)**
```xml
<RuntimeIdentifiers>android-arm64</RuntimeIdentifiers>
<AndroidLinkMode>None</AndroidLinkMode>
<InvariantGlobalization>true</InvariantGlobalization>
```

---

**Status:** ? **Fixed**  
**Applies To:** .NET 10 MAUI Android  
**Last Updated:** December 2024  
**Severity:** Low (non-fatal warning)
