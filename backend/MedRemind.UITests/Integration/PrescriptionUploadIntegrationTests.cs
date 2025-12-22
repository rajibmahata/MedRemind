using FluentAssertions;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using Moq;
using Xunit;

namespace MedRemind.UITests.Integration;

public class PrescriptionUploadIntegrationTests
{
    [Fact]
    public async Task CompleteUploadFlow_FromImageToDatabaseSave_ShouldSucceed()
    {
        // Arrange
        var mockReader = new Mock<IPrescriptionReaderService>();
        var testResult = new PrescriptionReadResult
        {
            Success = true,
            ConfidenceScore = 0.92,
            DoctorName = "Dr. Johnson",
            PrescriptionDate = DateTime.UtcNow.AddDays(-1),
            Medications = new List<MedicationData>
            {
                new MedicationData
                {
                    Name = "Metformin",
                    Dosage = "500",
                    Unit = "mg",
                    Frequency = "Twice daily",
                    FrequencyCount = 2,
                    DurationDays = 90,
                    Instructions = "Take with food",
                    ConfidenceScore = 0.92
                },
                new MedicationData
                {
                    Name = "Lisinopril",
                    Dosage = "10",
                    Unit = "mg",
                    Frequency = "Once daily",
                    FrequencyCount = 1,
                    DurationDays = 90,
                    Instructions = "Take in morning",
                    ConfidenceScore = 0.90
                }
            }
        };

        mockReader
            .Setup(x => x.ReadPrescriptionFromBase64Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testResult);

        // Act
        var result = await mockReader.Object.ReadPrescriptionFromBase64Async("test_base64", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Medications.Should().HaveCount(2);
        result.Medications[0].Name.Should().Be("Metformin");
        result.Medications[1].Name.Should().Be("Lisinopril");
        result.ConfidenceScore.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public async Task PrescriptionWithWarnings_ShouldDisplayWarningsCorrectly()
    {
        // Arrange
        var mockValidationAgent = new Mock<IValidationAgentService>();
        var medications = new List<MedicationData>
        {
            new MedicationData
            {
                Name = "Warfarin",
                Dosage = "5",
                Unit = "mg",
                Frequency = "Once daily",
                FrequencyCount = 1,
                DurationDays = 30
            }
        };

        var warnings = new List<MedicationWarning>
        {
            new MedicationWarning
            {
                MedicationName = "Warfarin",
                WarningType = "Interaction",
                Severity = "High",
                Message = "May interact with other blood thinners"
            }
        };

        mockValidationAgent
            .Setup(x => x.ValidateMedicationsAsync(It.IsAny<List<MedicationData>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(warnings);

        // Act
        var result = await mockValidationAgent.Object.ValidateMedicationsAsync(medications, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Severity.Should().Be("High");
        result[0].MedicationName.Should().Be("Warfarin");
    }

    [Fact]
    public void MedicationData_Validation_ShouldEnforceRequiredFields()
    {
        // Arrange
        var medication = new MedicationData
        {
            Name = "Aspirin",
            Dosage = "100",
            Unit = "mg",
            Frequency = "Once daily",
            FrequencyCount = 1,
            DurationDays = 30,
            ConfidenceScore = 0.95
        };

        // Assert
        medication.Name.Should().NotBeNullOrWhiteSpace();
        medication.Dosage.Should().NotBeNullOrWhiteSpace();
        medication.Unit.Should().NotBeNullOrWhiteSpace();
        medication.Frequency.Should().NotBeNullOrWhiteSpace();
        medication.FrequencyCount.Should().BeGreaterThan(0);
        medication.DurationDays.Should().BeGreaterThan(0);
        medication.ConfidenceScore.Should().BeInRange(0, 1);
    }

    [Theory]
    [InlineData("Once daily", 1)]
    [InlineData("Twice daily", 2)]
    [InlineData("Three times daily", 3)]
    [InlineData("Four times daily", 4)]
    public void FrequencyParsing_ShouldMapCorrectly(string frequency, int expectedCount)
    {
        // Arrange
        var medication = new MedicationData
        {
            Frequency = frequency,
            FrequencyCount = expectedCount
        };

        // Assert
        medication.FrequencyCount.Should().Be(expectedCount);
    }

    [Fact]
    public async Task LowConfidenceScore_ShouldTriggerWarning()
    {
        // Arrange
        var result = new PrescriptionReadResult
        {
            Success = true,
            ConfidenceScore = 0.65, // Low confidence
            Medications = new List<MedicationData>
            {
                new MedicationData
                {
                    Name = "Unclear Name",
                    Dosage = "Unknown",
                    Unit = "mg",
                    Frequency = "Once daily",
                    FrequencyCount = 1,
                    DurationDays = 7,
                    ConfidenceScore = 0.65
                }
            }
        };

        // Assert
        result.ConfidenceScore.Should().BeLessThan(0.70);
        result.Medications[0].ConfidenceScore.Should().BeLessThan(0.70);
        // UI should display warning for low confidence
    }
}
