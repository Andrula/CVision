using CVision.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CVision.Api.Tests.Services;

/// <summary>
/// Unit tests for FileStorageService, focusing on file I/O operations
/// </summary>
public class FileStorageServiceTests : IDisposable
{
    private readonly string _testUploadPath;
    private readonly FileStorageSettings _settings;
    private readonly Mock<ILogger<FileStorageService>> _mockLogger;
    private readonly FileStorageService _fileStorageService;

    public FileStorageServiceTests()
    {
        // Create a unique temp directory for each test
        _testUploadPath = Path.Combine(Path.GetTempPath(), $"cvision_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testUploadPath);

        _settings = new FileStorageSettings
        {
            UploadPath = _testUploadPath,
            MaxFileSizeInMB = 10,
            AllowedExtensions = new List<string> { ".pdf", ".doc", ".docx" }
        };

        var mockOptions = new Mock<IOptions<FileStorageSettings>>();
        mockOptions.Setup(o => o.Value).Returns(_settings);

        _mockLogger = new Mock<ILogger<FileStorageService>>();

        _fileStorageService = new FileStorageService(mockOptions.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testUploadPath))
        {
            Directory.Delete(_testUploadPath, recursive: true);
        }
    }

    #region SaveFileAsync Tests

    [Fact]
    public async Task SaveFileAsync_WithValidFile_SavesFileWithGuidPrefix()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf", "Test PDF content");

        // Act
        var result = await _fileStorageService.SaveFileAsync(mockFile);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().EndWith("_test_cv.pdf");
        result.Should().MatchRegex(@"^[a-f0-9\-]{36}_test_cv\.pdf$"); // GUID pattern

        // Verify file exists on disk
        var filePath = Path.Combine(_testUploadPath, result);
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveFileAsync_GeneratesUniqueFilenames()
    {
        // Arrange
        var mockFile1 = TestHelpers.CreateMockFormFile("same_name.pdf", "Content 1");
        var mockFile2 = TestHelpers.CreateMockFormFile("same_name.pdf", "Content 2");

        // Act
        var result1 = await _fileStorageService.SaveFileAsync(mockFile1);
        var result2 = await _fileStorageService.SaveFileAsync(mockFile2);

        // Assert
        result1.Should().NotBe(result2);

        // Both files should exist
        File.Exists(Path.Combine(_testUploadPath, result1)).Should().BeTrue();
        File.Exists(Path.Combine(_testUploadPath, result2)).Should().BeTrue();
    }

    [Fact]
    public async Task SaveFileAsync_SavesCorrectContent()
    {
        // Arrange
        var expectedContent = "This is the test PDF content that should be saved";
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf", expectedContent);

        // Act
        var result = await _fileStorageService.SaveFileAsync(mockFile);

        // Assert
        var filePath = Path.Combine(_testUploadPath, result);
        var actualContent = await File.ReadAllTextAsync(filePath);
        actualContent.Should().Be(expectedContent);
    }

    [Fact]
    public async Task SaveFileAsync_WithNullFile_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _fileStorageService.SaveFileAsync(null!));
    }

    [Fact]
    public async Task SaveFileAsync_WithEmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        mockFile.Setup(f => f.FileName).Returns("empty.pdf");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _fileStorageService.SaveFileAsync(mockFile.Object));
    }

    [Fact]
    public async Task SaveFileAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var newUploadPath = Path.Combine(Path.GetTempPath(), $"cvision_new_{Guid.NewGuid()}");
        var newSettings = new FileStorageSettings { UploadPath = newUploadPath };
        var mockOptions = new Mock<IOptions<FileStorageSettings>>();
        mockOptions.Setup(o => o.Value).Returns(newSettings);

        var newService = new FileStorageService(mockOptions.Object, _mockLogger.Object);
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");

        try
        {
            // Act
            var result = await newService.SaveFileAsync(mockFile);

            // Assert
            Directory.Exists(newUploadPath).Should().BeTrue();
            File.Exists(Path.Combine(newUploadPath, result)).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(newUploadPath))
                Directory.Delete(newUploadPath, recursive: true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_LogsFileOperation()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");

        // Act
        await _fileStorageService.SaveFileAsync(mockFile);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Saved file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetFileStreamAsync Tests

    [Fact]
    public async Task GetFileStreamAsync_WithExistingFile_ReturnsStream()
    {
        // Arrange
        var fileName = $"{Guid.NewGuid()}_test.pdf";
        var filePath = Path.Combine(_testUploadPath, fileName);
        var expectedContent = "Test content";
        await File.WriteAllTextAsync(filePath, expectedContent);

        // Act
        var result = await _fileStorageService.GetFileStreamAsync(fileName);

        // Assert
        result.Should().NotBeNull();
        using var reader = new StreamReader(result!);
        var actualContent = await reader.ReadToEndAsync();
        actualContent.Should().Be(expectedContent);
    }

    [Fact]
    public async Task GetFileStreamAsync_WithNonExistentFile_ReturnsNull()
    {
        // Act
        var result = await _fileStorageService.GetFileStreamAsync("non_existent_file.pdf");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFileStreamAsync_WithEmptyFileName_ReturnsNull()
    {
        // Act
        var result = await _fileStorageService.GetFileStreamAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFileStreamAsync_WithNullFileName_ReturnsNull()
    {
        // Act
        var result = await _fileStorageService.GetFileStreamAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFileStreamAsync_WithNonExistentFile_LogsWarning()
    {
        // Act
        await _fileStorageService.GetFileStreamAsync("missing_file.pdf");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("File not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_DeletesFile()
    {
        // Arrange
        var fileName = $"{Guid.NewGuid()}_test.pdf";
        var filePath = Path.Combine(_testUploadPath, fileName);
        await File.WriteAllTextAsync(filePath, "Test content");
        File.Exists(filePath).Should().BeTrue();

        // Act
        await _fileStorageService.DeleteFileAsync(fileName);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_DoesNotThrow()
    {
        // Act
        var act = async () => await _fileStorageService.DeleteFileAsync("non_existent.pdf");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteFileAsync_WithEmptyFileName_DoesNotThrow()
    {
        // Act
        var act = async () => await _fileStorageService.DeleteFileAsync("");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNullFileName_DoesNotThrow()
    {
        // Act
        var act = async () => await _fileStorageService.DeleteFileAsync(null!);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteFileAsync_LogsDeletion()
    {
        // Arrange
        var fileName = $"{Guid.NewGuid()}_test.pdf";
        var filePath = Path.Combine(_testUploadPath, fileName);
        await File.WriteAllTextAsync(filePath, "Test content");

        // Act
        await _fileStorageService.DeleteFileAsync(fileName);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleted file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region FileExists Tests

    [Fact]
    public void FileExists_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var fileName = $"{Guid.NewGuid()}_test.pdf";
        var filePath = Path.Combine(_testUploadPath, fileName);
        File.WriteAllText(filePath, "Test content");

        // Act
        var result = _fileStorageService.FileExists(fileName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void FileExists_WithNonExistentFile_ReturnsFalse()
    {
        // Act
        var result = _fileStorageService.FileExists("non_existent.pdf");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExists_WithEmptyFileName_ReturnsFalse()
    {
        // Act
        var result = _fileStorageService.FileExists("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FileExists_WithNullFileName_ReturnsFalse()
    {
        // Act
        var result = _fileStorageService.FileExists(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullLifecycle_SaveGetDelete_WorksCorrectly()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("lifecycle_test.pdf", "Lifecycle content");

        // Act & Assert - Save
        var savedFileName = await _fileStorageService.SaveFileAsync(mockFile);
        savedFileName.Should().NotBeNullOrEmpty();

        // Act & Assert - File Exists
        _fileStorageService.FileExists(savedFileName).Should().BeTrue();

        // Act & Assert - Get Stream
        var stream = await _fileStorageService.GetFileStreamAsync(savedFileName);
        stream.Should().NotBeNull();
        string content;
        using (var reader = new StreamReader(stream!))
        {
            content = await reader.ReadToEndAsync();
        }
        content.Should().Be("Lifecycle content");

        // Act & Assert - Delete
        await _fileStorageService.DeleteFileAsync(savedFileName);
        _fileStorageService.FileExists(savedFileName).Should().BeFalse();

        // Act & Assert - Get Stream after deletion
        var deletedStream = await _fileStorageService.GetFileStreamAsync(savedFileName);
        deletedStream.Should().BeNull();
    }

    #endregion
}
