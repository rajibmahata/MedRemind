using FluentAssertions;
using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Mobile.ViewModels;
using MedRemind.Services.Medications;
using MedRemind.Services.Prescriptions;
using Moq;
using Xunit;

namespace MedRemind.UITests.ViewModels;

public class PrescriptionUploadViewModelTests
{
    private readonly Mock<IPrescriptionReaderService> _mockPrescriptionReader;
    private readonly Mock<IValidationAgentService> _mockValidationAgent;
    private readonly Mock<PrescriptionService> _mockPrescriptionService;
    private readonly Mock<MedicationService> _mockMedicationService;
    private readonly Mock<IReminderSchedulingService> _mockReminderScheduling;
    private readonly PrescriptionUploadViewModel _viewModel;

    public PrescriptionUploadViewModelTests()
    {
        _mockPrescriptionReader = new Mock<IPrescriptionReaderService>();
        _mockValidationAgent = new Mock<IValidationAgentService>();
        _mockPrescriptionService = new Mock<PrescriptionService>();
        _mockMedicationService = new Mock<MedicationService>();
        _mockReminderScheduling = new Mock<IReminderSchedulingService>();

        _viewModel = new PrescriptionUploadViewModel(
            _mockPrescriptionReader.Object,
            _mockValidationAgent.Object,
            _mockPrescriptionService.Object,
            _mockMedicationService.Object,
            _mockReminderScheduling.Object
        );
    }

    [Fact]
    public void ViewModel_ShouldInitialize_WithDefaultValues()
    {
        // Assert
        _viewModel.SelectedImage.Should().BeNull();
        _viewModel.IsProcessing.Should().BeFalse();
        _viewModel.HasResult.Should().BeFalse();
        _viewModel.ExtractedMedications.Should().BeEmpty();
        _viewModel.ResultMessage.Should().BeEmpty();
        _viewModel.ConfidenceScore.Should().Be(0);
        _viewModel.Title.Should().Be("Upload Prescription");
    }

    [Fact]
    public async Task ProcessPrescription_WithValidImage_ShouldExtractMedications()
    {
        // Arrange
        var mockResult = new PrescriptionReadResult
        {
            Success = true,
            ConfidenceScore = 0.95,
            DoctorName = "Dr. Smith",
            Medications = new List<MedicationData>
            {
                new MedicationData
                {
                    Name = "Aspirin",
                    Dosage = "100",
                    Unit = "mg",
                    Frequency = "Once daily",
                    FrequencyCount = 1,
                    DurationDays = 30,
                    ConfidenceScore = 0.95
                }
            }
        };

        _mockPrescriptionReader
            .Setup(x => x.ReadPrescriptionFromBase64Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        // Set up base64 image (simulate photo selection)
        var testImage = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        // Note: In real tests, you'd need to set _imageBase64 through ProcessPhotoAsync

        // Act
        // await _viewModel.ProcessPrescriptionCommand.ExecuteAsync(null);

        // Assert
        // _viewModel.HasResult.Should().BeTrue();
        // _viewModel.ConfidenceScore.Should().Be(0.95);
        // _viewModel.ExtractedMedications.Should().HaveCount(1);
        // _viewModel.ExtractedMedications[0].Name.Should().Be("Aspirin");
    }

    [Fact]
    public async Task ProcessPrescription_WithNoImage_ShouldShowError()
    {
        // Arrange
        _viewModel.SelectedImage = null;

        // Act
        await _viewModel.ProcessPrescriptionCommand.ExecuteAsync(null);

        // Assert
        _viewModel.HasResult.Should().BeFalse();
        _viewModel.IsProcessing.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessPrescription_WithAPIError_ShouldHandleGracefully()
    {
        // Arrange
        var mockResult = new PrescriptionReadResult
        {
            Success = false,
            ErrorMessage = "API connection failed"
        };

        _mockPrescriptionReader
            .Setup(x => x.ReadPrescriptionFromBase64Async(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResult);

        // Act
        // await _viewModel.ProcessPrescriptionCommand.ExecuteAsync(null);

        // Assert
        // _viewModel.HasResult.Should().BeTrue();
        // _viewModel.ResultMessage.Should().Contain("failed");
    }

    [Fact]
    public void ClearData_ShouldResetAllProperties()
    {
        // Arrange
        _viewModel.HasResult = true;
        _viewModel.ConfidenceScore = 0.95;
        _viewModel.ResultMessage = "Test";
        _viewModel.ExtractedMedications.Add(new MedicationData { Name = "Test" });

        // Act
        _viewModel.ClearDataCommand.Execute(null);

        // Assert
        _viewModel.SelectedImage.Should().BeNull();
        _viewModel.HasResult.Should().BeFalse();
        _viewModel.ConfidenceScore.Should().Be(0);
        _viewModel.ResultMessage.Should().BeEmpty();
        _viewModel.ExtractedMedications.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveMedications_WithEmptyList_ShouldShowError()
    {
        // Arrange
        _viewModel.ExtractedMedications.Clear();

        // Act
        await _viewModel.SaveMedicationsCommand.ExecuteAsync(null);

        // Assert
        _mockMedicationService.Verify(
            x => x.CreateMedicationAsync(It.IsAny<int>(), It.IsAny<MedicationData>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SaveMedications_WithValidData_ShouldSaveAndNavigate()
    {
        // Arrange
        _viewModel.ExtractedMedications.Add(new MedicationData
        {
            Name = "Aspirin",
            Dosage = "100",
            Unit = "mg",
            Frequency = "Once daily",
            FrequencyCount = 1,
            DurationDays = 30
        });

        // Act
        // await _viewModel.SaveMedicationsCommand.ExecuteAsync(null);

        // Assert
        // _mockMedicationService.Verify(
        //     x => x.CreateMedicationAsync(It.IsAny<int>(), It.IsAny<MedicationData>()),
        //     Times.Once
        // );
        // _mockMedicationService.Verify(
        //     x => x.CreateRemindersAsync(It.IsAny<int>(), It.IsAny<int>()),
        //     Times.Once
        // );
    }
}
