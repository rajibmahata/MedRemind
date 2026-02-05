# Prescription Duplicate Mapping Feature

## Overview
When a duplicate prescription is detected, the system now automatically maps the new prescription to the original prescription using the `MappedPrescriptionId` field. This creates a clear reference chain showing which prescriptions are duplicates of each other.

## Database Schema

### Prescriptions Table - New Column

```sql
MappedPrescriptionId INTEGER NULL
```

- **Type**: Nullable Integer (Foreign Key reference to Prescriptions.Id)
- **Purpose**: References the original prescription ID when this is a duplicate
- **Populated**: Only when `Status = 'Duplicate'`
- **Indexed**: Yes (for performance)

### Example Data

| Id  | UserId | Status     | MappedPrescriptionId | ImagePath          | CreatedAt           |
|-----|--------|------------|----------------------|--------------------|---------------------|
| 101 | 5      | Processed  | NULL                 | /files/img_101.jpg | 2024-02-01 10:00:00 |
| 102 | 5      | Duplicate  | 101                  | /files/img_102.jpg | 2024-02-01 11:30:00 |
| 103 | 5      | Duplicate  | 101                  | /files/img_103.jpg | 2024-02-01 14:20:00 |

In this example:
- Prescription #101 is the original
- Prescriptions #102 and #103 are duplicates, both mapped to #101

## Workflow

### 1. Prescription Upload
```
User uploads prescription image
    ?
Create Prescription record (Status = "Processing")
    ?
Extract OCR text
    ?
Check for duplicates (pass new prescription ID)
```

### 2. Duplicate Detection Logic

#### Exact Match (100% similarity)
```csharp
var duplicateCheck = await _deduplicationService.CheckForDuplicateAsync(
    ocrText, 
    userId, 
    newPrescriptionId: prescription.Id  // Pass new prescription ID
);

if (duplicateCheck.IsDuplicate)
{
    // Deduplication service automatically:
    // 1. Sets newPrescription.Status = "Duplicate"
    // 2. Sets newPrescription.MappedPrescriptionId = originalPrescriptionId
    // 3. Sets newPrescription.ProcessedAt = DateTime.UtcNow
    // 4. Returns existing prescription data
}
```

#### Similar Match (?95% similarity)
Same as exact match - automatically maps to original prescription.

### 3. Response to Client
```json
{
  "success": true,
  "isDuplicate": true,
  "prescriptionId": 102,
  "mappedPrescriptionId": 101,
  "status": "Duplicate",
  "similarityScore": 1.0,
  "message": "Exact duplicate found - prescription already processed",
  "existingData": {
    "prescriptionId": 101,
    "doctorName": "Dr. Smith",
    "medications": [...]
  }
}
```

## Code Changes

### 1. Prescription Model
```csharp
public class Prescription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = "Processing";
    public int? MappedPrescriptionId { get; set; } // NEW: Maps to original prescription
    // ... other properties
}
```

### 2. CheckForDuplicateAsync Method
```csharp
public async Task<DuplicateCheckResult> CheckForDuplicateAsync(
    string ocrText, 
    int userId, 
    int? newPrescriptionId = null)  // NEW: Receives new prescription ID
{
    // Find duplicate...
    
    if (exactMatch != null && newPrescriptionId.HasValue)
    {
        var newPrescription = await _context.Prescriptions.FindAsync(newPrescriptionId.Value);
        if (newPrescription != null)
        {
            newPrescription.Status = PrescriptionStatus.Duplicate.ToString();
            newPrescription.MappedPrescriptionId = exactMatch.PrescriptionId; // Map to original
            newPrescription.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
```

### 3. PrescriptionReaderService
```csharp
// Create prescription first
var prescription = await _prescriptionService.AddPrescriptionAsync(new Prescription
{
    UserId = userId,
    Status = "Processing"
});

// Pass prescription ID to duplicate check
var duplicateCheck = await _deduplicationService.CheckForDuplicateAsync(
    ocrText, 
    userId, 
    prescription.Id  // Pass new prescription ID
);

if (duplicateCheck.IsDuplicate)
{
    // Status and MappedPrescriptionId already set by deduplication service
    // Just update additional fields
    prescription.DoctorName = existingData.Doctor?.Name;
    await _unitOfWork.SaveChangesAsync();
}
```

## Database Migration

### Migration Script
Located: `backend\MedRemind.API\database_scripts\AddMappedPrescriptionIdColumn.sql`

```sql
-- Add MappedPrescriptionId column
ALTER TABLE Prescriptions
ADD MappedPrescriptionId INTEGER NULL;

-- Add index for performance
CREATE INDEX IF NOT EXISTS IX_Prescriptions_MappedPrescriptionId 
ON Prescriptions(MappedPrescriptionId);

CREATE INDEX IF NOT EXISTS IX_Prescriptions_Status_MappedPrescriptionId 
ON Prescriptions(Status, MappedPrescriptionId);
```

### Running the Migration

**PowerShell:**
```powershell
.\run-database-migrations.ps1 -SingleScript "AddMappedPrescriptionIdColumn.sql"
```

**Batch:**
```cmd
run-database-migrations.bat
```

## Querying Duplicates

### Find all duplicates of a prescription
```sql
SELECT * FROM Prescriptions 
WHERE MappedPrescriptionId = 101;
```

### Find original prescription from duplicate
```sql
SELECT p.* 
FROM Prescriptions p
WHERE p.Id = (
    SELECT MappedPrescriptionId 
    FROM Prescriptions 
    WHERE Id = 102
);
```

### Count duplicates per user
```sql
SELECT 
    UserId,
    COUNT(*) as DuplicateCount
FROM Prescriptions
WHERE Status = 'Duplicate'
GROUP BY UserId;
```

### Get duplicate chain (original + all duplicates)
```sql
WITH RECURSIVE DuplicateChain AS (
    -- Original prescription
    SELECT Id, UserId, Status, MappedPrescriptionId, 0 as Level
    FROM Prescriptions
    WHERE Id = 101
    
    UNION ALL
    
    -- All duplicates
    SELECT p.Id, p.UserId, p.Status, p.MappedPrescriptionId, dc.Level + 1
    FROM Prescriptions p
    INNER JOIN DuplicateChain dc ON p.MappedPrescriptionId = dc.Id
)
SELECT * FROM DuplicateChain ORDER BY Level;
```

## Benefits

### 1. Clear Audit Trail
- Every duplicate prescription has a clear reference to the original
- Easy to trace back to source prescription
- Helps identify users who frequently upload duplicates

### 2. Data Integrity
- Prevents orphaned duplicate prescriptions
- Maintains referential relationship between original and duplicates
- Enables cleanup of duplicate prescriptions

### 3. Analytics
- Track duplicate detection rate
- Identify patterns in duplicate uploads
- Measure deduplication service effectiveness

### 4. User Experience
- Show users which prescription is the original
- Allow users to view original prescription from duplicate
- Provide context about when original was processed

## API Response Changes

### Before (No Mapping)
```json
{
  "isDuplicate": true,
  "message": "Duplicate found",
  "existingPrescriptionId": 101
}
```

### After (With Mapping)
```json
{
  "isDuplicate": true,
  "prescriptionId": 102,  // NEW prescription ID
  "mappedPrescriptionId": 101,  // Original prescription ID
  "status": "Duplicate",
  "message": "Exact duplicate found - prescription already processed",
  "existingPrescriptionId": 101,
  "similarityScore": 1.0
}
```

## Testing

### Unit Test Example
```csharp
[Fact]
public async Task CheckForDuplicate_WhenDuplicateFound_ShouldMapToOriginal()
{
    // Arrange
    var originalPrescription = await CreatePrescription(userId: 1, ocrText: "test text");
    var newPrescription = await CreatePrescription(userId: 1, ocrText: "test text");
    
    // Act
    var result = await _deduplicationService.CheckForDuplicateAsync(
        "test text", 
        userId: 1, 
        newPrescriptionId: newPrescription.Id
    );
    
    // Assert
    Assert.True(result.IsDuplicate);
    
    var updatedPrescription = await _context.Prescriptions.FindAsync(newPrescription.Id);
    Assert.Equal("Duplicate", updatedPrescription.Status);
    Assert.Equal(originalPrescription.Id, updatedPrescription.MappedPrescriptionId);
}
```

## Best Practices

1. **Always pass prescription ID** when calling `CheckForDuplicateAsync`
2. **Don't manually update Status** after duplicate check - it's done automatically
3. **Use MappedPrescriptionId** for querying original prescription data
4. **Index the column** for better query performance
5. **Validate MappedPrescriptionId** exists before displaying to users

## Troubleshooting

### Issue: MappedPrescriptionId is NULL for duplicates
**Solution**: Ensure you're passing `newPrescriptionId` parameter to `CheckForDuplicateAsync`

### Issue: Duplicate not showing mapped ID
**Solution**: Check that the deduplication service has access to the Prescriptions DbSet

### Issue: Status not updating to "Duplicate"
**Solution**: Verify that `SaveChangesAsync()` is called after setting status

---

**Last Updated:** 2024-02-04  
**Version:** 1.1
