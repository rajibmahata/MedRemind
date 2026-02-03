using MedRemind.Core.DTOs;
using MedRemind.Core.Interfaces;
using MedRemind.Core.Models;
using MedRemind.Services.AI;
using MedRemind.Services.AI.Agents;
using MedRemind.Services.Prescriptions;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MedRemind.Tests.Services;

/// <summary>
/// Comprehensive unit tests for PrescriptionReaderService
/// Tests basic and comprehensive prescription processing
/// </summary>
public class PrescriptionReaderServiceTests
{
    private readonly Mock<HttpClient> _mockHttpClient;
    private readonly Mock<IValidationAgentService> _mockValidationAgent;
    private readonly Mock<AzureDocumentIntelligenceService> _mockAzureDocService;
    private readonly Mock<MultiLlmAPIOrchestrator> _mockOrchestrator;
    private readonly Mock<PrescriptionDeduplicationService> _mockDeduplicationService;
    private readonly Mock<PrescriptionService> _mockPrescriptionService;
    private readonly PrescriptionReaderService _service;
    private const string TestApiKey = "test-api-key";

    public PrescriptionReaderServiceTests()
    {
        var mockHttpClientHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpClientHandler.Object);
        
        _mockHttpClient = new Mock<HttpClient>();
        _mockValidationAgent = new Mock<IValidationAgentService>();
        
        // Mock Azure Doc Service with proper constructor
        var azureLogger = new Mock<ILogger<AzureDocumentIntelligenceService>>();
        var mockPreprocessor = new Mock<PrescriptionOcrTextPreprocessor>();
        var mockFileStorage = new Mock<IFileStorageService>();
        
        _mockAzureDocService = new Mock<AzureDocumentIntelligenceService>(
            httpClient,
            "test-endpoint",
            "test-key",
            mockPreprocessor.Object,
            mockFileStorage.Object);
        _mockAzureDocService.CallBase = false;
        
        // Mock Parser Agents
        var mockOpenAIAgent = new Mock<OpenAIPrescriptionParserAgent>(new OpenAI.Chat.ChatClient("test-model", "test-key"));
        var mockDeepSeekAgent = new Mock<DeepSeekPrescriptionParserAgent>(httpClient, "test-key", "https://api.deepseek.com/v1/chat/completions", 5000);
        var mockClaudeAgent = new Mock<ClaudePrescriptionParserAgent>("test-key", "claude-3-5-sonnet-20241022", 5000);
        
        // Mock Orchestrator
        _mockOrchestrator = new Mock<MultiLlmAPIOrchestrator>(
            mockOpenAIAgent.Object,
            mockDeepSeekAgent.Object,
            mockClaudeAgent.Object);
        _mockOrchestrator.CallBase = false;
        
        // Mock other services
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockDeduplicationService = new Mock<PrescriptionDeduplicationService>(mockUnitOfWork.Object);
        _mockDeduplicationService.CallBase = false;
        
        _mockPrescriptionService = new Mock<PrescriptionService>(mockUnitOfWork.Object);
        _mockPrescriptionService.CallBase = false;

        _service = new PrescriptionReaderService(
            httpClient,
            TestApiKey,
            _mockValidationAgent.Object,
            _mockAzureDocService.Object,
            _mockOrchestrator.Object,
            _mockDeduplicationService.Object,
            _mockPrescriptionService.Object);
    }

    #region ReadPrescriptionFromBase64Async Tests

    [Fact]
    public async Task ReadPrescriptionFromBase64Async_WithValidImage_ReturnsSuccess()
    {
        // Arrange
        var base64Image = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var extractedText = "Dr. John Smith\nAspirin 500mg\nTake twice daily";

        var parseResult = new PrescriptionReadResult
        {
            Success = true,
            Doctor = new DoctorData { Name = "Dr. John Smith" },
            Medications = new List<MedicationData>
            {
                new MedicationData
                {
                    Name = "Aspirin",
                    Dosage = "500",
                    Unit = "mg",
                    Frequency = "Twice daily",
                    ConfidenceScore = 0.95f
                }
            }
        };

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                base64Image, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        // Setup orchestrator to return the parse result
        var orchestratorResult = new PrescriptionProcessingResult
        {
            Success = true,
            ParseResult = parseResult,
            SelectedProvider = "OpenAI",
            MatchScore = 0.95,
            TotalAttempts = 1
        };

        _mockOrchestrator
            .Setup(o => o.ProcessPrescriptionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestratorResult);

        _mockValidationAgent
            .Setup(v => v.ValidateMedicationsAsync(It.IsAny<List<MedicationData>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidationWarning>());

        // Act
        var result = await _service.ReadPrescriptionFromBase64Async(base64Image);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Medications.Should().HaveCount(1);
        result.Medications[0].Name.Should().Be("Aspirin");
        result.Doctor?.Name.Should().Be("Dr. John Smith");
        result.ConfidenceScore.Should().Be(0.95);
    }

    [Fact]
    public async Task ReadPrescriptionFromBase64Async_WithEmptyImage_ReturnsError()
    {
        // Arrange
        var emptyImage = string.Empty;

        // Act
        var result = await _service.ReadPrescriptionFromBase64Async(emptyImage);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Image data is empty");
    }

    [Fact]
    public async Task ReadPrescriptionFromBase64Async_WithAzureOcrFailure_ReturnsError()
    {
        // Arrange
        var base64Image = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                base64Image, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Azure OCR failed"));

        // Act
        var result = await _service.ReadPrescriptionFromBase64Async(base64Image);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Error processing prescription");
    }

    [Fact]
    public async Task ReadPrescriptionFromBase64Async_WithParserFailure_ReturnsError()
    {
        // Arrange
        var base64Image = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var extractedText = "Dr. John Smith\nAspirin 500mg";

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                base64Image, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        // Setup orchestrator to throw exception
        _mockOrchestrator
            .Setup(o => o.ProcessPrescriptionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Parser failed"));

        // Act
        var result = await _service.ReadPrescriptionFromBase64Async(base64Image);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Error processing prescription");
    }

    [Fact]
    public async Task ReadPrescriptionFromBase64Async_WithNoMedications_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var base64Image = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var extractedText = "This is a note with no medications";

        var parseResult = new PrescriptionReadResult
        {
            Success = true,
            Medications = new List<MedicationData>()
        };

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                base64Image, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        // Setup orchestrator to return empty result
        var orchestratorResult = new PrescriptionProcessingResult
        {
            Success = true,
            ParseResult = parseResult,
            SelectedProvider = "OpenAI",
            MatchScore = 0.0,
            TotalAttempts = 1
        };

        _mockOrchestrator
            .Setup(o => o.ProcessPrescriptionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestratorResult);

        // Act
        var result = await _service.ReadPrescriptionFromBase64Async(base64Image);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Medications.Should().BeEmpty();
        result.ConfidenceScore.Should().Be(0.0);
    }

    [Fact]
    public async Task ReadPrescriptionFromBase64Async_WithValidationWarnings_ReturnsWarnings()
    {
        // Arrange
        var base64Image = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var extractedText = "Dr. John Smith\nUnknownDrug 500mg";

        var parseResult = new PrescriptionReadResult
        {
            Success = true,
            Medications = new List<MedicationData>
            {
                new MedicationData
                {
                    Name = "UnknownDrug",
                    Dosage = "500",
                    Unit = "mg",
                    ConfidenceScore = 0.60f
                }
            }
        };

        var warnings = new List<ValidationWarning> 
        { 
            new ValidationWarning { Message = "UnknownDrug is not in our database" } 
        };

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                base64Image, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        // Setup orchestrator to return result with warnings
        var orchestratorResult = new PrescriptionProcessingResult
        {
            Success = true,
            ParseResult = parseResult,
            SelectedProvider = "OpenAI",
            MatchScore = 0.60,
            TotalAttempts = 1
        };

        _mockOrchestrator
            .Setup(o => o.ProcessPrescriptionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestratorResult);

        _mockValidationAgent
            .Setup(v => v.ValidateMedicationsAsync(It.IsAny<List<MedicationData>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(warnings);

        // Act
        var result = await _service.ReadPrescriptionFromBase64Async(base64Image);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ValidationWarnings.Should().HaveCount(1);
        result.ValidationWarnings[0].Message.Should().Be("UnknownDrug is not in our database");
    }

    #endregion

    #region ProcessPrescriptionComprehensiveAsync Tests

    [Fact]
    public async Task ProcessPrescriptionComprehensiveAsync_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var userId = 1;
        var imageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var imagePath = "Files/Prescriptions/test.jpg";
        var uniqueFileName = "user_1_20250129_143022.jpg";
        var originalFileName = "my_prescription.jpg";
        var extractedText = "Dr. John Smith\nAspirin 500mg\nTake twice daily";

        var prescription = new Prescription
        {
            Id = 1,
            UserId = userId,
            ImagePath = imagePath,
            Status = "Processing"
        };

        var orchestratorResult = new MedRemind.Core.DTOs.PrescriptionProcessingResult
        {
            Success = true,
            TotalAttempts = 2,
            MatchScore = 0.95,
            SelectedProvider = "OpenAI + DeepSeek",
            ProcessingTime = TimeSpan.FromSeconds(5),
            ParseResult = new PrescriptionReadResult
            {
                Success = true,
                Doctor = new DoctorData { Name = "Dr. John Smith" },
                PrescriptionDate = DateTime.UtcNow,
                Medications = new List<MedicationData>
                {
                    new MedicationData
                    {
                        Name = "Aspirin",
                        Dosage = "500",
                        Unit = "mg",
                    Frequency = "Twice daily"
                    }
                }
            }
        };

        _mockPrescriptionService
            .Setup(s => s.AddPrescriptionAsync(It.IsAny<Prescription>()))
            .ReturnsAsync(prescription);

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                imageBase64, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        _mockDeduplicationService
            .Setup(s => s.CheckForDuplicateAsync(extractedText, userId))
            .ReturnsAsync(new DuplicateCheckResult { IsDuplicate = false });

        _mockOrchestrator
            .Setup(o => o.ProcessPrescriptionAsync(
                extractedText, 
                It.IsAny<string>(), 
                prescription.Id, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestratorResult);

        _mockPrescriptionService
            .Setup(s => s.UpdatePrescriptionStatusAsync(
                prescription.Id,
                "Processed"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessPrescriptionComprehensiveAsync(
            imageBase64, 
            imagePath, 
            uniqueFileName, 
            originalFileName, 
            userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.PrescriptionId.Should().Be(1);
        result.PrescriptionResult.Should().NotBeNull();
        result.PrescriptionResult!.Medications.Should().HaveCount(1);
        result.ProcessingAttempts.Should().Be(2);
        result.MatchScore.Should().Be(0.95);
        result.SelectedProvider.Should().Be("OpenAI + DeepSeek");
    }

    [Fact]
    public async Task ProcessPrescriptionComprehensiveAsync_WithDuplicate_ReturnsExistingResult()
    {
        // Arrange
        var userId = 1;
        var imageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var imagePath = "Files/Prescriptions/test.jpg";
        var uniqueFileName = "user_1_20250129_143022.jpg";
        var originalFileName = "my_prescription.jpg";
        var extractedText = "Dr. John Smith\nAspirin 500mg";

        var prescription = new Prescription
        {
            Id = 2,
            UserId = userId,
            ImagePath = imagePath,
            Status = "Processing"
        };

        var existingResult = new PrescriptionReadResult
        {
            Success = true,
            Doctor = new DoctorData { Name = "Dr. John Smith" },
            Medications = new List<MedicationData>
            {
                new MedicationData { Name = "Aspirin", Dosage = "500", Unit = "mg" }
            }
        };

        var duplicateCheckResult = new DuplicateCheckResult
        {
            IsDuplicate = true,
            Message = "Duplicate found",
            SimilarityScore = 0.95,
            ExistingResult = new PrescriptionOCRResult
            {
                PrescriptionId = 1,
                ProcessedAt = DateTime.UtcNow.AddDays(-1),
                ComparisonScore = 0.95,
                MedicationCount = 1,
                SelectedResponse = JsonSerializer.Serialize(existingResult)
            }
        };

        _mockPrescriptionService
            .Setup(s => s.AddPrescriptionAsync(It.IsAny<Prescription>()))
            .ReturnsAsync(prescription);

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                imageBase64, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        _mockDeduplicationService
            .Setup(s => s.CheckForDuplicateAsync(extractedText, userId))
            .ReturnsAsync(duplicateCheckResult);

        _mockPrescriptionService
            .Setup(s => s.UpdatePrescriptionStatusAsync(
                prescription.Id,
                "Processed"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessPrescriptionComprehensiveAsync(
            imageBase64, 
            imagePath, 
            uniqueFileName, 
            originalFileName, 
            userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.IsDuplicate.Should().BeTrue();
        result.SimilarityScore.Should().Be(0.95);
        result.ExistingPrescriptionId.Should().Be(1);
        result.PrescriptionResult.Should().NotBeNull();
        result.PrescriptionResult!.Medications.Should().HaveCount(1);

        // Verify orchestrator was NOT called (duplicate found)
        _mockOrchestrator.Verify(
            o => o.ProcessPrescriptionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessPrescriptionComprehensiveAsync_WithEmptyOcrText_ReturnsError()
    {
        // Arrange
        var userId = 1;
        var imageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var imagePath = "Files/Prescriptions/test.jpg";
        var uniqueFileName = "user_1_20250129_143022.jpg";
        var originalFileName = "my_prescription.jpg";

        var prescription = new Prescription
        {
            Id = 1,
            UserId = userId,
            ImagePath = imagePath,
            Status = "Processing"
        };

        _mockPrescriptionService
            .Setup(s => s.AddPrescriptionAsync(It.IsAny<Prescription>()))
            .ReturnsAsync(prescription);

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                imageBase64, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        _mockPrescriptionService
            .Setup(s => s.UpdatePrescriptionStatusAsync(
                prescription.Id,
                "Failed",
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessPrescriptionComprehensiveAsync(
            imageBase64, 
            imagePath, 
            uniqueFileName, 
            originalFileName, 
            userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Failed to extract text from image");
    }

    [Fact]
    public async Task ProcessPrescriptionComprehensiveAsync_WithOrchestratorFailure_ReturnsError()
    {
        // Arrange
        var userId = 1;
        var imageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var imagePath = "Files/Prescriptions/test.jpg";
        var uniqueFileName = "user_1_20250129_143022.jpg";
        var originalFileName = "my_prescription.jpg";
        var extractedText = "Dr. John Smith\nAspirin 500mg";

        var prescription = new Prescription
        {
            Id = 1,
            UserId = userId,
            ImagePath = imagePath,
            Status = "Processing"
        };

        var orchestratorResult = new MedRemind.Core.DTOs.PrescriptionProcessingResult
        {
            Success = false,
            ErrorMessage = "All AI providers failed",
            TotalAttempts = 3
        };

        _mockPrescriptionService
            .Setup(s => s.AddPrescriptionAsync(It.IsAny<Prescription>()))
            .ReturnsAsync(prescription);

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                imageBase64, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        _mockDeduplicationService
            .Setup(s => s.CheckForDuplicateAsync(extractedText, userId))
            .ReturnsAsync(new DuplicateCheckResult { IsDuplicate = false });

        _mockOrchestrator
            .Setup(o => o.ProcessPrescriptionAsync(
                extractedText, 
                It.IsAny<string>(), 
                prescription.Id, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestratorResult);

        _mockPrescriptionService
            .Setup(s => s.UpdatePrescriptionStatusAsync(
                prescription.Id,
                "Failed",
                "All AI providers failed"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessPrescriptionComprehensiveAsync(
            imageBase64, 
            imagePath, 
            uniqueFileName, 
            originalFileName, 
            userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("All AI providers failed");
        result.ProcessingAttempts.Should().Be(3);
    }

    [Fact]
    public async Task ProcessPrescriptionComprehensiveAsync_WithNoMedications_UpdatesStatusCorrectly()
    {
        // Arrange
        var userId = 1;
        var imageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var imagePath = "Files/Prescriptions/test.jpg";
        var uniqueFileName = "user_1_20250129_143022.jpg";
        var originalFileName = "my_prescription.jpg";
        var extractedText = "This is just a note";

        var prescription = new Prescription
        {
            Id = 1,
            UserId = userId,
            ImagePath = imagePath,
            Status = "Processing"
        };

        var orchestratorResult = new MedRemind.Core.DTOs.PrescriptionProcessingResult
        {
            Success = true,
            ParseResult = new PrescriptionReadResult
            {
                Success = true,
                Medications = new List<MedicationData>() // Empty list
            }
        };

        _mockPrescriptionService
            .Setup(s => s.AddPrescriptionAsync(It.IsAny<Prescription>()))
            .ReturnsAsync(prescription);

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                imageBase64, 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        _mockDeduplicationService
            .Setup(s => s.CheckForDuplicateAsync(extractedText, userId))
            .ReturnsAsync(new DuplicateCheckResult { IsDuplicate = false });

        _mockOrchestrator
            .Setup(o => o.ProcessPrescriptionAsync(
                extractedText, 
                It.IsAny<string>(), 
                prescription.Id, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestratorResult);

        _mockPrescriptionService
            .Setup(s => s.UpdatePrescriptionStatusAsync(
                prescription.Id,
                "Processed",
                "No medications found"))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ProcessPrescriptionComprehensiveAsync(
            imageBase64, 
            imagePath, 
            uniqueFileName, 
            originalFileName, 
            userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.WarningMessage.Should().Be("No medications found in the prescription.");
        
        _mockPrescriptionService.Verify(
            s => s.UpdatePrescriptionStatusAsync(prescription.Id, "Processed", "No medications found"),
            Times.Once);
    }

    [Fact]
    public async Task ProcessPrescriptionComprehensiveAsync_WithMissingServices_ReturnsError()
    {
        // Arrange - Create orchestrator mock for this test
        var mockOpenAIAgent = new Mock<OpenAIPrescriptionParserAgent>(new OpenAI.Chat.ChatClient("test-model", "test-key"));
        var mockDeepSeekAgent = new Mock<DeepSeekPrescriptionParserAgent>(new HttpClient(), "test-key", "https://api.deepseek.com/v1/chat/completions", 5000);
        var mockClaudeAgent = new Mock<ClaudePrescriptionParserAgent>("test-key", "claude-3-5-sonnet-20241022", 5000);
        
        var mockOrchestrator = new Mock<MultiLlmAPIOrchestrator>(
            mockOpenAIAgent.Object,
            mockDeepSeekAgent.Object,
            mockClaudeAgent.Object);
        
        var serviceWithoutDependencies = new PrescriptionReaderService(
            new HttpClient(),
            TestApiKey,
            _mockValidationAgent.Object,
            _mockAzureDocService.Object,
            mockOrchestrator.Object,
            null, // No deduplication
            null  // No prescription service
        );

        var userId = 1;
        var imageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 });
        var uniqueFileName = "user_1_20250129_143022.jpg";
        var originalFileName = "my_prescription.jpg";

        // Act
        var result = await serviceWithoutDependencies.ProcessPrescriptionComprehensiveAsync(
            imageBase64,
            null,
            uniqueFileName,
            originalFileName,
            userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Required services not configured");
    }

    #endregion

    #region ReadPrescriptionAsync Tests

    [Fact]
    public async Task ReadPrescriptionAsync_WithValidFilePath_ReturnsSuccess()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        File.WriteAllBytes(tempFile, new byte[] { 1, 2, 3, 4, 5 });

        var extractedText = "Dr. John Smith\nAspirin 500mg";
        var parseResult = new PrescriptionReadResult
        {
            Success = true,
            Medications = new List<MedicationData>
            {
                new MedicationData { Name = "Aspirin", Dosage = "500", Unit = "mg" }
            }
        };

        _mockAzureDocService
            .Setup(s => s.ExtractTextFromImageAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        // Setup orchestrator
        var orchestratorResult = new PrescriptionProcessingResult
        {
            Success = true,
            ParseResult = parseResult,
            SelectedProvider = "OpenAI",
            MatchScore = 0.95,
            TotalAttempts = 1
        };

        _mockOrchestrator
            .Setup(o => o.ProcessPrescriptionAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orchestratorResult);

        _mockValidationAgent
            .Setup(v => v.ValidateMedicationsAsync(It.IsAny<List<MedicationData>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidationWarning>());

        try
        {
            // Act
            var result = await _service.ReadPrescriptionAsync(tempFile);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Medications.Should().HaveCount(1);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadPrescriptionAsync_WithNonExistentFile_ReturnsError()
    {
        // Arrange
        var nonExistentFile = "nonexistent.jpg";

        // Act
        var result = await _service.ReadPrescriptionAsync(nonExistentFile);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Error reading prescription");
    }

    #endregion
}
