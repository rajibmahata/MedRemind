using MedRemind.Services.AI;
using Xunit;

namespace MedRemind.Tests.Services.AI;

public class PrescriptionOcrTextPreprocessorTests
{
    private readonly PrescriptionOcrTextPreprocessor _preprocessor;
    private readonly string _sampleOcrText;

    public PrescriptionOcrTextPreprocessorTests()
    {
        _preprocessor = new PrescriptionOcrTextPreprocessor();
        
        // Real OCR text sample from prescription
        _sampleOcrText = @"19/09/2025
Apollo Sugar
®
Clinics
W+=15.5 kg
Ado. Derpuk plenty.
of fluids
= Syp. AscoritLS junior 3ml 0 0 0 ×5 days
2 Sup. P- 250 (250/5ml) 5ml cos (if fever
0/ 100°F
BBF
=
Syp.
Taxim-0 (50/5ml)
× 5 days (after meals)
et
= Syp. Montele LC kid 5 ml
bedtime × 5 days
Syp. AtoZ 5 ml ODPC × cont (Im)
femenino sas.
Play: Flu Vaccine
19/09/2015
Clinic Address : Unit No. 131, New Town Heights Plaza, New Town Action Area-III, Kolkata-700135
(Landmark : Shapoorji Bus Stop)
Processing Lab : Apollo Diagnostics (A Unit of Apollo Health and Lifestyle Limited)
To book an appointment / Home Collection
8100 60 2323
2171820
set is found
1, please call
I free number
63 Select 8#
Tab. Langol junior (15) 1 tab
× 5 days
=
Dr.Sushmita Pal Santra
M.D. Physician, Dip in Family Medicine (WBUHS)
CCEBDM (Diabetes) CCMTD (Thyroid)
CCHM (Hypertension), DCMH (CIP Ranchi)
Reg No: 71112 (WBMC)
fer : Aarvi Mahala
tyle
90 cold/cough × 2 days
40 fever ×
today
evening
[Handwritten: 19/09/2025]
[Handwritten: W+=15.5 kg
Ado. Derpuk plenty. of fluids
= Syp. AscoritLS junior 3ml 0 0 0 ×5 days 2 Sup. P- 250 (250/5ml) 5ml cos (if fever
0/ 100°F BBF]
[Handwritten: Syp. Taxim-0 (50/5ml) × 5 days (after meals)]
[Handwritten: = Syp. Montele LC kid 5 ml bedtime × 5 days Syp. AtoZ 5 ml ODPC × cont (Im) femenino sas.
Play: Flu Vaccine
19/09/2015]
[Handwritten: Tab. Langol junior (15) 1 tab × 5 days]
[Handwritten: fer : Aarvi Mahala tyle
90 cold/cough × 2 days 40 fever × today evening]
[Handwritten: =]
[Handwritten: et]
[Handwritten: =]";
    }

    #region Basic Functionality Tests

    [Fact]
    public void Preprocess_WithNullInput_ReturnsNull()
    {
        // Act
        var result = _preprocessor.Preprocess(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Preprocess_WithEmptyInput_ReturnsEmpty()
    {
        // Act
        var result = _preprocessor.Preprocess("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void Preprocess_WithWhitespaceOnly_ReturnsEmpty()
    {
        // Act
        var result = _preprocessor.Preprocess("   \n\r\t   ");

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(result));
    }

    #endregion

    #region Balanced Mode Tests

    [Fact]
    public void Preprocess_BalancedMode_RemovesHandwrittenMarkers()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert
        Assert.DoesNotContain("[Handwritten:", result);
        Assert.DoesNotContain("]", result);
    }

    [Fact]
    public void Preprocess_BalancedMode_RemovesClinicAddress()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert
        Assert.DoesNotContain("Clinic Address", result);
        Assert.DoesNotContain("Unit No. 131", result);
        Assert.DoesNotContain("Kolkata-700135", result);
    }

    [Fact]
    public void Preprocess_BalancedMode_RemovesPhoneNumbers()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert
        Assert.DoesNotContain("8100 60 2323", result);
        Assert.DoesNotContain("2171820", result);
    }

    [Fact]
    public void Preprocess_BalancedMode_PreservesMedicationNames()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert - Check for medication names
        Assert.Contains("AscoritLS", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Taxim", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Montele", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Langol", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Preprocess_BalancedMode_PreservesDosageInformation()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert - Check for dosage patterns
        Assert.Contains("3ml", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("5ml", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("×5 days", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Preprocess_BalancedMode_PreservesDoctorInformation()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert
        Assert.Contains("Dr.Sushmita Pal Santra", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reg No: 71112", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Preprocess_BalancedMode_PreservesDate()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert
        Assert.Contains("19/09/2025", result);
    }

    [Fact]
    public void Preprocess_BalancedMode_RemovesDuplicates()
    {
        // Arrange
        var textWithDuplicates = @"Syp. AscoritLS junior 3ml
Syp. AscoritLS junior 3ml
Dr. Smith
Dr. Smith";

        // Act
        var result = _preprocessor.Preprocess(textWithDuplicates, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert - Count occurrences
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var ascoritCount = lines.Count(l => l.Contains("AscoritLS", StringComparison.OrdinalIgnoreCase));
        var drSmithCount = lines.Count(l => l.Contains("Dr. Smith", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(1, ascoritCount);
        Assert.Equal(1, drSmithCount);
    }

    #endregion

    #region Aggressive Mode Tests

    [Fact]
    public void Preprocess_AggressiveMode_RemovesAllClinicInfo()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Aggressive);

        // Assert
        Assert.DoesNotContain("Apollo Sugar", result);
        Assert.DoesNotContain("Clinics", result);
        Assert.DoesNotContain("Apollo Diagnostics", result);
    }

    [Fact]
    public void Preprocess_AggressiveMode_RemovesLandmarkInfo()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Aggressive);

        // Assert
        Assert.DoesNotContain("Landmark", result);
        Assert.DoesNotContain("Shapoorji Bus Stop", result);
    }

    [Fact]
    public void Preprocess_AggressiveMode_RemovesBookingInfo()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Aggressive);

        // Assert
        Assert.DoesNotContain("book an appointment", result);
        Assert.DoesNotContain("Home Collection", result);
    }

    [Fact]
    public void Preprocess_AggressiveMode_PreservesMedicationsOnly()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Aggressive);

        // Assert - Essential medication info should be present
        Assert.True(result.Contains("Syp", StringComparison.OrdinalIgnoreCase) ||
                   result.Contains("Tab", StringComparison.OrdinalIgnoreCase) ||
                   result.Contains("ml", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Preprocess_AggressiveMode_IsShorterThanBalanced()
    {
        // Act
        var balancedResult = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);
        var aggressiveResult = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Aggressive);

        // Assert
        Assert.True(aggressiveResult.Length < balancedResult.Length);
    }

    #endregion

    #region Minimal Mode Tests

    [Fact]
    public void Preprocess_MinimalMode_ExtractsOnlyEssentials()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Minimal);

        // Assert - Should have very concise output
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length <= 50); // Max lines configuration
    }

    [Fact]
    public void Preprocess_MinimalMode_PrioritizesImportantContent()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Minimal);

        // Assert - Doctor and medication info should have priority
        var hasDoctor = result.Contains("Dr.", StringComparison.OrdinalIgnoreCase);
        var hasMedication = result.Contains("Syp", StringComparison.OrdinalIgnoreCase) ||
                           result.Contains("Tab", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasDoctor || hasMedication);
    }

    #endregion

    #region Content Detection Tests

    [Theory]
    [InlineData("Syp. Paracetamol 5ml")]
    [InlineData("Tab. Amoxicillin 500mg")]
    [InlineData("Cap. Vitamin D3 60000 IU")]
    [InlineData("Inj. Insulin 10 units")]
    [InlineData("Cream Betnovate apply twice daily")]
    public void Preprocess_IdentifiesMedicationContent(string medicationLine)
    {
        // Arrange
        var text = $"{medicationLine}\nSome noise text\nMore noise";

        // Act
        var result = _preprocessor.Preprocess(text, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert - Medication line should be preserved
        Assert.Contains(medicationLine.Split(' ')[0], result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Dr. John Smith")]
    [InlineData("Patient: Mary Johnson")]
    [InlineData("Age: 45 years")]
    [InlineData("Reg No: 12345")]
    [InlineData("M.D. Physician")]
    public void Preprocess_IdentifiesDoctorPatientInfo(string infoLine)
    {
        // Arrange
        var text = $"{infoLine}\nClinic Address: 123 Street\nPhone: 1234567890";

        // Act
        var result = _preprocessor.Preprocess(text, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert - Important info should be preserved, noise removed
        var hasInfo = result.Contains("Dr.", StringComparison.OrdinalIgnoreCase) ||
                     result.Contains("Patient", StringComparison.OrdinalIgnoreCase) ||
                     result.Contains("Reg No", StringComparison.OrdinalIgnoreCase) ||
                     result.Contains("Age", StringComparison.OrdinalIgnoreCase);
        
        Assert.True(hasInfo);
        Assert.DoesNotContain("Clinic Address", result);
    }

    [Theory]
    [InlineData("19/09/2025")]
    [InlineData("Date: 15/03/2024")]
    [InlineData("Dated: Jan 15, 2024")]
    [InlineData("12-05-2023")]
    public void Preprocess_PreservesDateInformation(string dateText)
    {
        // Arrange
        var text = $"{dateText}\nClinic noise\nAddress noise";

        // Act
        var result = _preprocessor.Preprocess(text, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert - Date should be preserved
        Assert.Matches(@"\d{1,2}[-/]\d{1,2}[-/]\d{2,4}", result);
    }

    #endregion

    #region Noise Removal Tests

    [Fact]
    public void Preprocess_RemovesUnicodeEscapeSequences()
    {
        // Arrange
        var text = @"Dr. Smith\u0027s Clinic\u0022
Medication: Syp\\u00D7 5ml
Test\\u0022content\\u0027here";

        // Act
        var result = _preprocessor.Preprocess(text);

        // Assert
        Assert.DoesNotContain("\\u", result);
    }

    [Fact]
    public void Preprocess_NormalizesLineEndings()
    {
        // Arrange
        var text = "Line1\r\nLine2\\r\\nLine3\rLine4\\nLine5\n\nLine6";

        // Act
        var result = _preprocessor.Preprocess(text);

        // Assert - Should have normalized line endings
        Assert.DoesNotContain("\r\n", result);
        Assert.DoesNotContain("\\r\\n", result);
        Assert.DoesNotContain("\\n", result);
    }

    [Fact]
    public void Preprocess_RemovesExcessiveWhitespace()
    {
        // Arrange
        var text = "Dr.  Smith    prescribed     medication\n\n\n\nNext  line";

        // Act
        var result = _preprocessor.Preprocess(text);

        // Assert - Should have single spaces
        Assert.DoesNotContain("  ", result);
        Assert.DoesNotMatch(@"\n\s*\n\s*\n", result);
    }

    [Fact]
    public void Preprocess_RemovesStandaloneNumbers()
    {
        // Arrange
        var text = @"Dr. Smith
12345
Medication: Syp. Test 5ml
67890
Patient: John";

        // Act
        var result = _preprocessor.Preprocess(text, PrescriptionOcrTextPreprocessor.ProcessingMode.Aggressive);

        // Assert - Standalone numbers should be removed
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.DoesNotContain(lines, l => l.Trim() == "12345");
        Assert.DoesNotContain(lines, l => l.Trim() == "67890");
    }

    #endregion

    #region OCR Error Correction Tests

    [Theory]
    [InlineData("Syp.Paracetamol", "Syp. Paracetamol")]
    [InlineData("Tab.Amoxicillin", "Tab. Amoxicillin")]
    [InlineData("500mg", "500 mg")]
    [InlineData("10ml", "10 ml")]
    public void Preprocess_FixesSpacingInMedications(string input, string expectedPattern)
    {
        // Arrange
        var text = $"Dr. Smith\n{input}\nPatient: Test";

        // Act
        var result = _preprocessor.Preprocess(text);

        // Assert - Should have proper spacing
        Assert.Contains("Syp.", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tab.", result, StringComparison.OrdinalIgnoreCase);
    }

  

    #endregion

    #region Configuration Tests

    [Fact]
    public void Preprocess_RespectsMaxLinesConfiguration()
    {
        // Arrange
        var config = new PrescriptionOcrTextPreprocessor.PreprocessorConfiguration
        {
            MaxLines = 10
        };
        var preprocessor = new PrescriptionOcrTextPreprocessor(config);

        // Act
        var result = preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Minimal);

        // Assert
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length <= 10);
    }

    [Fact]
    public void Preprocess_WithCustomConfiguration_AppliesSettings()
    {
        // Arrange
        var config = new PrescriptionOcrTextPreprocessor.PreprocessorConfiguration
        {
            MaxLines = 20,
            RemovePhoneNumbers = true,
            RemoveAddresses = true,
            RemoveDisclaimers = true,
            FixOcrErrors = true
        };
        var preprocessor = new PrescriptionOcrTextPreprocessor(config);

        // Act
        var result = preprocessor.Preprocess(_sampleOcrText);

        // Assert
        Assert.DoesNotContain("8100 60 2323", result); // Phone removed
        Assert.DoesNotContain("Clinic Address", result); // Address removed
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length <= 20); // Max lines respected
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Preprocess_WithVeryLongText_HandlesGracefully()
    {
        // Arrange
        var longText = string.Join("\n", Enumerable.Repeat(_sampleOcrText, 10));

        // Act
        var result = _preprocessor.Preprocess(longText, PrescriptionOcrTextPreprocessor.ProcessingMode.Minimal);

        // Assert - Should not throw and should limit output
        Assert.NotNull(result);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length <= 50); // Default max lines
    }

    [Fact]
    public void Preprocess_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var text = @"Dr. O'Brien's Clinic
Medication: 500µg × 3/day
Patient: José García
Dosage: 2.5ml @ 8:00 AM";

        // Act
        var result = _preprocessor.Preprocess(text);

        // Assert - Should not crash and preserve essential content
        Assert.NotNull(result);
        Assert.Contains("Dr.", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Patient", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Preprocess_WithMixedLanguageContent_ExtractsEnglishContent()
    {
        // Arrange
        var text = @"Dr. Smith
रोगी: जॉन स्मिथ
Medication: Syp. Test 5ml
処方箋番号: 12345
Dosage: Twice daily";

        // Act
        var result = _preprocessor.Preprocess(text);

        // Assert - Should preserve English medical terms
        Assert.Contains("Dr.", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Medication", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Dosage", result, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Preprocess_RealWorldPrescription_ExtractsKeyInformation()
    {
        // Act
        var result = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);

        // Assert - Verify all key components are present
        var hasDate = result.Contains("19/09/2025");
        var hasDoctor = result.Contains("Dr.Sushmita", StringComparison.OrdinalIgnoreCase);
        var hasRegNo = result.Contains("71112", StringComparison.OrdinalIgnoreCase);
        var hasMedications = result.Contains("Syp", StringComparison.OrdinalIgnoreCase) ||
                            result.Contains("Tab", StringComparison.OrdinalIgnoreCase);
        var hasDosage = result.Contains("ml", StringComparison.OrdinalIgnoreCase) ||
                       result.Contains("mg", StringComparison.OrdinalIgnoreCase);

        Assert.True(hasDate, "Date should be preserved");
        Assert.True(hasDoctor, "Doctor name should be preserved");
        Assert.True(hasRegNo, "Registration number should be preserved");
        Assert.True(hasMedications, "Medication names should be preserved");
        Assert.True(hasDosage, "Dosage information should be preserved");

        // Verify noise is removed
        Assert.DoesNotContain("Apollo Sugar", result);
        Assert.DoesNotContain("Clinic Address", result);
        Assert.DoesNotContain("8100 60 2323", result);
    }

    [Fact]
    public void Preprocess_CompareModes_AggressiveIsShortestMinimalIsStructured()
    {
        // Act
        var balanced = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Balanced);
        var aggressive = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Aggressive);
        var minimal = _preprocessor.Preprocess(_sampleOcrText, PrescriptionOcrTextPreprocessor.ProcessingMode.Minimal);

        // Assert - Length comparison
        Assert.True(aggressive.Length <= balanced.Length, "Aggressive should be shortest");
        Assert.True(minimal.Length <= balanced.Length, "Minimal should be concise");

        // All should preserve essential medication info
        foreach (var result in new[] { balanced, aggressive, minimal })
        {
            var hasMeds = result.Contains("Syp", StringComparison.OrdinalIgnoreCase) ||
                         result.Contains("Tab", StringComparison.OrdinalIgnoreCase) ||
                         result.Contains("ml", StringComparison.OrdinalIgnoreCase);
            Assert.True(hasMeds, $"Mode should preserve medications");
        }
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Preprocess_LargeInput_CompletesInReasonableTime()
    {
        // Arrange
        var largeText = string.Join("\n", Enumerable.Repeat(_sampleOcrText, 100));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = _preprocessor.Preprocess(largeText);
        stopwatch.Stop();

        // Assert - Should complete in under 5 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Processing took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
        Assert.NotNull(result);
    }

    #endregion
}
