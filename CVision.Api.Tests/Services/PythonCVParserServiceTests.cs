using CVision.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace CVision.Api.Tests.Services;

/// <summary>
/// Unit tests for PythonCVParserService, focusing on HTTP integration and error handling
/// </summary>
public class PythonCVParserServiceTests
{
    private readonly CvParserSettings _settings;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly PythonCVParserService _parserService;

    public PythonCVParserServiceTests()
    {
        _settings = new CvParserSettings
        {
            ServiceUrl = "http://localhost:8000"
        };

        var mockOptions = new Mock<IOptions<CvParserSettings>>();
        mockOptions.Setup(o => o.Value).Returns(_settings);

        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);

        _parserService = new PythonCVParserService(_httpClient, mockOptions.Object);
    }

    #region ParseCandidateFromFileAsync Tests

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithValidResponse_ReturnsDTO()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf", "CV content");
        var expectedDto = TestHelpers.CreateTestCandidateProfileDTO(1, "John Doe", 85);

        var responseJson = JsonSerializer.Serialize(expectedDto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _parserService.ParseCandidateFromFileAsync(
            mockFile, 1, "Software Developer", "Looking for experienced developer");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("John Doe");
        result.MatchScore.Should().Be(85);
        result.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_DeserializesAllFields()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        var expectedDto = new CandidateProfileDTO
        {
            JobId = 1,
            Name = "Jane Smith",
            Email = "jane.smith@example.com",
            Phone = "+45 12345678",
            Location = "Copenhagen",
            ExperienceYears = 7,
            ProfileSummary = "Experienced developer",
            MatchScore = 92,
            Skills = new List<string> { "C#", "Python", "SQL" },
            Strengths = new List<string> { "Strong problem solver", "Team player" },
            Weaknesses = new List<string> { "Limited frontend experience" },
            AnalysisSummary = "Excellent candidate"
        };

        var responseJson = JsonSerializer.Serialize(expectedDto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _parserService.ParseCandidateFromFileAsync(
            mockFile, 1, "Dev", "Description");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Jane Smith");
        result.Email.Should().Be("jane.smith@example.com");
        result.Phone.Should().Be("+45 12345678");
        result.Location.Should().Be("Copenhagen");
        result.ExperienceYears.Should().Be(7);
        result.MatchScore.Should().Be(92);
        result.Skills.Should().Contain(new[] { "C#", "Python", "SQL" });
        result.Strengths.Should().Contain("Strong problem solver");
        result.Weaknesses.Should().Contain("Limited frontend experience");
        result.AnalysisSummary.Should().Be("Excellent candidate");
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithInvalidJson_ThrowsException()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        SetupHttpResponse(HttpStatusCode.OK, "Invalid JSON {{{");

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            _parserService.ParseCandidateFromFileAsync(
                mockFile, 1, "Title", "Description"));
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithNullResponse_ThrowsException()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        SetupHttpResponse(HttpStatusCode.OK, "null");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _parserService.ParseCandidateFromFileAsync(
                mockFile, 1, "Title", "Description"));

        exception.Message.Should().Contain("Parsed response was null");
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithServiceError_ThrowsException()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        SetupHttpResponse(HttpStatusCode.InternalServerError, "Internal server error");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _parserService.ParseCandidateFromFileAsync(
                mockFile, 1, "Title", "Description"));

        exception.Message.Should().Contain("Parser service failed");
        exception.Message.Should().Contain("Internal server error");
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithBadRequest_ThrowsException()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        SetupHttpResponse(HttpStatusCode.BadRequest, "Invalid file format");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _parserService.ParseCandidateFromFileAsync(
                mockFile, 1, "Title", "Description"));

        exception.Message.Should().Contain("Parser service failed");
        exception.Message.Should().Contain("Invalid file format");
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithTimeout_ThrowsException()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _parserService.ParseCandidateFromFileAsync(
                mockFile, 1, "Title", "Description"));
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_ValidatesMatchScoreRange()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        var dto = TestHelpers.CreateTestCandidateProfileDTO(1, "Test", 95);

        var responseJson = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _parserService.ParseCandidateFromFileAsync(
            mockFile, 1, "Title", "Description");

        // Assert
        result!.MatchScore.Should().BeInRange(0, 100);
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_SendsCorrectUrl()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        var dto = TestHelpers.CreateTestCandidateProfileDTO(1);
        var responseJson = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        string? capturedUrl = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                capturedUrl = request.RequestUri?.ToString();
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                };
            });

        // Act
        await _parserService.ParseCandidateFromFileAsync(
            mockFile, 1, "Title", "Description");

        // Assert
        capturedUrl.Should().Be("http://localhost:8000/parse-cv/");
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_SendsMultipartFormData()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf", "CV content");
        var dto = TestHelpers.CreateTestCandidateProfileDTO(1);
        var responseJson = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        HttpContent? capturedContent = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                capturedContent = request.Content;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                };
            });

        // Act
        await _parserService.ParseCandidateFromFileAsync(
            mockFile, 1, "Developer", "Job description text");

        // Assert
        capturedContent.Should().NotBeNull();
        capturedContent.Should().BeOfType<MultipartFormDataContent>();
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithCaseInsensitiveJson_Deserializes()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");

        // Response with different casing
        var responseJson = @"{
            ""jobId"": 1,
            ""NAME"": ""Test User"",
            ""email"": ""test@example.com"",
            ""phone"": ""+45 12345678"",
            ""location"": ""Copenhagen"",
            ""experienceYears"": 5,
            ""profileSummary"": ""Test"",
            ""MATCHSCORE"": 80,
            ""skills"": [""C#""],
            ""strengths"": [""Strong""],
            ""weaknesses"": [""Weak""],
            ""analysisSummary"": ""Analysis""
        }";

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _parserService.ParseCandidateFromFileAsync(
            mockFile, 1, "Title", "Description");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test User");
        result.MatchScore.Should().Be(80);
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithEmptySkillsArray_HandlesGracefully()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");
        var dto = TestHelpers.CreateTestCandidateProfileDTO(1);
        dto.Skills = new List<string>();
        dto.Strengths = new List<string>();
        dto.Weaknesses = new List<string>();

        var responseJson = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        SetupHttpResponse(HttpStatusCode.OK, responseJson);

        // Act
        var result = await _parserService.ParseCandidateFromFileAsync(
            mockFile, 1, "Title", "Description");

        // Assert
        result.Should().NotBeNull();
        result!.Skills.Should().BeEmpty();
        result.Strengths.Should().BeEmpty();
        result.Weaknesses.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseCandidateFromFileAsync_WithNetworkError_ThrowsException()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test.pdf");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _parserService.ParseCandidateFromFileAsync(
                mockFile, 1, "Title", "Description"));
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    #endregion
}
