using MedRemind.Core.Data;
using MedRemind.Core.DTOs;
using MedRemind.Core.Models;
using MedRemind.Services.Validation;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace MedRemind.Tests;

public class ValidationWorkflowServiceTests
{
    private readonly Mock<IRepository<PrescriptionValidationWorkflow>> _mockWorkflowRepo;
    private readonly Mock<IRepository<MedicationValidation>> _mockValidationRepo;
    private readonly Mock<IRepository<Prescription>> _mockPrescriptionRepo;
    private readonly Mock<IRepository<Medication>> _mockMedicationRepo;
    private readonly Mock<IRepository<PrescriptionOCRResult>> _mockOcrRepo;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ValidationWorkflowService _service;

    public ValidationWorkflowServiceTests()
    {
        _mockWorkflowRepo = new Mock<IRepository<PrescriptionValidationWorkflow>>();
        _mockValidationRepo = new Mock<IRepository<MedicationValidation>>();
        _mockPrescriptionRepo = new Mock<IRepository<Prescription>>();
        _mockMedicationRepo = new Mock<IRepository<Medication>>();
        _mockOcrRepo = new Mock<IRepository<PrescriptionOCRResult>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _service = new ValidationWorkflowService(
            _mockWorkflowRepo.Object,
            _mockValidationRepo.Object,
            _mockPrescriptionRepo.Object,
            _mockMedicationRepo.Object,
            _mockOcrRepo.Object,
            _mockUnitOfWork.Object
        );
    }

    [Fact]
    public async Task CreateValidationWorkflow_Should_Create_Workflow_And_Validations()
    {
        // Arrange
        var userId = 1;
        var prescriptionId = 1;
        var medications = new List<Medication>
        {
            new Medication { Id = 1, Name = "Aspirin", UserId = userId, PrescriptionId = prescriptionId, SafetyScore = 0.9 },
            new Medication { Id = 2, Name = "Metformin", UserId = userId, PrescriptionId = prescriptionId, SafetyScore = 0.5 }
        };
        var prescription = new Prescription
        {
            Id = prescriptionId,
            UserId = userId,
            Medications = medications
        };

        var prescriptionQueryable = new List<Prescription> { prescription }.AsQueryable();
        var mockPrescriptionSet = CreateMockDbSet(prescriptionQueryable);
        _mockPrescriptionRepo.Setup(r => r.GetQueryable()).Returns(mockPrescriptionSet.Object);

        _mockWorkflowRepo.Setup(r => r.GetQueryable()).Returns(new List<PrescriptionValidationWorkflow>().AsQueryable().BuildMockDbSet().Object);
        _mockOcrRepo.Setup(r => r.GetQueryable()).Returns(new List<PrescriptionOCRResult>().AsQueryable().BuildMockDbSet().Object);

        // Act
        var result = await _service.CreateValidationWorkflowAsync(prescriptionId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(prescriptionId, result.PrescriptionId);
        Assert.Equal(2, result.TotalMedications);
        Assert.True(result.HasHighRiskMedications);
        _mockWorkflowRepo.Verify(r => r.AddAsync(It.IsAny<PrescriptionValidationWorkflow>()), Times.Once);
        _mockValidationRepo.Verify(r => r.AddAsync(It.IsAny<MedicationValidation>()), Times.Exactly(2));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ConfirmMedication_Should_Mark_As_Confirmed()
    {
        // Arrange
        var userId = 1;
        var medicationId = 1;
        var medication = new Medication
        {
            Id = medicationId,
            UserId = userId,
            Name = "Aspirin",
            PrescriptionId = 1
        };

        _mockMedicationRepo.Setup(r => r.GetByIdAsync(medicationId))
            .ReturnsAsync(medication);

        var validation = new MedicationValidation
        {
            Id = 1,
            MedicationId = medicationId,
            UserId = userId,
            IsConfirmed = false
        };

        var validationQueryable = new List<MedicationValidation> { validation }.AsQueryable();
        _mockValidationRepo.Setup(r => r.GetQueryable()).Returns(validationQueryable.BuildMockDbSet().Object);

        var request = new ConfirmMedicationRequest
        {
            MedicationId = medicationId,
            AcknowledgeWarnings = true
        };

        // Act
        var result = await _service.ConfirmMedicationAsync(request, userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsConfirmed);
        _mockValidationRepo.Verify(r => r.UpdateAsync(It.Is<MedicationValidation>(v => v.IsConfirmed)), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CorrectMedication_Should_Store_Original_Values()
    {
        // Arrange
        var userId = 1;
        var medicationId = 1;
        var medication = new Medication
        {
            Id = medicationId,
            UserId = userId,
            Name = "Asprin", // Typo
            Dosage = "100mg",
            Frequency = "Once Daily",
            PrescriptionId = 1
        };

        _mockMedicationRepo.Setup(r => r.GetByIdAsync(medicationId))
            .ReturnsAsync(medication);

        var validationQueryable = new List<MedicationValidation>().AsQueryable();
        _mockValidationRepo.Setup(r => r.GetQueryable()).Returns(validationQueryable.BuildMockDbSet().Object);

        var request = new CorrectMedicationRequest
        {
            MedicationId = medicationId,
            CorrectedName = "Aspirin", // Fixed
            CorrectionReason = "Fixed spelling"
        };

        // Act
        var result = await _service.CorrectMedicationAsync(request, userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.HasCorrections);
        Assert.True(result.IsConfirmed); // Auto-confirmed after correction
        _mockValidationRepo.Verify(r => r.AddAsync(It.Is<MedicationValidation>(v =>
            v.HasCorrections &&
            v.OriginalName == "Asprin" &&
            v.IsConfirmed
        )), Times.Once);
        _mockMedicationRepo.Verify(r => r.UpdateAsync(It.Is<Medication>(m =>
            m.Name == "Aspirin"
        )), Times.Once);
    }

    [Fact]
    public async Task DeleteMedication_Should_Remove_Medication_And_Validation()
    {
        // Arrange
        var userId = 1;
        var medicationId = 1;
        var medication = new Medication
        {
            Id = medicationId,
            UserId = userId,
            Name = "Aspirin",
            PrescriptionId = 1
        };

        _mockMedicationRepo.Setup(r => r.GetByIdAsync(medicationId))
            .ReturnsAsync(medication);

        var validation = new MedicationValidation
        {
            Id = 1,
            MedicationId = medicationId,
            UserId = userId
        };

        var validationQueryable = new List<MedicationValidation> { validation }.AsQueryable();
        _mockValidationRepo.Setup(r => r.GetQueryable()).Returns(validationQueryable.BuildMockDbSet().Object);

        var request = new DeleteMedicationRequest
        {
            MedicationId = medicationId,
            Reason = "Incorrect medication"
        };

        // Act
        var result = await _service.DeleteMedicationAsync(request, userId);

        // Assert
        Assert.True(result);
        _mockValidationRepo.Verify(r => r.DeleteAsync(validation), Times.Once);
        _mockMedicationRepo.Verify(r => r.DeleteAsync(medication), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CompleteValidation_Should_Fail_If_Not_All_Confirmed()
    {
        // Arrange
        var userId = 1;
        var prescriptionId = 1;
        var workflow = new PrescriptionValidationWorkflow
        {
            Id = 1,
            PrescriptionId = prescriptionId,
            UserId = userId,
            Status = "InProgress"
        };

        var workflowQueryable = new List<PrescriptionValidationWorkflow> { workflow }.AsQueryable();
        _mockWorkflowRepo.Setup(r => r.GetQueryable()).Returns(workflowQueryable.BuildMockDbSet().Object);

        var medications = new List<Medication>
        {
            new Medication { Id = 1, PrescriptionId = prescriptionId },
            new Medication { Id = 2, PrescriptionId = prescriptionId }
        };

        var medicationQueryable = medications.AsQueryable();
        _mockMedicationRepo.Setup(r => r.GetQueryable()).Returns(medicationQueryable.BuildMockDbSet().Object);

        var validations = new List<MedicationValidation>
        {
            new MedicationValidation { MedicationId = 1, IsConfirmed = true },
            new MedicationValidation { MedicationId = 2, IsConfirmed = false } // Not confirmed!
        };

        var validationQueryable = validations.AsQueryable();
        _mockValidationRepo.Setup(r => r.GetQueryable()).Returns(validationQueryable.BuildMockDbSet().Object);

        var request = new CompleteValidationRequest
        {
            PrescriptionId = prescriptionId
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CompleteValidationAsync(request, userId)
        );
    }

    [Fact]
    public async Task CompleteValidation_Should_Update_Status_To_Completed()
    {
        // Arrange
        var userId = 1;
        var prescriptionId = 1;
        var workflow = new PrescriptionValidationWorkflow
        {
            Id = 1,
            PrescriptionId = prescriptionId,
            UserId = userId,
            Status = "InProgress",
            StartedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var workflowQueryable = new List<PrescriptionValidationWorkflow> { workflow }.AsQueryable();
        _mockWorkflowRepo.Setup(r => r.GetQueryable()).Returns(workflowQueryable.BuildMockDbSet().Object);

        var medications = new List<Medication>
        {
            new Medication { Id = 1, PrescriptionId = prescriptionId }
        };

        var medicationQueryable = medications.AsQueryable();
        _mockMedicationRepo.Setup(r => r.GetQueryable()).Returns(medicationQueryable.BuildMockDbSet().Object);

        var validations = new List<MedicationValidation>
        {
            new MedicationValidation { MedicationId = 1, IsConfirmed = true }
        };

        var validationQueryable = validations.AsQueryable();
        _mockValidationRepo.Setup(r => r.GetQueryable()).Returns(validationQueryable.BuildMockDbSet().Object);

        var request = new CompleteValidationRequest
        {
            PrescriptionId = prescriptionId,
            UserConsultedPharmacist = true,
            PharmacistNotes = "All clear"
        };

        // Act
        var result = await _service.CompleteValidationAsync(request, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        _mockWorkflowRepo.Verify(r => r.UpdateAsync(It.Is<PrescriptionValidationWorkflow>(w =>
            w.Status == "Completed" &&
            w.CompletedAt.HasValue &&
            w.UserConsultedPharmacist
        )), Times.Once);
    }

    // Helper method to create mock DbSet
    private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        return data.BuildMockDbSet();
    }
}

// Extension method for creating mock DbSet
public static class MockDbSetExtensions
{
    public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet;
    }
}
