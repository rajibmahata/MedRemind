using MedRemind.Core.DTOs;
using MedRemind.Services.AI;
using Xunit;

namespace MedRemind.Tests.Services;

public class MedicineValidationAgentTests
{
    private readonly MedicineValidationAgent _validationAgent;

    public MedicineValidationAgentTests()
    {
        _validationAgent = new MedicineValidationAgent();
    }

    [Fact]
    public async Task ValidateMedicationsAsync_WithValidMedicine_ShouldReturnNoWarnings()
    {
        // Arrange
        var medications = new List<MedicationData>
        {
            new MedicationData
            {
                Name = "Paracetamol",
                Dosage = "500",
                Unit = "mg",
                Frequency = "Twice daily",
                FrequencyCount = 2,
                DurationDays = 7,
                ConfidenceScore = 0.95
            }
        };

        // Act
        var warnings = await _validationAgent.ValidateMedicationsAsync(medications);

        // Assert
        Assert.Empty(warnings);
    }

    [Fact]
    public async Task ValidateMedicationsAsync_WithInvalidMedicineName_ShouldReturnWarning()
    {
        // Arrange
        var medications = new List<MedicationData>
        {
            new MedicationData
            {
                Name = "InvalidMedicineName123",
                Dosage = "500",
                Unit = "mg",
                Frequency = "Once daily",
                FrequencyCount = 1,
                DurationDays = 7,
                ConfidenceScore = 0.5
            }
        };

        // Act
        var warnings = await _validationAgent.ValidateMedicationsAsync(medications);

        // Assert
        Assert.Single(warnings);
        Assert.Equal("InvalidName", warnings[0].WarningType);
        Assert.Equal("High", warnings[0].Severity);
    }

    [Fact]
    public async Task ValidateMedicationsAsync_WithUnusualDosage_ShouldReturnWarning()
    {
        // Arrange
        var medications = new List<MedicationData>
        {
            new MedicationData
            {
                Name = "Paracetamol",
                Dosage = "5000", // Unusually high dosage
                Unit = "mg",
                Frequency = "Once daily",
                FrequencyCount = 1,
                DurationDays = 7,
                ConfidenceScore = 0.8
            }
        };

        // Act
        var warnings = await _validationAgent.ValidateMedicationsAsync(medications);

        // Assert
        Assert.Contains(warnings, w => w.WarningType == "UnusualDosage");
        Assert.Equal("Medium", warnings.First(w => w.WarningType == "UnusualDosage").Severity);
    }

    [Fact]
    public async Task ValidateMedicationsAsync_WithDrugInteraction_ShouldReturnWarning()
    {
        // Arrange
        var medications = new List<MedicationData>
        {
            new MedicationData
            {
                Name = "Warfarin",
                Dosage = "5",
                Unit = "mg",
                Frequency = "Once daily",
                FrequencyCount = 1,
                DurationDays = 30,
                ConfidenceScore = 0.9
            },
            new MedicationData
            {
                Name = "Aspirin",
                Dosage = "75",
                Unit = "mg",
                Frequency = "Once daily",
                FrequencyCount = 1,
                DurationDays = 30,
                ConfidenceScore = 0.9
            }
        };

        // Act
        var warnings = await _validationAgent.ValidateMedicationsAsync(medications);

        // Assert
        Assert.Contains(warnings, w => w.WarningType == "DrugInteraction");
        Assert.Contains(warnings, w => w.Severity == "High");
    }

    [Theory]
    [InlineData("Paracetamol", "500", "mg", 1.0)]
    [InlineData("Ibuprofen", "400", "mg", 1.0)]
    [InlineData("Amoxicillin", "500", "mg", 1.0)]
    public void CalculateConfidenceScore_WithValidMedication_ShouldReturnHighScore(
        string name, string dosage, string unit, double expectedMinScore)
    {
        // Arrange
        var medication = new MedicationData
        {
            Name = name,
            Dosage = dosage,
            Unit = unit,
            Frequency = "Once daily",
            FrequencyCount = 1,
            DurationDays = 7
        };

        // Act
        var score = _validationAgent.CalculateConfidenceScore(medication);

        // Assert
        Assert.True(score >= expectedMinScore);
    }

    [Fact]
    public void CalculateConfidenceScore_WithInvalidName_ShouldReturnLowerScore()
    {
        // Arrange
        var medication = new MedicationData
        {
            Name = "InvalidMed",
            Dosage = "500",
            Unit = "mg",
            Frequency = "Once daily",
            FrequencyCount = 1,
            DurationDays = 7
        };

        // Act
        var score = _validationAgent.CalculateConfidenceScore(medication);

        // Assert
        Assert.True(score < 1.0);
        Assert.True(score <= 0.7); // Should be reduced by at least 0.3
    }

    [Fact]
    public void CalculateConfidenceScore_WithUnusualFrequency_ShouldReduceScore()
    {
        // Arrange
        var medication = new MedicationData
        {
            Name = "Paracetamol",
            Dosage = "500",
            Unit = "mg",
            Frequency = "Five times daily",
            FrequencyCount = 5, // Unusual frequency
            DurationDays = 7
        };

        // Act
        var score = _validationAgent.CalculateConfidenceScore(medication);

        // Assert
        Assert.True(score < 1.0);
    }

    [Fact]
    public void CalculateConfidenceScore_WithLongDuration_ShouldReduceScore()
    {
        // Arrange
        var medication = new MedicationData
        {
            Name = "Paracetamol",
            Dosage = "500",
            Unit = "mg",
            Frequency = "Once daily",
            FrequencyCount = 1,
            DurationDays = 120 // Very long duration
        };

        // Act
        var score = _validationAgent.CalculateConfidenceScore(medication);

        // Assert
        Assert.True(score < 1.0);
    }

    [Fact]
    public async Task ValidateMedicationsAsync_WithMultipleIssues_ShouldReturnMultipleWarnings()
    {
        // Arrange
        var medications = new List<MedicationData>
        {
            new MedicationData
            {
                Name = "UnknownMed",
                Dosage = "9999",
                Unit = "mg",
                Frequency = "Once daily",
                FrequencyCount = 1,
                DurationDays = 7,
                ConfidenceScore = 0.3
            }
        };

        // Act
        var warnings = await _validationAgent.ValidateMedicationsAsync(medications);

        // Assert
        Assert.True(warnings.Count >= 2); // Should have both InvalidName and UnusualDosage warnings
        Assert.Contains(warnings, w => w.WarningType == "InvalidName");
        Assert.Contains(warnings, w => w.WarningType == "UnusualDosage");
    }

    [Theory]
    [InlineData("5", "Tablet", false)]
    [InlineData("3", "Tablet", false)]
    [InlineData("6", "Tablet", true)]
    [InlineData("25", "ml", false)]
    [InlineData("35", "ml", true)]
    [InlineData("500", "mg", false)]
    [InlineData("1500", "mg", true)]
    public async Task ValidateMedicationsAsync_DosageValidation_ShouldDetectUnusualDosages(
        string dosage, string unit, bool shouldHaveWarning)
    {
        // Arrange
        var medications = new List<MedicationData>
        {
            new MedicationData
            {
                Name = "Paracetamol",
                Dosage = dosage,
                Unit = unit,
                Frequency = "Once daily",
                FrequencyCount = 1,
                DurationDays = 7,
                ConfidenceScore = 0.9
            }
        };

        // Act
        var warnings = await _validationAgent.ValidateMedicationsAsync(medications);

        // Assert
        if (shouldHaveWarning)
        {
            Assert.Contains(warnings, w => w.WarningType == "UnusualDosage");
        }
        else
        {
            Assert.DoesNotContain(warnings, w => w.WarningType == "UnusualDosage");
        }
    }
}
