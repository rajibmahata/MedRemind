# Prescription Upload UI Testing Guide

## Overview

This document provides comprehensive UI testing scenarios for the Prescription Upload feature in MedRemind mobile application.

---

## Test Project Structure

```
backend/MedRemind.UITests/
??? MedRemind.UITests.csproj
??? ViewModels/
?   ??? PrescriptionUploadViewModelTests.cs
??? Integration/
?   ??? PrescriptionUploadIntegrationTests.cs
??? README.md (this file)
```

---

## Running Tests

### From Visual Studio

1. Open Test Explorer: Test ? Test Explorer
2. Click "Run All" to execute all tests
3. View results in Test Explorer window

### From Command Line

```bash
cd backend/MedRemind.UITests
dotnet test
```

### With Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Test Categories

### 1. Unit Tests (ViewModels)

**File**: `ViewModels/PrescriptionUploadViewModelTests.cs`

#### Test Cases

| Test Name | Description | Expected Result |
|-----------|-------------|-----------------|
| `ViewModel_ShouldInitialize_WithDefaultValues` | Verify initial state | All properties are default/empty |
| `ProcessPrescription_WithValidImage_ShouldExtractMedications` | Process valid prescription | Medications extracted successfully |
| `ProcessPrescription_WithNoImage_ShouldShowError` | Process without image | Error message displayed |
| `ProcessPrescription_WithAPIError_ShouldHandleGracefully` | Handle API failures | Graceful error handling |
| `ClearData_ShouldResetAllProperties` | Clear all data | All properties reset |
| `SaveMedications_WithEmptyList_ShouldShowError` | Save empty list | Error shown, no DB call |
| `SaveMedications_WithValidData_ShouldSaveAndNavigate` | Save valid medications | DB save and navigation |

### 2. Integration Tests

**File**: `Integration/PrescriptionUploadIntegrationTests.cs`

#### Test Cases

| Test Name | Description | Expected Result |
|-----------|-------------|-----------------|
| `CompleteUploadFlow_FromImageToDatabaseSave_ShouldSucceed` | End-to-end flow | Complete success |
| `PrescriptionWithWarnings_ShouldDisplayWarningsCorrectly` | Validation warnings | Warnings displayed |
| `MedicationData_Validation_ShouldEnforceRequiredFields` | Data validation | Required fields enforced |
| `FrequencyParsing_ShouldMapCorrectly` | Frequency parsing | Correct frequency count |
| `LowConfidenceScore_ShouldTriggerWarning` | Low confidence | Warning triggered |

---

## Manual UI Test Cases

### Test Suite 1: Image Selection

#### TC-001: Camera Capture
**Pre-conditions**: Camera permission granted

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Upload page | Page loads successfully |
| 2 | Tap "?? Camera" button | Camera opens |
| 3 | Take photo of prescription | Photo captured |
| 4 | Confirm photo | Preview displays |
| 5 | Verify success alert | "Photo Selected" alert shows |

#### TC-002: Gallery Selection
**Pre-conditions**: Storage permission granted

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Upload page | Page loads successfully |
| 2 | Tap "??? Gallery" button | Gallery opens |
| 3 | Select prescription image | Image selected |
| 4 | Verify preview | Preview displays |
| 5 | Verify success alert | "Photo Selected" alert shows |

#### TC-003: Permission Denied
**Pre-conditions**: Permissions not granted

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "?? Camera" | Permission prompt appears |
| 2 | Deny permission | Error message displayed |
| 3 | Verify button state | Button remains enabled |
| 4 | Retry after granting | Camera works |

---

### Test Suite 2: AI Processing

#### TC-004: Successful Processing
**Pre-conditions**: Valid prescription image selected

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "?? Process with AI" | Loading indicator shows |
| 2 | Wait for processing | Processing completes |
| 3 | Verify medications list | 1+ medications displayed |
| 4 | Verify confidence score | Score displayed (0-100%) |
| 5 | Verify success message | "? All medications validated" |

#### TC-005: Processing Failure
**Pre-conditions**: Invalid image selected OR no internet

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "?? Process with AI" | Loading indicator shows |
| 2 | Wait for timeout/error | Error message displayed |
| 3 | Verify error details | Specific error shown |
| 4 | Verify retry option | Can try again |

#### TC-006: Low Confidence Score
**Pre-conditions**: Unclear prescription image

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Process unclear image | Processing completes |
| 2 | Verify confidence score | Score < 70% |
| 3 | Check warning message | Warning displayed |
| 4 | Verify edit options | Can edit medications |

---

### Test Suite 3: Medication Editing

#### TC-007: Edit Single Medication
**Pre-conditions**: Medications extracted

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "Edit" on medication | Edit prompts appear |
| 2 | Change medication name | Name updated |
| 3 | Change dosage | Dosage updated |
| 4 | Change frequency | Frequency updated |
| 5 | Change duration | Duration updated |
| 6 | Verify changes | All changes reflected |

#### TC-008: Edit Validation
**Pre-conditions**: Editing medication

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Enter empty name | Validation error |
| 2 | Enter invalid dosage | Validation error |
| 3 | Enter negative duration | Validation error |
| 4 | Cancel edit | Changes discarded |

---

### Test Suite 4: Saving Medications

#### TC-009: Save All Medications
**Pre-conditions**: Valid medications extracted

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "?? Save All" | Saving process starts |
| 2 | Wait for save | Success alert shows |
| 3 | Verify count | "Saved X medication(s)" |
| 4 | Verify navigation | Navigates to Medications page |
| 5 | Verify medications | All meds appear in list |

#### TC-010: Save with Reminders
**Pre-conditions**: Medications with frequency set

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Save medications | Save completes |
| 2 | Navigate to Reminders | Reminders page opens |
| 3 | Verify reminders created | Reminders match frequency |
| 4 | Check reminder times | Times are reasonable |

#### TC-011: Save Failure
**Pre-conditions**: Database issue OR invalid data

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Attempt save | Error occurs |
| 2 | Verify error message | Clear error shown |
| 3 | Verify data retained | Data not lost |
| 4 | Retry after fix | Save succeeds |

---

### Test Suite 5: Error Handling

#### TC-012: No Image Selected
**Pre-conditions**: No image selected

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "Process" directly | Error message shown |
| 2 | Verify message | "Please select a photo" |
| 3 | Verify button state | Process button enabled |

#### TC-013: Network Error
**Pre-conditions**: No internet connection

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Turn off internet | Internet disabled |
| 2 | Select image | Image selected OK |
| 3 | Tap "Process" | Loading starts |
| 4 | Wait for timeout | Network error shown |
| 5 | Turn on internet | Internet enabled |
| 6 | Retry | Processing succeeds |

#### TC-014: API Key Error
**Pre-conditions**: Invalid API key configured

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Process prescription | API error occurs |
| 2 | Verify error message | "API error" displayed |
| 3 | Check logs | API error logged |

---

### Test Suite 6: User Experience

#### TC-015: Tips Display
**Pre-conditions**: None

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Scroll to tips section | Tips visible |
| 2 | Read tips | 4 tips displayed |
| 3 | Verify icons | Checkmarks present |

#### TC-016: Clear Data
**Pre-conditions**: Prescription processed

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Tap "?? Try Again" | Confirmation if needed |
| 2 | Confirm clear | All data cleared |
| 3 | Verify image removed | Preview gone |
| 4 | Verify medications cleared | List empty |
| 5 | Verify state reset | Back to initial state |

#### TC-017: Navigation
**Pre-conditions**: None

| Step | Action | Expected Result |
|------|--------|-----------------|
| 1 | Navigate to Upload page | Page loads |
| 2 | Tap back button | Returns to Home |
| 3 | Navigate again | State preserved OR reset |
| 4 | Complete flow | Saves successfully |

---

## Performance Tests

### Test Suite 7: Performance

#### TC-018: Image Load Time
**Measurement**: Time from selection to preview display

| Image Size | Expected Time | Acceptable Range |
|------------|---------------|------------------|
| 1 MB | < 0.5 sec | 0.2-1 sec |
| 5 MB | < 1 sec | 0.5-2 sec |
| 10 MB | < 2 sec | 1-3 sec |

#### TC-019: AI Processing Time
**Measurement**: Time from tap to results display

| Complexity | Expected Time | Acceptable Range |
|------------|---------------|------------------|
| 1 medication | 3 sec | 2-5 sec |
| 3 medications | 5 sec | 3-8 sec |
| 5+ medications | 8 sec | 5-12 sec |

#### TC-020: Save Performance
**Measurement**: Time to save all medications

| Medication Count | Expected Time | Acceptable Range |
|------------------|---------------|------------------|
| 1-3 meds | < 1 sec | 0.5-2 sec |
| 4-6 meds | < 2 sec | 1-3 sec |
| 7+ meds | < 3 sec | 2-5 sec |

---

## Regression Test Checklist

Run these tests before each release:

- [ ] Camera capture works on Android 11-14
- [ ] Gallery selection works on Android 11-14
- [ ] Permission prompts appear correctly
- [ ] AI processing extracts medications
- [ ] Confidence scores display correctly
- [ ] Edit functionality works for all fields
- [ ] Save creates medications in database
- [ ] Save creates reminders correctly
- [ ] Navigation works after save
- [ ] Try Again clears all data
- [ ] Error messages display properly
- [ ] No crashes during any operation
- [ ] Memory usage is acceptable
- [ ] Battery usage is reasonable
- [ ] App works offline (for saved data)

---

## Test Environment

### Required Setup

1. **Android Device/Emulator**:
   - Android 11+ (API 30+)
   - Camera enabled
   - Internet connection

2. **Test Data**:
   - 5+ sample prescription images (varying quality)
   - Clear prescription images
   - Unclear/low-quality images
   - Multiple medications per prescription

3. **Configuration**:
   - Valid OpenAI API key
   - Database initialized
   - Permissions granted

---

## Bug Reporting Template

```markdown
### Bug Title
[Brief description]

### Steps to Reproduce
1. 
2. 
3. 

### Expected Result


### Actual Result


### Environment
- Device: 
- Android Version: 
- App Version: 

### Screenshots
[Attach screenshots]

### Logs
```
[Paste relevant logs]
```

### Severity
- [ ] Critical (App crash)
- [ ] High (Feature broken)
- [ ] Medium (Workaround exists)
- [ ] Low (Minor issue)
```

---

## Continuous Testing

### Automated Test Run Schedule

- **On commit**: Unit tests only
- **On PR**: Unit + Integration tests
- **Nightly**: Full test suite + performance tests
- **Pre-release**: Regression + manual testing

### Test Coverage Goals

- Unit Tests: > 80%
- Integration Tests: > 60%
- E2E Tests: Critical paths covered

---

## Contact

For questions about testing:
- Review test documentation
- Check test code comments
- Contact QA team

---

**Last Updated**: December 22, 2024  
**Version**: 1.0  
**Status**: Ready for Testing
