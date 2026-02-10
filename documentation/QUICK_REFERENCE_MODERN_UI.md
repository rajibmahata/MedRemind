# ?? MedRemind Modern UI - Quick Reference Guide

## ? BUILD STATUS: PASSING

---

## ?? Using the New Components

### **1. StatCard - Display Statistics**

```razor
<StatCard Icon="@Icons.Material.Filled.Medication"
         Value="12"
         Label="Active Medications"
         Subtitle="Total medications"
         IconColor="Color.Primary"
         ShowTrend="true"
         TrendValue="5.2" />
```

**Props**:
- `Icon` - Material icon (default: Info)
- `Value` - Main numeric value (string)
- `Label` - Label text
- `Subtitle` - Optional secondary text
- `IconColor` - MudBlazor Color enum
- `ShowTrend` - Show up/down arrow
- `TrendValue` - Percentage change (+/-)

---

### **2. SearchBar - Universal Search**

```razor
<SearchBar Placeholder="Search medications..."
          OnSearch="HandleSearch"
          FullWidth="true" />

@code {
    private async Task HandleSearch(string query)
    {
        // Search logic here
        Console.WriteLine($"Searching for: {query}");
    }
}
```

**Props**:
- `Placeholder` - Placeholder text
- `OnSearch` - EventCallback<string> triggered on search
- `FullWidth` - Boolean, default true

**Features**:
- ? 300ms debouncing
- ? Clear button
- ? Search icon
- ? Rounded design

---

### **3. EmptyState - No Data Placeholder**

```razor
<EmptyState Icon="@Icons.Material.Filled.SearchOff"
           Title="No medications found"
           Message="Start by uploading your first prescription"
           ActionText="Upload Now"
           ActionIcon="@Icons.Material.Filled.Upload"
           OnAction="@(() => Navigation.NavigateTo("/upload"))" />
```

**Props**:
- `Icon` - Material icon
- `Title` - Main heading
- `Message` - Description text
- `ActionText` - Button text (optional)
- `ActionIcon` - Button icon (optional)
- `OnAction` - EventCallback for button click
- `ChildContent` - RenderFragment for custom content

**With Custom Content**:
```razor
<EmptyState Icon="@Icons.Material.Filled.Info"
           Title="Custom Empty State"
           Message="Add your own content below">
    <MudButton Color="Color.Primary">Custom Button 1</MudButton>
    <MudButton Color="Color.Secondary">Custom Button 2</MudButton>
</EmptyState>
```

---

## ?? Using Theme Classes

### **Card Styles**

```razor
@* Stat card with hover effect *@
<div class="stat-card">
    <p>Content here</p>
</div>

@* Medication card with left border *@
<div class="medication-card">
    <p>Medication info</p>
</div>

@* Reminder card with green border *@
<div class="reminder-card">
    <p>Reminder info</p>
</div>

@* Prescription card with rounded corners *@
<div class="prescription-card">
    <p>Prescription info</p>
</div>
```

---

### **Animations**

```razor
@* Fade in animation *@
<div class="animate-fade-in">
    <p>This fades in</p>
</div>

@* Slide up animation *@
<div class="animate-slide-in-up">
    <p>This slides up</p>
</div>

@* Staggered list animation *@
<div class="animate-stagger">
    <div>Item 1 (delays 0.05s)</div>
    <div>Item 2 (delays 0.1s)</div>
    <div>Item 3 (delays 0.15s)</div>
    <div>Item 4 (delays 0.2s)</div>
</div>
```

---

### **Utility Classes**

```razor
@* Glass effect (frosted glass) *@
<div class="glass-effect pa-4">
    <p>Frosted glass effect</p>
</div>

@* Gradient backgrounds *@
<div class="gradient-primary pa-4">
    <p style="color: white;">Primary gradient</p>
</div>

<div class="gradient-success pa-4">
    <p style="color: white;">Success gradient</p>
</div>

@* Gradient text *@
<h2 class="text-gradient">Beautiful Gradient Text</h2>

@* Responsive visibility *@
<div class="hide-mobile">Only visible on desktop</div>
<div class="hide-desktop">Only visible on mobile</div>
```

---

### **Status Badges**

```razor
<span class="status-badge badge-success">Active</span>
<span class="status-badge badge-warning">Pending</span>
<span class="status-badge badge-error">Failed</span>
<span class="status-badge badge-info">Processing</span>
```

---

## ?? Responsive Design

### **Breakpoints**:
```css
xs: 0px - 600px (Mobile)
sm: 600px - 960px (Tablet)
md: 960px - 1280px (Desktop)
lg: 1280px+ (Large Desktop)
```

### **MudBlazor Grid Example**:
```razor
<MudGrid>
    <MudItem xs="12" sm="6" md="4" lg="3">
        @* Full width on mobile, half on tablet, 1/3 on desktop, 1/4 on large *@
        <StatCard Value="12" Label="Medications" />
    </MudItem>
</MudGrid>
```

---

## ?? Theme Variables

### **Access CSS Variables in Razor**:
```razor
<div style="color: var(--primary-color); background: var(--bg-secondary);">
    Themed content
</div>
```

### **Available Variables**:
```css
/* Colors */
--primary-color
--secondary-color
--accent-color
--success-color
--error-color
--warning-color
--info-color

/* Backgrounds */
--bg-primary
--bg-secondary
--bg-tertiary
--bg-hover

/* Text */
--text-primary
--text-secondary
--text-tertiary

/* Borders & Shadows */
--border-color
--shadow-sm
--shadow-md
--shadow-lg
--shadow-xl

/* Radius */
--radius-sm (6px)
--radius-md (8px)
--radius-lg (12px)
--radius-xl (16px)
--radius-full (9999px)
```

---

## ?? Dark Mode (Theme Ready)

The theme system supports dark mode via `[data-theme="dark"]` attribute.

**To enable** (future enhancement):
```razor
<body data-theme="@currentTheme">
```

**Toggle Logic** (example):
```csharp
private string currentTheme = "light";

private void ToggleTheme()
{
    currentTheme = currentTheme == "light" ? "dark" : "light";
    // Save to localStorage
}
```

---

## ?? Quick Examples

### **Example 1: Dashboard Stats Row**

```razor
<MudGrid Class="mb-4">
    <MudItem xs="12" sm="6" md="3">
        <StatCard Icon="@Icons.Material.Filled.Medication"
                 Value="@totalMeds.ToString()"
                 Label="Medications"
                 IconColor="Color.Primary" />
    </MudItem>
    <MudItem xs="12" sm="6" md="3">
        <StatCard Icon="@Icons.Material.Filled.Notifications"
                 Value="@reminders.ToString()"
                 Label="Reminders"
                 IconColor="Color.Info" />
    </MudItem>
    <MudItem xs="12" sm="6" md="3">
        <StatCard Icon="@Icons.Material.Filled.CheckCircle"
                 Value="@adherence%"
                 Label="Adherence"
                 ShowTrend="true"
                 TrendValue="5"
                 IconColor="Color.Success" />
    </MudItem>
    <MudItem xs="12" sm="6" md="3">
        <StatCard Icon="@Icons.Material.Filled.Mic"
                 Value="@voices.ToString()"
                 Label="Voices"
                 IconColor="Color.Secondary" />
    </MudItem>
</MudGrid>
```

---

### **Example 2: Search with Results**

```razor
<MudPaper Class="pa-4">
    <SearchBar Placeholder="Search medications..."
              OnSearch="HandleSearch" />
    
    @if (searchResults.Any())
    {
        <MudList>
            @foreach (var medication in searchResults)
            {
                <MudListItem>
                    ?? @medication.Name - @medication.Dosage
                </MudListItem>
            }
        </MudList>
    }
    else if (searched)
    {
        <EmptyState Icon="@Icons.Material.Filled.SearchOff"
                   Title="No results found"
                   Message="Try a different search term" />
    }
</MudPaper>

@code {
    private List<MedicationDto> searchResults = new();
    private bool searched = false;
    
    private async Task HandleSearch(string query)
    {
        searched = true;
        searchResults = await MedicationService.SearchMedicationsAsync(query);
    }
}
```

---

### **Example 3: Empty State with Action**

```razor
@if (medications.Any())
{
    @* Show medications *@
    @foreach (var med in medications)
    {
        <MudCard>@med.Name</MudCard>
    }
}
else
{
    <EmptyState Icon="@Icons.Material.Filled.MedicalServices"
               Title="No medications yet"
               Message="Upload your first prescription to get started"
               ActionText="Upload Prescription"
               ActionIcon="@Icons.Material.Filled.Upload"
               OnAction="@(() => Navigation.NavigateTo("/upload"))" />
}
```

---

### **Example 4: Animated Card List**

```razor
<MudStack Spacing="2" Class="animate-stagger">
    @foreach (var reminder in reminders)
    {
        <MudCard Elevation="2" Class="reminder-card">
            <MudCardContent>
                <MudText Typo="Typo.h6">@reminder.MedicationName</MudText>
                <MudText Typo="Typo.body2">@reminder.ReminderTime.ToString(@"hh\:mm")</MudText>
            </MudCardContent>
        </MudCard>
    }
</MudStack>
```

---

## ?? Service Usage Examples

### **MedicationService**

```csharp
@inject IMedicationService MedicationService

private List<MedicationDto> medications = new();

protected override async Task OnInitializedAsync()
{
    // Get all medications
    medications = await MedicationService.GetAllMedicationsAsync();
    
    // Search medications
    var results = await MedicationService.SearchMedicationsAsync("aspirin");
    
    // Get by prescription
    var meds = await MedicationService.GetMedicationsByPrescriptionAsync(1);
    
    // Update medication
    var updated = await MedicationService.UpdateMedicationAsync(1, new UpdateMedicationRequest
    {
        Name = "Aspirin Updated",
        Dosage = "100mg",
        Unit = "tablet",
        Frequency = "Once daily",
        Instructions = "Take with food"
    });
    
    // Delete medication
    var success = await MedicationService.DeleteMedicationAsync(1);
}
```

---

### **ReminderService**

```csharp
@inject IReminderService ReminderService

private List<ReminderModel> reminders = new();

protected override async Task OnInitializedAsync()
{
    // Get all reminders
    reminders = await ReminderService.GetUserRemindersAsync();
    
    // Calculate suggested times
    var suggestion = await ReminderService.CalculateReminderTimesAsync(2); // twice daily
    
    // Create bulk reminders
    var success = await ReminderService.CreateMultipleRemindersAsync(new CreateMultipleRemindersModel
    {
        MedicationId = 1,
        VoiceRecordingId = 1,
        ReminderTimes = new List<TimeSpan> { new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0) }
    });
}
```

---

## ?? Common Patterns

### **Pattern 1: Loading State**

```razor
@if (loading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (items.Any())
{
    @* Show items *@
}
else
{
    <EmptyState Title="No items" Message="Get started by adding your first item" />
}
```

---

### **Pattern 2: Error Handling**

```razor
@try
{
    var result = await Service.DoSomethingAsync();
    if (result != null)
    {
        Snackbar.Add("? Success!", Severity.Success);
    }
    else
    {
        Snackbar.Add("Failed to complete action", Severity.Error);
    }
}
catch (Exception ex)
{
    Snackbar.Add($"Error: {ex.Message}", Severity.Error);
}
```

---

### **Pattern 3: Confirmation Dialog**

```razor
@inject IDialogService DialogService

private async Task DeleteItem(int id)
{
    var result = await DialogService.ShowMessageBox(
        "Confirm Delete",
        "Are you sure you want to delete this item?",
        yesText: "Delete",
        cancelText: "Cancel");
    
    if (result == true)
    {
        var success = await Service.DeleteAsync(id);
        if (success)
        {
            Snackbar.Add("Item deleted", Severity.Info);
            await Reload();
        }
    }
}
```

---

## ? Checklist for New Pages

When creating a new page, include:

- [ ] `@page` directive with route
- [ ] `@inject` required services
- [ ] `<PageTitle>` for SEO
- [ ] `<AuthorizeView>` if authentication required
- [ ] Loading state (`@if (loading)`)
- [ ] Empty state (use `<EmptyState>` component)
- [ ] Error handling (`try/catch` with Snackbar)
- [ ] Responsive layout (MudGrid with xs/sm/md/lg)
- [ ] Animations (add `animate-*` classes)
- [ ] Modern card styles (use theme classes)

---

## ?? Start Coding!

```bash
# 1. Start backend
cd backend/MedRemind.API
dotnet run

# 2. Start web
cd web/MedRemind.Web
dotnet watch run

# 3. Open browser
# http://localhost:5001
```

---

*Happy coding! ??*
