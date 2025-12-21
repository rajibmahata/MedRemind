# MedRemind Android App Icon Design

## ?? Icon Design Overview

### App Icon
**Location**: `mobile/MedRemind.Mobile/Resources/AppIcon/`

**Design Elements**:
1. **Background (appicon.svg)**:
   - Medical green gradient (#4CAF50 ? #388E3C)
   - Professional healthcare look
   
2. **Foreground (appiconfg.svg)**:
   - **Pill Capsule**: White and light green split capsule (rotated 45°)
   - **Clock Symbol**: Represents medication reminders and scheduling
   - **Notification Bell**: Red badge indicating active reminders
   
**Color Scheme**:
- Primary Green: `#4CAF50` (Medical/Healthcare standard)
- Dark Green: `#388E3C` (Gradient)
- Light Green: `#81C784` (Capsule half)
- White: `#FFFFFF` (Capsule half)
- Alert Red: `#FF5722` (Notification badge)

### Splash Screen
**Location**: `mobile/MedRemind.Mobile/Resources/Splash/splash.svg`

**Elements**:
- Large pill capsule with clock
- "MedRemind" branding text
- Tagline: "Never Miss a Dose"

## ?? Generated Android Icons

.NET MAUI automatically generates all required Android icon sizes:

| Density | Size | Location |
|---------|------|----------|
| mdpi | 48x48 | `mipmap-mdpi/` |
| hdpi | 72x72 | `mipmap-hdpi/` |
| xhdpi | 96x96 | `mipmap-xhdpi/` |
| xxhdpi | 144x144 | `mipmap-xxhdpi/` |
| xxxhdpi | 192x192 | `mipmap-xxxhdpi/` |

## ?? How to Modify Icons

### Change Background Color
Edit `mobile/MedRemind.Mobile/MedRemind.Mobile.csproj`:
```xml
<MauiIcon Include="Resources\AppIcon\appicon.svg" 
          ForegroundFile="Resources\AppIcon\appiconfg.svg" 
          Color="#4CAF50" />  <!-- Change this hex color -->
```

### Update Icon Design
1. Edit SVG files in `Resources/AppIcon/`
2. Clean and rebuild project:
   ```bash
   dotnet clean
   dotnet build -f net10.0-android
   ```
3. Icons auto-generate during build

## ?? Icon Design Guidelines

### Medical App Best Practices
? **DO**:
- Use green/blue tones (healthcare standard)
- Include medical symbols (pill, cross, heartbeat)
- Keep design simple and recognizable
- Use high contrast for visibility
- Include reminder/clock elements

? **DON'T**:
- Use red as primary color (associated with danger)
- Overcomplicate with too many elements
- Use small text (unreadable at small sizes)
- Violate medical symbol guidelines

## ?? Testing Your Icon

### On Emulator
1. Build and deploy app
2. Check home screen icon
3. Verify all sizes look good

### On Physical Device
1. Install app via USB
2. Check icon on home screen
3. Check notification icon
4. Verify app switcher icon

## ?? Icon Dimensions

- **SVG Source**: 456x456px
- **Android Adaptive Icon**: 108x108dp
- **Safe Zone**: Keep important content within center 72x72dp
- **Foreground**: Should work on any background color

## ?? Regenerate Icons After Changes

```bash
# Navigate to mobile project
cd mobile/MedRemind.Mobile

# Clean previous builds
dotnet clean

# Rebuild to regenerate icons
dotnet build -f net10.0-android
```

## ?? Preview Generated Icons

After building, check generated icons at:
```
mobile/MedRemind.Mobile/obj/Debug/net10.0-android/
  ??? lp/
  ?   ??? [density folders with generated PNGs]
```

## ?? Alternative Icon Variations

If you want to try different designs, here are some ideas:

1. **Pill Only**: Simple capsule, no clock
2. **Medical Cross**: Traditional healthcare symbol
3. **Calendar + Pill**: Emphasize scheduling
4. **Heartbeat Line**: Health monitoring theme
5. **Bell + Pill**: Focus on reminders

To implement, just edit the SVG files and rebuild!

---

**Current Status**: ? Icons created and built successfully!
**App Ready**: Yes, icons will appear when you deploy to device/emulator
