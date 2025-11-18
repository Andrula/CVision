using CVision.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CVision.Api.Tests.Controllers;

/// <summary>
/// Unit tests for CandidatesController, focusing on HTTP responses and validation
/// </summary>
public class CandidatesControllerTests
{
    private readonly Mock<ICandidateService> _mockCandidateService;
    private readonly CandidatesController _controller;

    public CandidatesControllerTests()
    {
        _mockCandidateService = new Mock<ICandidateService>();
        _controller = new CandidatesController(_mockCandidateService.Object);
    }

    #region GetCandidates Tests

    [Fact]
    public async Task GetCandidates_ReturnsOkResult()
    {
        // Arrange
        var candidates = new List<CandidateBasicDto>
        {
            new CandidateBasicDto { Id = 1, Name = "John Doe", MatchScore = 85, JobId = 1, ExperienceYears = 5 },
            new CandidateBasicDto { Id = 2, Name = "Jane Smith", MatchScore = 92, JobId = 1, ExperienceYears = 3 }
        };

        _mockCandidateService
            .Setup(s => s.GetCandidatesForJobAsync(It.IsAny<int>()))
            .ReturnsAsync(candidates);

        // Act
        var result = await _controller.GetCandidates(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(candidates);
    }

    [Fact]
    public async Task GetCandidates_CallsServiceWithCorrectJobId()
    {
        // Arrange
        _mockCandidateService
            .Setup(s => s.GetCandidatesForJobAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<CandidateBasicDto>());

        // Act
        await _controller.GetCandidates(42);

        // Assert
        _mockCandidateService.Verify(
            s => s.GetCandidatesForJobAsync(42),
            Times.Once);
    }

    #endregion

    #region Upload Tests

    [Fact]
    public async Task Upload_WithValidFile_ReturnsOkResult()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        var candidate = TestHelpers.CreateTestCandidate(1, "John Doe");

        _mockCandidateService
            .Setup(s => s.UploadCandidateAsync(1, mockFile))
            .ReturnsAsync(candidate);

        // Act
        var result = await _controller.Upload(1, mockFile);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(candidate);
    }

    [Fact]
    public async Task Upload_WithNullFile_ReturnsBadRequest()
    {
        // Arrange
        _mockCandidateService
            .Setup(s => s.UploadCandidateAsync(It.IsAny<int>(), It.IsAny<IFormFile>()))
            .ThrowsAsync(new ArgumentException("No file uploaded"));

        // Act
        var result = await _controller.Upload(1, null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeEquivalentTo(new { error = "No file uploaded" });
    }

    [Fact]
    public async Task Upload_WithInvalidJobId_ReturnsNotFound()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");

        _mockCandidateService
            .Setup(s => s.UploadCandidateAsync(999, mockFile))
            .ThrowsAsync(new InvalidOperationException("Job with ID 999 not found"));

        // Act
        var result = await _controller.Upload(999, mockFile);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result as NotFoundObjectResult;
        notFound!.Value.Should().BeEquivalentTo(new { error = "Job with ID 999 not found" });
    }

    [Fact]
    public async Task Upload_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");

        _mockCandidateService
            .Setup(s => s.UploadCandidateAsync(It.IsAny<int>(), It.IsAny<IFormFile>()))
            .ThrowsAsync(new Exception("Parser service down"));

        // Act
        var result = await _controller.Upload(1, mockFile);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetProfile Tests

    [Fact]
    public async Task GetProfile_WithExistingProfile_ReturnsOkResult()
    {
        // Arrange
        var profile = TestHelpers.CreateTestCandidateProfile(1, 1, "John Doe");

        _mockCandidateService
            .Setup(s => s.GetProfileAsync(1))
            .ReturnsAsync(profile);

        // Act
        var result = await _controller.GetProfile(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(profile);
    }

    [Fact]
    public async Task GetProfile_WithNonExistentProfile_ReturnsNotFound()
    {
        // Arrange
        _mockCandidateService
            .Setup(s => s.GetProfileAsync(999))
            .ReturnsAsync((CandidateProfile?)null);

        // Act
        var result = await _controller.GetProfile(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result as NotFoundObjectResult;
        notFound!.Value.Should().BeEquivalentTo(new { error = "Profile not found" });
    }

    #endregion

    #region SaveProfile Tests

    [Fact]
    public async Task SaveProfile_WithValidDTO_ReturnsOkResult()
    {
        // Arrange
        var dto = TestHelpers.CreateTestCandidateProfileDTO(1, "John Doe");
        var profile = TestHelpers.CreateTestCandidateProfile(1, 1, "John Doe");

        _mockCandidateService
            .Setup(s => s.SaveProfileAsync(dto))
            .ReturnsAsync(profile);

        // Act
        var result = await _controller.SaveProfile(dto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(profile);
    }

    [Fact]
    public async Task SaveProfile_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var dto = TestHelpers.CreateTestCandidateProfileDTO(1);

        _mockCandidateService
            .Setup(s => s.SaveProfileAsync(It.IsAny<CandidateProfileDTO>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.SaveProfile(dto);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetCandidateCV Tests

    [Fact]
    public async Task GetCandidateCV_WithExistingFile_ReturnsFileResult()
    {
        // Arrange
        var stream = new MemoryStream();

        _mockCandidateService
            .Setup(s => s.GetCandidateCvStreamAsync(1))
            .ReturnsAsync(stream);

        // Act
        var result = await _controller.GetCandidateCV(1);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileResult = result as FileStreamResult;
        fileResult!.ContentType.Should().Be("application/pdf");
        fileResult.EnableRangeProcessing.Should().BeTrue();
        fileResult.FileStream.Should().BeSameAs(stream);
    }

    [Fact]
    public async Task GetCandidateCV_WithNonExistentFile_ReturnsNotFound()
    {
        // Arrange
        _mockCandidateService
            .Setup(s => s.GetCandidateCvStreamAsync(999))
            .ReturnsAsync((Stream?)null);

        // Act
        var result = await _controller.GetCandidateCV(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result as NotFoundObjectResult;
        notFound!.Value.Should().BeEquivalentTo(new { error = "CV file not found" });
    }

    #endregion
}
