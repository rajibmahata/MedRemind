# Fix for "??Upload" and Other Icon Display Issues

## Problem
Emoji and Unicode characters (?, ?, ?, ?, ?, etc.) are not rendering properly in the mobile app, showing as "??" instead. This happens because the default system font doesn't support these characters.

## Root Cause
The issue appears in:
1. `PrescriptionUploadPage.xaml` - Line 40: `Text="??? Gallery"`
2. Various other pages using emoji characters

## Solution Options

### Option 1: Remove All Emojis (Recommended for Stability)

Replace all emoji characters with plain text:

**In `PrescriptionUploadPage.xaml`:**

```xml
<!-- BEFORE (Line 37-42) -->
<Button Text="?? Camera"
       Command="{Binding TakePhotoCommand}"
       Style="{StaticResource PrimaryButton}"/>

<Button Grid.Column="1"
       Text="??? Gallery"
       Command="{Binding PickPhotoCommand}"
       Style="{StaticResource SecondaryButton}"/>

<!-- AFTER -->
<Button Text="Camera"
       Command="{Binding TakePhotoCommand}"
       Style="{StaticResource PrimaryButton}"/>

<Button Grid.Column="1"
       Text="Gallery"
       Command="{Binding PickPhotoCommand}"
       Style="{StaticResource SecondaryButton}"/>
```

**Replace ALL instances:**
- `?? AI Prescription Scanner` ? `AI Prescription Scanner`
- `?? Camera` ? `Camera`
- `??? Gallery` ? `Gallery`
- `?? Process with AI` ? `Process with AI`
- `?? Save All` ? `Save All`
- `?? Try Again` ? `Clear`
- `?? Tips for Best Results` ? `Tips for Best Results`

**Similarly in `splash.svg`** (already fixed):
- Removed text elements that don't render properly
- Using only SVG graphics

### Option 2: Use Material Design Icons (Professional Approach)

Install icon font and use proper icon references:

#### Step 1: Add Material Icons Font

Download Material Design Icons from: https://fonts.google.com/icons

Add to `Resources/Fonts/` folder:
- `MaterialIcons-Regular.ttf`

#### Step 2: Register Font in `MauiProgram.cs`:

```csharp
.ConfigureFonts(fonts =>
{
    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons"); // Add this
});
```

#### Step 3: Create Icon Labels:

```xml
<!-- Camera Button with Icon -->
<Button>
    <Button.Content>
        <HorizontalStackLayout Spacing="8">
            <Label Text="&#xE3B0;" 
                   FontFamily="MaterialIcons"
                   FontSize="20"
                   VerticalOptions="Center"/>
            <Label Text="Camera" 
                   VerticalOptions="Center"/>
        </HorizontalStackLayout>
    </Button.Content>
</Button>

<!-- Gallery Button with Icon -->
<Button>
    <Button.Content>
        <HorizontalStackLayout Spacing="8">
            <Label Text="&#xE410;" 
                   FontFamily="MaterialIcons"
                   FontSize="20"
                   VerticalOptions="Center"/>
            <Label Text="Gallery" 
                   VerticalOptions="Center"/>
        </HorizontalStackLayout>
    </Button.Content>
</Button>
```

### Option 3: Use Image Icons

Create small icon images and use `ImageButton`:

```xml
<ImageButton Source="camera_icon.png"
             Command="{Binding TakePhotoCommand}"
             BackgroundColor="{StaticResource Primary}"
             CornerRadius="25"
             WidthRequest="120"
             HeightRequest="50"/>

<ImageButton Source="gallery_icon.png"
             Command="{Binding PickPhotoCommand}"
             BackgroundColor="{StaticResource Secondary}"
             CornerRadius="25"
             WidthRequest="120"
             HeightRequest="50"/>
```

## Quick Fix Instructions

### For Immediate Fix (5 minutes):

1. Open `PrescriptionUploadPage.xaml`
2. Use Find & Replace (Ctrl+H):
   - Find: `?? ` ? Replace with: `` (empty)
   - Find: `?? ` ? Replace with: `` (empty)
   - Find: `??? ` ? Replace with: `` (empty)
   - Find: `?? ` ? Replace with: `` (empty)
   - Find: `?? ` ? Replace with: `` (empty)

3. For the bullet list at the bottom (Tips section), replace with:

```xml
<Label TextColor="{StaticResource PrimaryDark}"
       FontSize="12"
       LineBreakMode="WordWrap">
    <Label.FormattedText>
        <FormattedString>
            <Span Text="• Ensure good lighting"/>
            <Span Text="&#10;• Keep prescription flat"/>
            <Span Text="&#10;• Capture all medication details"/>
            <Span Text="&#10;• Avoid shadows and glare"/>
        </FormattedString>
    </Label.FormattedText>
</Label>
```

### Files to Check and Fix:

1. ? `splash.svg` - Already fixed (no text emojis)
2. ?? `PrescriptionUploadPage.xaml` - Needs fixing
3. Check other XAML files:
   - `HomePage.xaml`
   - `LoginPage.xaml`
   - `MedicationsPage.xaml`
   - `RemindersPage.xaml`
   - `AdherencePage.xaml`
   - `SettingsPage.xaml`

## Testing

After making changes:

1. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build -f net10.0-android
   ```

2. Run on emulator/device

3. Verify buttons show text correctly:
   - ? "Camera" instead of "??Camera"
   - ? "Gallery" instead of "??Upload"  ? This fixes your issue!
   - ? "Process with AI" instead of "??Process"

## Recommended Approach

For **production app**, I recommend **Option 2** (Material Design Icons) because:
- ? Professional look
- ? Consistent across platforms
- ? Scalable vector icons
- ? Industry standard
- ? Thousands of icons available

For **quick testing**, use **Option 1** (Remove emojis) because:
- ? Works immediately
- ? No dependencies
- ? Simple and reliable
- ? Good enough for MVP

## Summary

The "??Upload" issue is caused by the Unicode character `???` (U+1F4F8 camera emoji) not being supported by the default font. 

**Quick Fix**: Replace `Text="??? Gallery"` with `Text="Gallery"`

This will immediately fix the display issue and make your upload button work properly!
