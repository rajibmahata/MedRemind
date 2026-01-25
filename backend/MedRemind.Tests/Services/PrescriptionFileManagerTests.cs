using MedRemind.Core.Interfaces;
using MedRemind.Services.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MedRemind.Tests.Services;

/// <summary>
/// Unit tests for PrescriptionFileManager
/// Tests file upload, compression, validation, and storage management
/// </summary>
public class PrescriptionFileManagerTests : IDisposable
{
    private readonly Mock<ILogger<PrescriptionFileManager>> _mockLogger;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly string _testStoragePath;
    private readonly PrescriptionFileManager _fileManager;

    public PrescriptionFileManagerTests()
    {
        _mockLogger = new Mock<ILogger<PrescriptionFileManager>>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _testStoragePath = Path.Combine(Path.GetTempPath(), "MedRemindTests", Guid.NewGuid().ToString());
        
        Directory.CreateDirectory(_testStoragePath);
        
        _fileManager = new PrescriptionFileManager(_mockLogger.Object, _mockFileStorageService.Object);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testStoragePath))
        {
            Directory.Delete(_testStoragePath, true);
        }
    }

    #region SavePrescriptionFileAsync Tests

    [Fact]
    public async Task SavePrescriptionFileAsync_WithValidImage_SavesSuccessfully()
    {
        // Arrange
        var userId = 1;
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", 1024);

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FilePath.Should().NotBeNullOrEmpty();
        result.FileName.Should().Contain($"user_{userId}_");
        result.FileName.Should().EndWith(".jpg");
        result.FileType.Should().Be("Image");
        result.WasCompressed.Should().BeFalse(); // Small file, no compression needed

        // Verify file exists
        File.Exists(result.FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task SavePrescriptionFileAsync_WithValidPdf_SavesSuccessfully()
    {
        // Arrange
        var userId = 1;
        var mockFile = CreateMockFormFile("prescription.pdf", "application/pdf", 2048);

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FileName.Should().EndWith(".pdf");
        result.FileType.Should().Be("PDF");
        File.Exists(result.FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task SavePrescriptionFileAsync_WithInvalidFileType_ReturnsError()
    {
        // Arrange
        var mockFile = CreateMockFormFile("document.txt", "text/plain", 1024);

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid file type");
    }

    [Fact]
    public async Task SavePrescriptionFileAsync_WithEmptyFile_ReturnsError()
    {
        // Arrange
        var mockFile = CreateMockFormFile("empty.jpg", "image/jpeg", 0);

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task SavePrescriptionFileAsync_WithFileTooLarge_ReturnsError()
    {
        // Arrange
        var mockFile = CreateMockFormFile("huge.jpg", "image/jpeg", 25 * 1024 * 1024); // 25 MB

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("exceeds maximum upload size");
    }

    [Fact]
    public async Task SavePrescriptionFileAsync_WithLargeImage_CompressesAutomatically()
    {
        // Arrange
        var userId = 1;
        // Create a larger file (> 3 MB to trigger compression)
        var mockFile = CreateMockFormFile("large.jpg", "image/jpeg", 5 * 1024 * 1024);

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.WasCompressed.Should().BeTrue();
        result.OriginalSize.Should().BeGreaterThan(result.CompressedSize);
        result.CompressionRatio.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SavePrescriptionFileAsync_CreatesUniqueFileNames()
    {
        // Arrange
        var userId = 1;
        var mockFile1 = CreateMockFormFile("test.jpg", "image/jpeg", 1024);
        var mockFile2 = CreateMockFormFile("test.jpg", "image/jpeg", 1024);

        // Act
        var result1 = await _fileManager.SavePrescriptionFileAsync(mockFile1, userId);
        await Task.Delay(1100); // Wait to ensure different timestamp
        var result2 = await _fileManager.SavePrescriptionFileAsync(mockFile2, userId);

        // Assert
        result1.FileName.Should().NotBe(result2.FileName);
        File.Exists(result1.FilePath).Should().BeTrue();
        File.Exists(result2.FilePath).Should().BeTrue();
    }

    [Fact]
    public async Task SavePrescriptionFileAsync_GeneratesBase64Content()
    {
        // Arrange
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", 1024);

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Base64Content.Should().NotBeNullOrEmpty();
        
        // Verify base64 is valid
        var bytes = Convert.FromBase64String(result.Base64Content!);
        bytes.Should().NotBeEmpty();
    }

    #endregion

    #region GetFileAsync Tests

    [Fact]
    public async Task GetFileAsync_WithExistingFile_ReturnsBytes()
    {
        // Arrange
        var userId = 1;
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", 1024);
        var saveResult = await _fileManager.SavePrescriptionFileAsync(mockFile, userId);
        saveResult.Success.Should().BeTrue();

        // Act
        var fileBytes = await _fileManager.GetFileAsync(saveResult.FileName!);

        // Assert
        fileBytes.Should().NotBeNull();
        fileBytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFileAsync_WithNonExistentFile_ReturnsNull()
    {
        // Arrange
        var nonExistentFile = "nonexistent.jpg";

        // Act
        var fileBytes = await _fileManager.GetFileAsync(nonExistentFile);

        // Assert
        fileBytes.Should().BeNull();
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_DeletesSuccessfully()
    {
        // Arrange
        var userId = 1;
        var mockFile = CreateMockFormFile("test.jpg", "image/jpeg", 1024);
        var saveResult = await _fileManager.SavePrescriptionFileAsync(mockFile, userId);
        saveResult.Success.Should().BeTrue();
        File.Exists(saveResult.FilePath).Should().BeTrue();

        // Act
        var deleted = await _fileManager.DeleteFileAsync(saveResult.FileName!);

        // Assert
        deleted.Should().BeTrue();
        File.Exists(saveResult.FilePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var nonExistentFile = "nonexistent.jpg";

        // Act
        var deleted = await _fileManager.DeleteFileAsync(nonExistentFile);

        // Assert
        deleted.Should().BeFalse();
    }

    #endregion

    #region GetStorageStatistics Tests

    [Fact]
    public async Task GetStorageStatistics_WithFiles_ReturnsCorrectStats()
    {
        // Arrange
        var mockFile1 = CreateMockFormFile("test1.jpg", "image/jpeg", 1024);
        var mockFile2 = CreateMockFormFile("test2.jpg", "image/jpeg", 2048);
        
        await _fileManager.SavePrescriptionFileAsync(mockFile1, 1);
        await _fileManager.SavePrescriptionFileAsync(mockFile2, 1);

        // Act
        var stats = _fileManager.GetStorageStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalFiles.Should().Be(2);
        stats.TotalSizeBytes.Should().BeGreaterThan(0);
        stats.TotalSizeMB.Should().BeGreaterThan(0);
        stats.StoragePath.Should().Be(_testStoragePath);
    }

    [Fact]
    public void GetStorageStatistics_WithEmptyStorage_ReturnsZeroStats()
    {
        // Act
        var stats = _fileManager.GetStorageStatistics();

        // Assert
        stats.Should().NotBeNull();
        stats.TotalFiles.Should().Be(0);
        stats.TotalSizeBytes.Should().Be(0);
        stats.TotalSizeMB.Should().Be(0);
    }

    #endregion

    #region File Format Tests

    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".bmp", "image/bmp")]
    [InlineData(".webp", "image/webp")]
    public async Task SavePrescriptionFileAsync_SupportsAllImageFormats(string extension, string contentType)
    {
        // Arrange
        var fileName = $"test{extension}";
        var mockFile = CreateMockFormFile(fileName, contentType, 1024);

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.FileName.Should().EndWith(extension);
        result.FileType.Should().Be("Image");
    }

    #endregion

    #region Compression Tests

    [Fact]
    public async Task SavePrescriptionFileAsync_SmallFile_NoCompression()
    {
        // Arrange
        var mockFile = CreateMockFormFile("small.jpg", "image/jpeg", 1 * 1024 * 1024); // 1 MB

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.WasCompressed.Should().BeFalse();
        result.OriginalSize.Should().Be(result.CompressedSize);
    }

    [Fact]
    public async Task SavePrescriptionFileAsync_LargeFile_WithCompression()
    {
        // Arrange
        var mockFile = CreateMockFormFile("large.jpg", "image/jpeg", 5 * 1024 * 1024); // 5 MB

        // Act
        var result = await _fileManager.SavePrescriptionFileAsync(mockFile, 1);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.WasCompressed.Should().BeTrue();
        result.CompressedSize.Should().BeLessThan(result.OriginalSize);
        result.CompressedSize.Should().BeLessThan(4 * 1024 * 1024); // Should be under 4 MB
    }

    #endregion

    #region Helper Methods

    private IFormFile CreateMockFormFile(string fileName, string contentType, long length)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        mockFile.Setup(f => f.Length).Returns(length);

        var content = new byte[length];
        // Fill with some data
        for (int i = 0; i < Math.Min(length, 1024); i++)
        {
            content[i] = (byte)(i % 256);
        }

        var stream = new MemoryStream(content);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(async (Stream target, CancellationToken token) =>
            {
                stream.Position = 0;
                await stream.CopyToAsync(target, token);
            });

        return mockFile.Object;
    }

    #endregion
}
