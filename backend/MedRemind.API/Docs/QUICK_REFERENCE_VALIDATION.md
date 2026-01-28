# ? Quick Reference - Multi-Agent Validation System

Fast reference guide for the multi-agent prescription validation system.

---

## ?? Quick Start

### 1. Upload Prescription (Automatic Validation)
```http
POST /api/prescriptions/upload
Content-Type: multipart/form-data

file: [prescription image]
```

### 2. Check Analysis Report
```powershell
# Location
cd F:\rajibmahata\MedRemind\backend\MedRemind.API\Files\LLMResponses

# View latest report
cat Analysis_prescription_*.json | ConvertFrom-Json
```

---

## ?? Validation Status Levels

| Status | Quality | Agreement | Action |
|--------|---------|-----------|--------|
| **Excellent** | ?90% | ?90% | ? Auto-approve |
| **Good** | ?70% | ?70% | ? Auto-approve |
| **Acceptable** | ?50% | - | ?? Review if issues |
| **NeedsReview** | <50% | <50% | ? Manual review |
| **Failed** | - | - | ? Re-upload |

---

## ?? File Outputs

```
Files/LLMResponses/
??? OpenAI_prescription_1_20260128_193022.json      // OpenAI parser result
??? DeepSeek_prescription_1_20260128_193022.json    // DeepSeek parser result
??? Claude_prescription_1_20260128_193022.json      // Claude parser result
??? Analysis_prescription_1_20260128_193022.json    // ? Validation report
```

---

## ?? Quick Checks

### Check Parser Results
```powershell
# List all parser outputs
dir Files/LLMResponses/OpenAI_*.json
dir Files/LLMResponses/DeepSeek_*.json
dir Files/LLMResponses/Claude_*.json
```

### Check Analysis Reports
```powershell
# List all analysis reports
dir Files/LLMResponses/Analysis_*.json

# View report summary
$report = cat Files/LLMResponses/Analysis_prescription_*.json | ConvertFrom-Json
Write-Host "Status: $($report.finalRecommendation.status)"
Write-Host "Confidence: $($report.finalRecommendation.confidence)"
Write-Host "Manual Review: $($report.finalRecommendation.requiresManualReview)"
```

---

## ?? Key Features

### ? Automatic Validation
- Runs after every prescription upload
- No manual intervention needed
- Results saved automatically

### ? Multi-Parser Comparison
- OpenAI vs DeepSeek vs Claude
- Identifies best parser
- Detects conflicts

### ? Quality Metrics
- OCR quality (0.0 - 1.0)
- Data completeness (0.0 - 1.0)
- Parser consistency (0.0 - 1.0)
- Overall quality score

### ? Issue Detection
- Medication conflicts
- Missing data
- Parser disagreements
- OCR quality problems

---

## ?? Configuration

### Enable/Disable Validation

**Enabled (default):**
```csharp
// Program.cs
builder.Services.AddScoped<PrescriptionValidationAgent>(sp =>
{
    var logger = sp.GetService<ILogger<PrescriptionValidationAgent>>();
    return new PrescriptionValidationAgent(logger);
});
```

**Disabled:**
```csharp
// Pass null to orchestrator
multiAgentValidationAgent: null
```

---

## ?? Report Structure

### Key Fields

```json
{
  "reportId": "guid",
  "prescriptionFileName": "prescription_1_*.jpg",
  "generatedAt": "2026-01-28T19:30:22Z",
  
  "parserResults": [
    {
      "parserName": "OpenAI",
      "medicationCount": 3,
      "averageConfidence": 0.92,
      "completenessScore": 0.95
    }
  ],
  
  "crossValidation": {
    "overallAgreement": 0.92,
    "conflictingMedications": []
  },
  
  "finalRecommendation": {
    "status": "Excellent",
    "confidence": 0.92,
    "requiresManualReview": false
  },
  
  "qualityMetrics": {
    "overallQuality": 0.90
  },
  
  "issues": [],
  "warnings": []
}
```

---

## ?? Build & Deploy

### Build Commands
```powershell
# Build all backend projects
cd F:\rajibmahata\MedRemind\backend
dotnet build

# Build specific project
cd MedRemind.Services
dotnet build

# Run API
cd MedRemind.API
dotnet run
```

### Build Status
```
? MedRemind.Core:     0 errors
? MedRemind.Services: 0 errors
? MedRemind.API:      0 errors
```

---

## ?? Usage Examples

### Example 1: Check Last Upload
```powershell
# Get latest analysis
$latest = dir Files/LLMResponses/Analysis_*.json | 
    Sort-Object LastWriteTime -Descending | 
    Select -First 1

$report = cat $latest.FullName | ConvertFrom-Json

# Display summary
Write-Host "Status: $($report.finalRecommendation.status)"
Write-Host "Issues: $($report.issues.Count)"
Write-Host "Quality: $($report.qualityMetrics.overallQuality * 100)%"
```

### Example 2: Find Problem Prescriptions
```powershell
# Find all prescriptions needing review
$reports = dir Files/LLMResponses/Analysis_*.json

foreach ($file in $reports) {
    $report = cat $file.FullName | ConvertFrom-Json
    if ($report.finalRecommendation.requiresManualReview) {
        Write-Host "$($report.prescriptionFileName) needs review"
        Write-Host "  Reason: $($report.finalRecommendation.reason)"
    }
}
```

### Example 3: Compare Parser Performance
```powershell
$report = cat Files/LLMResponses/Analysis_prescription_*.json | ConvertFrom-Json

foreach ($parser in $report.parserResults) {
    Write-Host "$($parser.parserName):"
    Write-Host "  Medications: $($parser.medicationCount)"
    Write-Host "  Confidence: $($parser.averageConfidence * 100)%"
    Write-Host "  Completeness: $($parser.completenessScore * 100)%"
}
```

---

## ?? Troubleshooting

### Issue: Validation Not Running
**Check:**
1. PrescriptionValidationAgent registered in Program.cs?
2. FileStorageService injected?
3. EnableFileLogging = true in appsettings?

### Issue: No Analysis Reports
**Check:**
1. Files/LLMResponses/ directory exists?
2. Check API logs for errors
3. Verify FileStorageService.SaveAnalysisReportAsync called

### Issue: Low Quality Scores
**Actions:**
1. Re-scan prescription with better quality
2. Ensure good lighting
3. Check OCR text quality
4. Verify prescription is readable

---

## ?? Documentation

- **Full Guide:** `MULTI_AGENT_VALIDATION_SYSTEM.md`
- **Date Parsing:** `DATE_PARSING_TEST_VALIDATION_GUIDE.md`
- **Build Status:** `BUILD_STATUS.md`
- **Quick Fix:** `QUICK_FIX_DATE_PARSING.md`

---

## ? Summary

**System Status:** ? Operational  
**Build Status:** ? 0 errors  
**Validation:** ? Automatic  
**Reports:** ? Saved to Files/LLMResponses/  

**Quick Actions:**
1. Upload prescription ? Automatic validation
2. Check `Analysis_*.json` ? Review results
3. Status "Excellent" or "Good" ? Auto-approved
4. Status "NeedsReview" ? Manual review

**The multi-agent validation system is ready! ??**
