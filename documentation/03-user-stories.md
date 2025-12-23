# User Stories - MedRemind MVP

## Epic Structure

### Epic 1: User Authentication & Profile Management
### Epic 2: Prescription Management & AI Reading
### Epic 3: Medication Management
### Epic 4: Reminder & Notification System
### Epic 5: Voice Recording & Playback
### Epic 6: Dashboard & Reporting

---

## Epic 1: User Authentication & Profile Management

### Story 1.1: Phone Number Login
**As a** new user  
**I want to** log in using my phone number  
**So that** I can securely access the app without remembering passwords

**Acceptance Criteria:**
- [ ] User can enter a 10-digit phone number
- [ ] App validates phone number format before sending OTP
- [ ] Error message shown for invalid phone numbers
- [ ] "Send OTP" button is disabled until valid phone number entered
- [ ] Country code selector available (default: +91 for India)
- [ ] Loading indicator shown while OTP is being sent
- [ ] Success message: "OTP sent to +91-XXXXX-XXX10"
- [ ] If OTP fails to send, show error and retry button

**Technical Tasks:**
- [ ] Create LoginPage.xaml and LoginViewModel.cs
- [ ] Implement phone number validation (regex)
- [ ] Integrate 2Factor.in API for OTP sending
- [ ] Create IAuthenticationService interface
- [ ] Implement error handling and retry logic
- [ ] Add unit tests for phone validation

**Estimated Effort:** 8 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 1

---

### Story 1.2: OTP Verification
**As a** user who requested an OTP  
**I want to** enter the 6-digit code I received  
**So that** I can verify my phone number and access the app

**Acceptance Criteria:**
- [ ] User is navigated to OTP entry screen after OTP sent
- [ ] Screen displays masked phone number: "+91-XXXXX-XXX10"
- [ ] Six separate input boxes for 6-digit OTP (auto-focus on next)
- [ ] Auto-submit when all 6 digits entered
- [ ] "Resend OTP" button enabled after 60 seconds countdown
- [ ] Maximum 3 attempts allowed, then block for 15 minutes
- [ ] Success: Navigate to Home or Profile Setup (first time)
- [ ] Failure: Show error "Invalid OTP. X attempts remaining"
- [ ] OTP expires after 10 minutes

**Technical Tasks:**
- [ ] Create OtpVerificationPage.xaml and OtpVerificationViewModel.cs
- [ ] Implement 6-digit input component with auto-focus
- [ ] Add countdown timer for resend button
- [ ] Implement OTP verification via 2Factor.in API
- [ ] Add rate limiting (max 3 attempts)
- [ ] Store session token on success
- [ ] Create User record in database
- [ ] Add unit tests for OTP verification logic

**Estimated Effort:** 8 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 1

---

### Story 1.3: Biometric Authentication Setup
**As a** logged-in user  
**I want to** enable fingerprint or Face ID login  
**So that** I can quickly access the app without entering OTP every time

**Acceptance Criteria:**
- [ ] After first login, prompt: "Enable biometric login for faster access?"
- [ ] Check device biometric availability (fingerprint/Face ID)
- [ ] If not available, skip this step
- [ ] "Enable" button triggers biometric enrollment
- [ ] On success, save preference in secure storage
- [ ] "Skip" or "Not Now" option available
- [ ] Can be enabled later from Settings page
- [ ] Fallback to OTP login if biometric fails

**Technical Tasks:**
- [ ] Create BiometricSetupPage.xaml and BiometricSetupViewModel.cs
- [ ] Integrate Plugin.Fingerprint NuGet package
- [ ] Check biometric availability on iOS and Android
- [ ] Implement biometric authentication flow
- [ ] Store biometric preference in SecureStorage
- [ ] Add fallback to OTP login
- [ ] Create Settings page to toggle biometric
- [ ] Test on physical devices (emulators don't support biometrics well)

**Estimated Effort:** 6 hours  
**Priority:** P1 (High)  
**Sprint:** Sprint 1 - Day 2

---

### Story 1.4: User Profile Creation
**As a** first-time user  
**I want to** complete my profile with basic information  
**So that** the app can personalize my experience

**Acceptance Criteria:**
- [ ] After OTP verification (first time), navigate to Profile Setup
- [ ] Form fields: Name (required), Date of Birth, Gender, Profile Photo
- [ ] Date of Birth picker (calendar UI)
- [ ] Gender dropdown: Male, Female, Other, Prefer not to say
- [ ] Profile photo: Camera or Gallery option
- [ ] "Save" button validates required fields
- [ ] Can skip optional fields
- [ ] Success: Navigate to Home page
- [ ] Profile editable later from Settings

**Technical Tasks:**
- [ ] Create UserProfilePage.xaml and UserProfileViewModel.cs
- [ ] Add User model and UserRepository
- [ ] Create SQLite Users table
- [ ] Implement photo picker (camera/gallery)
- [ ] Save profile photo to app data folder
- [ ] Add form validation
- [ ] Create UserService for CRUD operations
- [ ] Add unit tests for UserService

**Estimated Effort:** 6 hours  
**Priority:** P1 (High)  
**Sprint:** Sprint 1 - Day 2

---

### Story 1.5: Logout and Session Management
**As a** logged-in user  
**I want to** log out of the app  
**So that** I can protect my privacy on shared devices

**Acceptance Criteria:**
- [ ] "Logout" button in Settings page
- [ ] Confirmation dialog: "Are you sure you want to logout?"
- [ ] On confirm: Clear session token, navigate to Login
- [ ] Clear cached data (optional: keep medications locally)
- [ ] Next login requires OTP verification again (unless biometric)
- [ ] Auto-logout after 30 days of inactivity (security)

**Technical Tasks:**
- [ ] Add Logout button in SettingsPage.xaml
- [ ] Implement logout logic in AuthenticationService
- [ ] Clear SecureStorage tokens
- [ ] Reset navigation stack to LoginPage
- [ ] Implement auto-logout timer (background task)
- [ ] Add "Remember Me" option (extend session)
- [ ] Test session persistence across app restarts

**Estimated Effort:** 4 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 1

---

## Epic 2: Prescription Management & AI Reading

### Story 2.1: Upload Prescription Image
**As a** user  
**I want to** upload a photo of my prescription  
**So that** the app can automatically read and extract medication details

**Acceptance Criteria:**
- [ ] "Upload Prescription" button on Home page
- [ ] Two options: "Take Photo" or "Choose from Gallery"
- [ ] Camera opens with photo guidelines overlay (align prescription within frame)
- [ ] After capture, show preview with "Retake" and "Use Photo" buttons
- [ ] Gallery picker allows selecting existing image
- [ ] Show loading indicator: "Processing prescription..."
- [ ] Navigate to Prescription Review page after upload

**Technical Tasks:**
- [ ] Create PrescriptionUploadPage.xaml and PrescriptionUploadViewModel.cs
- [ ] Integrate Plugin.Maui.Camera for camera access
- [ ] Add camera/photo permissions in AndroidManifest.xml and Info.plist
- [ ] Implement photo picker for gallery
- [ ] Optimize and resize image before saving (max 1024x1024)
- [ ] Save image to app data folder /prescriptions/
- [ ] Create Prescription model and PrescriptionRepository
- [ ] Create Prescriptions table in SQLite
- [ ] Add unit tests for image optimization

**Estimated Effort:** 8 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 3

---

### Story 2.2: AI Prescription Reading (Primary Agent)
**As a** user who uploaded a prescription  
**I want to** AI to automatically extract medication details  
**So that** I don't have to manually enter complex information

**Acceptance Criteria:**
- [ ] After image upload, AI processing starts automatically
- [ ] Loading screen shows: "Reading prescription... Please wait"
- [ ] AI extracts: Medicine name, dosage, frequency, duration, instructions
- [ ] AI also extracts: Doctor name, hospital, prescription date
- [ ] Processing completes in < 10 seconds (average)
- [ ] If AI fails, show option: "Enter Manually" or "Retry"
- [ ] Results shown in Prescription Review page

**Technical Tasks:**
- [ ] Create IPrescriptionReaderService interface
- [ ] Implement OpenAIPrescriptionReader using GPT-4 Vision API
- [ ] Create comprehensive prompt for prescription reading
- [ ] Parse JSON response from OpenAI
- [ ] Map extracted data to Medication model
- [ ] Handle API errors (rate limit, timeout, invalid response)
- [ ] Implement retry logic with exponential backoff (Polly)
- [ ] Cache results to avoid re-processing same image
- [ ] Add logging for AI responses (debugging)
- [ ] Create mock service for testing without API calls
- [ ] Add unit tests for response parsing

**Estimated Effort:** 12 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 3-4

---

### Story 2.3: AI Validation Agent
**As a** user  
**I want to** have a second AI agent validate the prescription reading  
**So that** I can trust the extracted information is accurate

**Acceptance Criteria:**
- [ ] Validation agent runs automatically after primary reading
- [ ] Checks medicine names against known drug database (OpenAI knowledge)
- [ ] Validates dosages are within normal ranges
- [ ] Flags suspicious values: "High dosage detected for Aspirin 500mg"
- [ ] Checks for dangerous drug interactions (if multiple medicines)
- [ ] Assigns confidence score: High (90-100%), Medium (70-89%), Low (<70%)
- [ ] Low confidence ? Show warning, suggest manual review
- [ ] Warnings displayed in Prescription Review page

**Technical Tasks:**
- [ ] Create IValidationAgentService interface
- [ ] Implement MedicineValidationAgent using OpenAI
- [ ] Create validation prompt with medical rules
- [ ] Implement dosage range checking logic
- [ ] Drug interaction detection (basic, based on OpenAI knowledge)
- [ ] Calculate confidence score algorithm
- [ ] Store validation results in Prescription record
- [ ] Display warnings in ReviewPage UI
- [ ] Add override option for false positives
- [ ] Add unit tests for validation logic

**Estimated Effort:** 10 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 4-5

---

### Story 2.4: Prescription Review & Confirmation
**As a** user  
**I want to** review and confirm the AI-extracted prescription details  
**So that** I can correct any errors before saving

**Acceptance Criteria:**
- [ ] Display extracted prescription in card format
- [ ] Show confidence score indicator (High/Medium/Low)
- [ ] Each medication shown with: Name, Dosage, Frequency, Duration, Instructions
- [ ] Edit icon on each field to make corrections
- [ ] Show validation warnings prominently (yellow/red background)
- [ ] Checkbox: "I confirm this information is correct"
- [ ] "Save Medications" button (enabled after confirmation)
- [ ] "Cancel" button returns to Home without saving
- [ ] On save: Create Medication records and navigate to Reminder Setup

**Technical Tasks:**
- [ ] Create PrescriptionReviewPage.xaml and PrescriptionReviewViewModel.cs
- [ ] Display prescription data in ListView or CollectionView
- [ ] Implement inline editing for each field
- [ ] Add validation warnings UI (Alert icons, tooltips)
- [ ] Implement confirmation checkbox logic
- [ ] Create SaveMedicationsAsync method
- [ ] Insert Medication records into SQLite
- [ ] Update Prescription status to "Processed"
- [ ] Navigate to Reminder Setup for each medication
- [ ] Add unit tests for save logic

**Estimated Effort:** 8 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 5

---

### Story 2.5: Manual Prescription Entry (Fallback)
**As a** user whose prescription couldn't be read by AI  
**I want to** manually enter medication details  
**So that** I can still use the app

**Acceptance Criteria:**
- [ ] "Enter Manually" button on Prescription Upload page
- [ ] Form fields: Medicine Name, Dosage, Unit (dropdown), Frequency, Duration, Instructions
- [ ] Frequency presets: Once daily, Twice daily, Three times daily, Custom
- [ ] Duration presets: 7 days, 14 days, 30 days, Custom
- [ ] Unit dropdown: Tablet, Capsule, ml, mg, drops, puffs
- [ ] All fields validated before save
- [ ] "Add Another Medicine" button (for multiple medications)
- [ ] "Save" button creates Medication records
- [ ] Navigate to Reminder Setup after save

**Technical Tasks:**
- [ ] Create AddEditMedicationPage.xaml and AddEditMedicationViewModel.cs
- [ ] Design form layout with proper UX
- [ ] Add form validation (required fields, numeric validation)
- [ ] Create frequency and duration pickers
- [ ] Implement "Add Another" functionality (list management)
- [ ] Reuse SaveMedicationsAsync logic
- [ ] Add unit tests for form validation

**Estimated Effort:** 8 hours  
**Priority:** P1 (High)  
**Sprint:** Sprint 1 - Day 5

---

### Story 2.6: View Prescription History
**As a** user  
**I want to** view all my past prescriptions  
**So that** I can refer back to them if needed

**Acceptance Criteria:**
- [ ] "Prescriptions" tab in bottom navigation
- [ ] List view showing: Thumbnail, Doctor name, Date, Status (Processed/Pending)
- [ ] Tap on prescription to view details
- [ ] Detail view shows: Full image (zoomable), Extracted medications, Validation notes
- [ ] Filter options: All, Processed, Pending
- [ ] Sort options: Date (newest first), Doctor name
- [ ] Delete prescription option (with confirmation)

**Technical Tasks:**
- [ ] Create PrescriptionListPage.xaml and PrescriptionListViewModel.cs
- [ ] Create PrescriptionDetailPage.xaml and PrescriptionDetailViewModel.cs
- [ ] Implement GetPrescriptionsAsync in PrescriptionRepository
- [ ] Add filtering and sorting logic
- [ ] Implement image zoom functionality (pinch-to-zoom)
- [ ] Add delete confirmation dialog
- [ ] Cascade delete (medications linked to prescription)
- [ ] Add unit tests for repository methods

**Estimated Effort:** 6 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 2

---

## Epic 3: Medication Management

### Story 3.1: View Active Medications
**As a** user  
**I want to** see all my active medications in one place  
**So that** I can quickly know what I'm currently taking

**Acceptance Criteria:**
- [ ] "Medications" tab in bottom navigation
- [ ] List view showing: Medicine name, Dosage, Frequency, Next dose time
- [ ] Visual indicator: Active (green), Ended (gray), Paused (yellow)
- [ ] Filter: Active, All, Ended
- [ ] Sort: Alphabetical, Next dose time, Start date
- [ ] Empty state: "No medications added. Upload a prescription to get started."
- [ ] Pull-to-refresh to update list

**Technical Tasks:**
- [ ] Create MedicationListPage.xaml and MedicationListViewModel.cs
- [ ] Query active medications from MedicationRepository
- [ ] Implement filtering and sorting
- [ ] Design medication card UI component
- [ ] Add status badge (Active/Ended/Paused)
- [ ] Calculate next dose time logic
- [ ] Implement pull-to-refresh
- [ ] Add unit tests for medication queries

**Estimated Effort:** 6 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 6

---

### Story 3.2: View Medication Details
**As a** user  
**I want to** view detailed information about a specific medication  
**So that** I can review dosage, instructions, and adherence history

**Acceptance Criteria:**
- [ ] Tap on medication card to view details
- [ ] Show: Full name, Generic name, Dosage, Frequency, Timing, Instructions
- [ ] Display: Start date, End date, Days remaining
- [ ] Adherence chart: Taken, Missed, Skipped (last 7 days)
- [ ] List of upcoming reminders
- [ ] "Edit" button to modify details
- [ ] "Pause" button to temporarily stop reminders
- [ ] "Delete" button (with confirmation)

**Technical Tasks:**
- [ ] Create MedicationDetailPage.xaml and MedicationDetailViewModel.cs
- [ ] Query medication with related data (reminders, dose logs)
- [ ] Calculate adherence statistics
- [ ] Implement adherence chart (simple bar chart or progress bars)
- [ ] Add edit navigation
- [ ] Implement pause/resume functionality
- [ ] Add delete confirmation
- [ ] Add unit tests for adherence calculations

**Estimated Effort:** 8 hours  
**Priority:** P1 (High)  
**Sprint:** Sprint 1 - Day 6

---

### Story 3.3: Edit Medication
**As a** user  
**I want to** edit medication details  
**So that** I can correct errors or update dosage changes from doctor

**Acceptance Criteria:**
- [ ] "Edit" button on Medication Detail page
- [ ] Pre-filled form with existing values
- [ ] Can edit: Name, Dosage, Frequency, Duration, Instructions
- [ ] Cannot edit: Start date (informational only)
- [ ] "Save Changes" updates medication record
- [ ] If frequency changed, offer to update reminders: "Reschedule reminders?"
- [ ] Validation: Same as manual entry
- [ ] Success message: "Medication updated successfully"

**Technical Tasks:**
- [ ] Reuse AddEditMedicationPage in edit mode
- [ ] Pre-populate form fields from medication data
- [ ] Implement UpdateMedicationAsync in MedicationService
- [ ] Detect frequency changes
- [ ] Prompt for reminder rescheduling
- [ ] Update related reminders if confirmed
- [ ] Add unit tests for update logic

**Estimated Effort:** 6 hours  
**Priority:** P1 (High)  
**Sprint:** Sprint 2 - Day 2

---

### Story 3.4: Pause/Resume Medication
**As a** user  
**I want to** temporarily pause a medication  
**So that** I don't get reminders while I'm on a short break (e.g., traveling)

**Acceptance Criteria:**
- [ ] "Pause" button on Medication Detail page
- [ ] Confirmation: "Pause reminders for [Medicine Name]?"
- [ ] On pause: Disable all reminders, change status to "Paused"
- [ ] Visual indicator: Yellow badge "Paused"
- [ ] "Resume" button appears when paused
- [ ] On resume: Re-enable reminders, change status to "Active"
- [ ] Pause reason optional: "Why are you pausing?" (for tracking)

**Technical Tasks:**
- [ ] Add IsActive field to Medications table (already exists)
- [ ] Implement PauseMedicationAsync in MedicationService
- [ ] Disable all related reminders
- [ ] Update UI to show paused state
- [ ] Implement ResumeMedicationAsync
- [ ] Re-enable reminders
- [ ] Add optional pause reason field
- [ ] Add unit tests for pause/resume logic

**Estimated Effort:** 4 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 3

---

### Story 3.5: Delete Medication
**As a** user  
**I want to** delete a medication I no longer need  
**So that** my list stays clean and relevant

**Acceptance Criteria:**
- [ ] "Delete" button on Medication Detail page
- [ ] Confirmation dialog: "Are you sure? This will also delete all reminders and history."
- [ ] Checkbox: "I understand this cannot be undone"
- [ ] On confirm: Delete medication, reminders, dose logs
- [ ] Navigate back to Medication List
- [ ] Success message: "Medication deleted"
- [ ] Cannot delete if dose logs exist (soft delete instead)

**Technical Tasks:**
- [ ] Implement DeleteMedicationAsync in MedicationService
- [ ] Cascade delete reminders and dose logs (or soft delete)
- [ ] Add confirmation dialog with checkbox
- [ ] Update UI after deletion
- [ ] Consider soft delete (set DeletedAt timestamp)
- [ ] Add unit tests for delete logic

**Estimated Effort:** 4 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 3

---

## Epic 4: Reminder & Notification System

### Story 4.1: Set Up Medication Reminders
**As a** user who added a medication  
**I want to** set up reminders for when to take it  
**So that** I never miss a dose

**Acceptance Criteria:**
- [ ] After saving medication, navigate to Reminder Setup page
- [ ] Show medication name and frequency (e.g., "Aspirin - Twice daily")
- [ ] Suggest optimal times based on frequency: 
  - Once daily: 09:00 AM
  - Twice daily: 09:00 AM, 09:00 PM
  - Three times daily: 08:00 AM, 02:00 PM, 08:00 PM
- [ ] User can customize each reminder time (time picker)
- [ ] Option: "Remind me before meals" or "Remind me after meals"
- [ ] Option: "Enable voice reminder" (toggle)
- [ ] "Save Reminders" creates reminder records
- [ ] Navigate to Voice Recording if voice enabled

**Technical Tasks:**
- [ ] Create ReminderSetupPage.xaml and ReminderSetupViewModel.cs
- [ ] Implement IReminderSchedulingService
- [ ] Calculate optimal reminder times based on frequency
- [ ] Create time picker component for each reminder
- [ ] Create Reminders table in SQLite
- [ ] Implement SaveRemindersAsync in ReminderService
- [ ] Add unit tests for time calculation logic

**Estimated Effort:** 10 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 7

---

### Story 4.2: Schedule Local Notifications
**As a** user who set up reminders  
**I want to** receive push notifications at the scheduled times  
**So that** I'm alerted to take my medication

**Acceptance Criteria:**
- [ ] Notification appears at exact scheduled time (±1 minute)
- [ ] Notification title: "Time for your medication"
- [ ] Notification body: "[Medicine Name] - [Dosage]"
- [ ] Notification actions: "Taken", "Snooze 10 min", "Dismiss"
- [ ] Notification sound: Custom reminder tone
- [ ] Notification vibration pattern (on Android)
- [ ] Badge count on app icon (iOS)
- [ ] Notification persists until user action
- [ ] Works even if app is closed

**Technical Tasks:**
- [ ] Integrate Plugin.LocalNotification NuGet
- [ ] Create INotificationService interface
- [ ] Implement ScheduleNotificationAsync
- [ ] Set notification channel (Android) with high priority
- [ ] Add custom notification sound (add .mp3 for Android, .aiff for iOS)
- [ ] Implement notification actions (Taken, Snooze, Dismiss)
- [ ] Handle notification tap (open app to medication detail)
- [ ] Request notification permissions (iOS 10+)
- [ ] Test notifications on physical devices
- [ ] Add unit tests for scheduling logic

**Estimated Effort:** 12 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 7-8

---

### Story 4.3: Handle Notification Actions
**As a** user who received a notification  
**I want to** mark the dose as taken, snoozed, or dismissed  
**So that** the app tracks my adherence

**Acceptance Criteria:**
- [ ] Tap "Taken": Log dose as taken, dismiss notification, show checkmark
- [ ] Tap "Snooze": Reschedule notification for 10 minutes later
- [ ] Tap "Dismiss": Dismiss notification (no log entry)
- [ ] If no action for 30 minutes: Log as "Missed"
- [ ] In-app notification history shows all alerts
- [ ] Can manually mark dose as taken from Medication Detail page

**Technical Tasks:**
- [ ] Implement notification action handlers
- [ ] Create DoseLog entry on "Taken" action
- [ ] Implement snooze logic (reschedule notification)
- [ ] Add background task to check for missed doses
- [ ] Create DoseLogs table in SQLite
- [ ] Implement LogDoseAsync in MedicationService
- [ ] Add manual dose logging UI
- [ ] Add unit tests for dose logging logic

**Estimated Effort:** 8 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 8

---

### Story 4.4: View Reminder Schedule
**As a** user  
**I want to** view all my upcoming reminders  
**So that** I can plan my day around medication times

**Acceptance Criteria:**
- [ ] "Reminders" section on Home page
- [ ] Show today's reminders grouped by time
- [ ] Indicate status: Upcoming (gray), Taken (green), Missed (red), Snoozed (yellow)
- [ ] Tap on reminder to view medication details
- [ ] "View All Reminders" button navigates to full list
- [ ] Full list shows next 7 days of reminders
- [ ] Calendar view option (future)

**Technical Tasks:**
- [ ] Add Reminders section to HomePage.xaml
- [ ] Query reminders for today in HomeViewModel
- [ ] Create ReminderListPage.xaml and ReminderListViewModel.cs
- [ ] Query reminders for next 7 days
- [ ] Join with DoseLogs to determine status
- [ ] Design reminder card UI component
- [ ] Add status indicators (color coding)
- [ ] Add unit tests for reminder queries

**Estimated Effort:** 6 hours  
**Priority:** P1 (High)  
**Sprint:** Sprint 2 - Day 4

---

### Story 4.5: Edit Reminder Times
**As a** user  
**I want to** change reminder times  
**So that** I can adjust to my daily routine changes

**Acceptance Criteria:**
- [ ] "Edit Reminders" button on Medication Detail page
- [ ] Show list of all reminders for this medication
- [ ] Each reminder has time picker to change time
- [ ] "Add Reminder" button (for extra doses)
- [ ] "Delete" icon on each reminder
- [ ] "Save Changes" updates reminders
- [ ] Reschedule notifications accordingly
- [ ] Confirm: "Reminder times updated"

**Technical Tasks:**
- [ ] Create EditRemindersPage.xaml and EditRemindersViewModel.cs
- [ ] Display current reminders in editable list
- [ ] Implement time picker for each reminder
- [ ] Add "Add Reminder" functionality
- [ ] Implement delete reminder
- [ ] Update reminder records in database
- [ ] Cancel old notifications and schedule new ones
- [ ] Add unit tests for reminder updates

**Estimated Effort:** 6 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 4

---

## Epic 5: Voice Recording & Playback

### Story 5.1: Record Voice Message
**As a** user  
**I want to** record a voice message for my medication reminder  
**So that** I hear a personal message when it's time to take my medicine

**Acceptance Criteria:**
- [ ] Navigate to Voice Recording page (from Reminder Setup or Settings)
- [ ] Prompt: "Record a voice reminder (up to 30 seconds)"
- [ ] Suggestion text: "It's time to take your [Medicine Name]. Don't forget!"
- [ ] Recording name field: "Mom", "Dad", "Self", "Custom"
- [ ] "Record" button starts recording (show waveform animation)
- [ ] Timer counts up (0:00 to 0:30)
- [ ] "Stop" button stops recording
- [ ] "Play" button to preview recording
- [ ] "Re-record" button to start over
- [ ] "Save" button saves recording and links to reminder
- [ ] Auto-stop at 30 seconds

**Technical Tasks:**
- [ ] Create VoiceRecordingPage.xaml and VoiceRecordingViewModel.cs
- [ ] Integrate Plugin.Maui.Audio for recording
- [ ] Request microphone permissions (Android/iOS)
- [ ] Implement audio recording logic (max 30 seconds)
- [ ] Save audio file to app data folder /voice-recordings/
- [ ] Create VoiceRecordings table in SQLite
- [ ] Implement playback preview
- [ ] Add waveform animation (optional, use Lottie)
- [ ] Add unit tests for recording service

**Estimated Effort:** 10 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 9

---

### Story 5.2: Assign Voice to Reminder
**As a** user who recorded a voice message  
**I want to** assign it to one or more medication reminders  
**So that** I hear this message when the notification fires

**Acceptance Criteria:**
- [ ] After saving voice recording, show list of medications
- [ ] Checkbox for each medication: "Use this voice for [Medicine Name]"
- [ ] Can select multiple medications
- [ ] "Apply to Selected" button updates reminders
- [ ] Show confirmation: "Voice reminder set for X medications"
- [ ] Can change voice later from Edit Reminders page

**Technical Tasks:**
- [ ] Add VoiceRecordingId field to Reminders table (already in schema)
- [ ] Query medications for voice assignment
- [ ] Implement checkbox selection UI
- [ ] Update reminder records with VoiceRecordingId
- [ ] Add unit tests for voice assignment logic

**Estimated Effort:** 4 hours  
**Priority:** P1 (High)  
**Sprint:** Sprint 1 - Day 9

---

### Story 5.3: Play Voice in Notification
**As a** user who enabled voice reminders  
**I want to** hear the voice message when notification arrives  
**So that** I have an emotional connection to the reminder

**Acceptance Criteria:**
- [ ] When notification fires, auto-play voice message
- [ ] Voice plays even if app is closed (platform-specific)
- [ ] Voice plays at system volume (respects Do Not Disturb)
- [ ] On iOS: Voice plays as notification sound (inline audio)
- [ ] On Android: Voice plays when notification expanded
- [ ] Option to disable voice playback in Settings
- [ ] Fallback to standard sound if voice file missing

**Technical Tasks:**
- [ ] Implement voice playback in notification on iOS (UNNotificationSound)
- [ ] Implement voice playback in notification on Android (custom notification with MediaPlayer)
- [ ] Handle Do Not Disturb mode
- [ ] Add fallback to default sound
- [ ] Test on physical devices (critical: emulators don't play sounds well)
- [ ] Add error handling for missing files
- [ ] Add unit tests for playback logic

**Estimated Effort:** 12 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 1 - Day 9-10

---

### Story 5.4: Manage Voice Recordings
**As a** user  
**I want to** view, edit, and delete my voice recordings  
**So that** I can manage my voice library

**Acceptance Criteria:**
- [ ] "Voice Recordings" section in Settings
- [ ] List view: Recording name, Duration, Date created, "Assigned to X medications"
- [ ] Play button to listen to each recording
- [ ] Edit icon to rename recording
- [ ] Delete icon (with confirmation)
- [ ] Cannot delete if assigned to active reminders (or offer to unassign)
- [ ] "Record New" button

**Technical Tasks:**
- [ ] Create VoiceRecordingsListPage.xaml and VoiceRecordingsListViewModel.cs
- [ ] Query voice recordings from VoiceRecordingRepository
- [ ] Count medications using each recording
- [ ] Implement playback in list view
- [ ] Implement rename functionality
- [ ] Implement delete with validation (check if in use)
- [ ] Add unit tests for voice management

**Estimated Effort:** 6 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 5

---

## Epic 6: Dashboard & Reporting

### Story 6.1: Home Dashboard
**As a** user  
**I want to** see an overview of my medications and reminders  
**So that** I can quickly understand my medication status

**Acceptance Criteria:**
- [ ] Top section: "Good morning, [Name]!"
- [ ] Adherence score: "95% adherence this week" (circular progress)
- [ ] Today's reminders section: Upcoming, Taken, Missed
- [ ] Active medications count: "3 active medications"
- [ ] Quick actions: "Upload Prescription", "Add Medication", "View Calendar"
- [ ] Recent activity: Last 3 dose logs
- [ ] Motivational message: "Keep up the great work!" or "Don't forget your evening dose"

**Technical Tasks:**
- [ ] Create HomePage.xaml and HomeViewModel.cs
- [ ] Calculate weekly adherence score
- [ ] Query today's reminders and their status
- [ ] Count active medications
- [ ] Fetch recent dose logs
- [ ] Design dashboard UI with cards
- [ ] Add quick action buttons with navigation
- [ ] Add unit tests for dashboard data calculations

**Estimated Effort:** 10 hours  
**Priority:** P0 (Critical)  
**Sprint:** Sprint 2 - Day 1-2

---

### Story 6.2: Adherence Tracking
**As a** user  
**I want to** see my medication adherence over time  
**So that** I can monitor my health habits

**Acceptance Criteria:**
- [ ] "Adherence" page accessible from Home or Settings
- [ ] Weekly view: Bar chart showing taken vs missed doses
- [ ] Monthly view: Calendar heatmap (green=100%, red=0%)
- [ ] Statistics: 
  - Overall adherence: 92%
  - Doses taken: 64 / 70
  - Missed doses: 6
  - Longest streak: 10 days
- [ ] Filter by medication: "Show adherence for Aspirin only"
- [ ] Export report (PDF or image, future)

**Technical Tasks:**
- [ ] Create AdherencePage.xaml and AdherenceViewModel.cs
- [ ] Query dose logs for date range
- [ ] Calculate adherence percentage
- [ ] Implement bar chart (use Microcharts NuGet or custom)
- [ ] Implement calendar heatmap (custom or third-party)
- [ ] Calculate streak logic
- [ ] Add medication filter dropdown
- [ ] Add unit tests for adherence calculations

**Estimated Effort:** 10 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 5-6

---

### Story 6.3: Medication Calendar View
**As a** user  
**I want to** see my medication schedule in a calendar format  
**So that** I can visualize my weekly routine

**Acceptance Criteria:**
- [ ] Calendar grid showing current week (Mon-Sun)
- [ ] Each day shows: Date, Number of doses, Adherence indicator
- [ ] Tap on day to see detailed schedule
- [ ] Color coding: Green (all taken), Yellow (partial), Red (missed), Gray (future)
- [ ] Navigation: Previous week, Next week
- [ ] Month view option (future)

**Technical Tasks:**
- [ ] Create CalendarViewPage.xaml and CalendarViewModel.cs
- [ ] Generate week grid UI (7 columns)
- [ ] Query reminders and dose logs for week
- [ ] Calculate adherence per day
- [ ] Implement day detail view
- [ ] Add navigation controls (prev/next week)
- [ ] Add unit tests for calendar data

**Estimated Effort:** 8 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 6

---

### Story 6.4: Settings and Preferences
**As a** user  
**I want to** customize app settings  
**So that** the app works best for me

**Acceptance Criteria:**
- [ ] Settings page accessible from bottom navigation
- [ ] Sections:
  - **Account**: Name, Phone, Profile Photo, Edit Profile
  - **Notifications**: Enable/Disable, Sound, Vibration, LED
  - **Reminders**: Default times, Snooze duration, Lead time
  - **Voice**: Manage voice recordings, Default voice
  - **Security**: Enable biometric, Auto-logout timeout
  - **App**: Language, Theme (Light/Dark), Font size
  - **About**: Version, Privacy Policy, Terms, Support
- [ ] Each setting saves immediately (no "Save" button)
- [ ] Logout button at bottom

**Technical Tasks:**
- [ ] Create SettingsPage.xaml and SettingsViewModel.cs
- [ ] Create AppSettings table in SQLite (already in schema)
- [ ] Implement ISettingsService for CRUD operations
- [ ] Design grouped settings list UI
- [ ] Implement toggle switches, pickers, sliders
- [ ] Save settings to database
- [ ] Apply theme change dynamically
- [ ] Add unit tests for settings service

**Estimated Effort:** 8 hours  
**Priority:** P1 (High)  
**Sprint:** Sprint 2 - Day 7

---

### Story 6.5: Onboarding Tutorial
**As a** first-time user  
**I want to** see a quick tutorial  
**So that** I understand how to use the app

**Acceptance Criteria:**
- [ ] Show on first app launch only
- [ ] 4-5 screens:
  1. Welcome: "Never miss your medication"
  2. Upload: "Snap a photo of your prescription"
  3. AI Magic: "AI reads it for you"
  4. Reminders: "Set custom voice reminders"
  5. Track: "Monitor your adherence"
- [ ] "Skip" button on each screen
- [ ] "Next" button to proceed
- [ ] "Get Started" button on last screen (navigate to Login)
- [ ] Don't show again (store in preferences)

**Technical Tasks:**
- [ ] Create OnboardingPage.xaml with CarouselView
- [ ] Design 5 onboarding screens (illustrations needed)
- [ ] Implement skip and next navigation
- [ ] Store "onboarding_completed" flag in AppSettings
- [ ] Check flag on app start
- [ ] Add unit tests for onboarding flow

**Estimated Effort:** 6 hours  
**Priority:** P2 (Medium)  
**Sprint:** Sprint 2 - Day 7

---

## Summary

### Total User Stories: 34
### Total Estimated Effort: 246 hours

### Epic Breakdown:
1. **Authentication & Profile**: 6 stories, 42 hours
2. **Prescription & AI**: 6 stories, 60 hours
3. **Medication Management**: 5 stories, 28 hours
4. **Reminders & Notifications**: 5 stories, 42 hours
5. **Voice Recording**: 4 stories, 32 hours
6. **Dashboard & Reporting**: 5 stories, 42 hours

### Priority Breakdown:
- **P0 (Critical)**: 18 stories, 146 hours
- **P1 (High)**: 9 stories, 62 hours
- **P2 (Medium)**: 7 stories, 38 hours

### Sprint Allocation:
- **Sprint 1 (Week 1)**: Stories 1.1-5.3 (19 stories, 146 hours)
- **Sprint 2 (Week 2)**: Stories 1.5-6.5 (15 stories, 100 hours)

---

**Document Version**: 1.0  
**Last Updated**: December 18, 2024  
**Owner**: Rajib Mahata  
**Status**: Ready for Sprint Planning
