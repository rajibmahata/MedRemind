# Prescription Status Implementation Guide

## Overview
This document explains the implementation of the `PrescriptionStatus` enum system for tracking prescription processing states and duplicate detection.

## Status Enum Definition

### `PrescriptionStatus` Enum
Located in: `backend\MedRemind.Core\Enums\PrescriptionStatus.cs`

```csharp
public enum PrescriptionStatus
{
    Processing = 1,   // Prescription is being processed
    Duplicate = 2,    // Duplicate prescription detected
    Processed = 3,    // Prescription processing completed successfully
    Failed = 4        // Prescription processing failed
}
```

### `OcrProcessingStatus` Enum
Located in: `backend\MedRemind.Core\Enums\OcrProcessingStatus.cs`

```csharp
public enum OcrProcessingStatus
{
    Processing = 1,   // OCR text extraction in progress
    OcrComplete = 2,  // OCR extraction complete, AI processing in progress
    Processed = 3,    // AI processing and validation complete
    Failed = 4,       // Processing failed
    Duplicate = 5     // Duplicate prescription detected, using cached result
}
```

## Data Model

### Prescription Model
The `Prescription.Status` field stores the enum value as a **string** in the database:

```csharp
public class Prescription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Processing"; // Stores enum value as string
    // ... other properties
}
```

**Important:** The Status is stored as a string to avoid database migration issues and maintain backward compatibility.

### PrescriptionOCRResult Model
Uses the `OcrProcessingStatus` enum directly:

```csharp
public class PrescriptionOCRResult
{
    public int Id { get; set; }
    public int PrescriptionId { get; set; }
    public OcrProcessingStatus Status { get; set; } = OcrProcessingStatus.Processing;
    // ... other properties
}
```

## Duplicate Detection Flow

### 1. Check for Duplicate
When a prescription is uploaded, the system checks for duplicates:

```csharp
var duplicateCheck = await _deduplicationService.CheckForDuplicateAsync(ocrText, userId);

if (duplicateCheck.IsDuplicate)
{
    // Return cached result from existing prescription
    var duplicateDetails = await _deduplicationService
        .GetDuplicatePrescriptionDetailsAsync(duplicateCheck.ExistingResult.PrescriptionId);
    
    return duplicateDetails.Medications;
}
```

### 2. Status Update on Duplicate Detection

**Exact Match (100% similarity):**
```csharp
// Update OCR result status
ocrResult.Status = OcrProcessingStatus.Duplicate;

// Update prescription status (convert enum to string)
prescription.Status = PrescriptionStatus.Duplicate.ToString();
```

**Very Similar Match (?95% similarity):**
```csharp
// Same status updates as exact match
prescription.Status = PrescriptionStatus.Duplicate.ToString();
```

### 3. Status Update on Successful Processing
```csharp
// Update OCR result
ocrResult.Status = OcrProcessingStatus.Processed;

// Update prescription
prescription.Status = PrescriptionStatus.Processed.ToString();
prescription.ProcessedAt = DateTime.UtcNow;
```

## Code Usage Patterns

### ? Correct Usage

**Setting Status (Convert enum to string):**
```csharp
prescription.Status = PrescriptionStatus.Processing.ToString();
prescription.Status = PrescriptionStatus.Processed.ToString();
prescription.Status = PrescriptionStatus.Duplicate.ToString();
```

**Checking Status (String comparison):**
```csharp
if (prescription.Status == "Processed")
{
    // Process completed prescriptions
}

// Or with enum conversion
if (prescription.Status == PrescriptionStatus.Processed.ToString())
{
    // Process completed prescriptions
}
```

**Parsing Status from String:**
```csharp
if (Enum.TryParse<PrescriptionStatus>(prescription.Status, out var status))
{
    switch (status)
    {
        case PrescriptionStatus.Processing:
            // Handle processing
            break;
        case PrescriptionStatus.Duplicate:
            // Handle duplicate
            break;
        case PrescriptionStatus.Processed:
            // Handle processed
            break;
    }
}
```

### ? Incorrect Usage

**Don't assign enum directly to string property:**
```csharp
// WRONG - Won't compile
prescription.Status = PrescriptionStatus.Processing;

// CORRECT
prescription.Status = PrescriptionStatus.Processing.ToString();
```

**Don't compare string to enum directly:**
```csharp
// WRONG - Won't compile
if (prescription.Status == PrescriptionStatus.Processed)

// CORRECT
if (prescription.Status == PrescriptionStatus.Processed.ToString())
```

## API Response Structure

### Duplicate Detected Response
```json
{
  "isDuplicate": true,
  "prescriptionId": 123,
  "status": "Duplicate",
  "message": "Exact duplicate found - prescription already processed",
  "similarityScore": 1.0,
  "existingData": {
    "doctorName": "Dr. Smith",
    "patientName": "John Doe",
    "medicationCount": 3,
    "processedAt": "2024-02-04T10:30:00Z",
    "medications": [
      {
        "name": "Aspirin",
        "dosage": "500mg",
        "frequency": "Twice daily"
      }
    ]
  }
}
```

### Normal Processing Response
```json
{
  "isDuplicate": false,
  "prescriptionId": 124,
  "status": "Processed",
  "message": "Prescription processed successfully",
  "medications": [...]
}
```

## Key Classes and Methods

### PrescriptionDeduplicationService

**Main Methods:**
- `CheckForDuplicateAsync(string ocrText, int userId)` - Check for duplicates
- `StoreOCRResultAsync(...)` - Store OCR result and update status
- `GetDuplicatePrescriptionDetailsAsync(int prescriptionId)` - Get full duplicate details
- `GetDuplicatePrescriptionMedicationsAsync(int prescriptionId)` - Get medications only

**DTOs:**
- `DuplicateCheckResult` - Result of duplicate check
- `DuplicatePrescriptionDetails` - Full prescription details with medications
- `SimilarPrescription` - Similar prescription with similarity score

## Database Schema

### Prescriptions Table
```sql
CREATE TABLE Prescriptions (
    Id INTEGER PRIMARY KEY,
    UserId INTEGER NOT NULL,
    Status TEXT DEFAULT 'Processing',  -- Stores: "Processing", "Duplicate", "Processed", "Failed"
    ProcessedAt DATETIME,
    -- ... other columns
);
```

### PrescriptionOCRResults Table
```sql
CREATE TABLE PrescriptionOCRResults (
    Id INTEGER PRIMARY KEY,
    PrescriptionId INTEGER NOT NULL,
    Status INTEGER DEFAULT 1,  -- Stores OcrProcessingStatus enum value
    OCRTextHash TEXT NOT NULL,
    -- ... other columns
);
```

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task ProcessPrescription_WhenDuplicate_ShouldReturnDuplicateStatus()
{
    // Arrange
    var prescription = new Prescription
    {
        UserId = 1,
        Status = PrescriptionStatus.Processing.ToString()
    };
    
    // Act
    var result = await service.ProcessPrescription(prescription);
    
    // Assert
    Assert.Equal("Duplicate", result.Status);
}
```

## Migration Notes

### No Database Migration Required
Since we're storing the status as a string, no database migration is needed. The existing data will work correctly:
- Old records: "Pending", "Processed", "Failed" ? Still valid
- New records: "Processing", "Duplicate", "Processed", "Failed" ? Enum values as strings

### Backward Compatibility
The system maintains backward compatibility with existing string statuses:
- "Pending" can be treated as "Processing"
- Existing "Processed" and "Failed" statuses work as-is

## Best Practices

1. **Always convert enum to string** when assigning to `Prescription.Status`
2. **Use enum for type safety** in code logic
3. **Return duplicate data immediately** to avoid reprocessing
4. **Log duplicate detection** for audit trails
5. **Update both OCR result and prescription statuses** consistently

## Future Enhancements

Potential improvements:
- Add `PendingReview` status for pharmacist review
- Add `Expired` status for old prescriptions
- Add `Archived` status for historical records
- Add `Rejected` status for invalid prescriptions

---

**Last Updated:** 2024-02-04  
**Version:** 1.0
