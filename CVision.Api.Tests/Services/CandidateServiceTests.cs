using CVision.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CVision.Api.Tests.Services;

/// <summary>
/// Unit tests for CandidateService, focusing on CV upload orchestration
/// </summary>
public class CandidateServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IFileStorageService> _mockFileStorage;
    private readonly Mock<IPythonCvParserService> _mockParser;
    private readonly Mock<ILogger<CandidateService>> _mockLogger;
    private readonly CandidateService _candidateService;

    public CandidateServiceTests()
    {
        _context = TestHelpers.CreateInMemoryDbContext();
        _mockFileStorage = new Mock<IFileStorageService>();
        _mockParser = new Mock<IPythonCvParserService>();
        _mockLogger = new Mock<ILogger<CandidateService>>();

        _candidateService = new CandidateService(
            _context,
            _mockFileStorage.Object,
            _mockParser.Object,
            _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region UploadCandidateAsync Tests

    [Fact]
    public async Task UploadCandidateAsync_WithValidFile_CreatesCandidate()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf");
        var parsedDto = TestHelpers.CreateTestCandidateProfileDTO(job.Id, "John Doe", 85);

        _mockParser.Setup(p => p.ParseCandidateFromFileAsync(
                It.IsAny<IFormFile>(), job.Id, job.Title, job.Description))
            .ReturnsAsync(parsedDto);

        _mockFileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("unique_filename.pdf");

        // Act
        var result = await _candidateService.UploadCandidateAsync(job.Id, mockFile);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.JobId.Should().Be(job.Id);
        result.Name.Should().Be("John Doe"); // Should be updated from parsed data
        result.FileName.Should().Be("test_cv.pdf");

        // Verify candidate is in database
        var dbCandidate = await _context.Candidates.FindAsync(result.Id);
        dbCandidate.Should().NotBeNull();
    }

    [Fact]
    public async Task UploadCandidateAsync_CreatesProfileWithParsedData()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf");
        var parsedDto = TestHelpers.CreateTestCandidateProfileDTO(job.Id, "Jane Smith", 92);
        parsedDto.Skills = new List<string> { "C#", "SQL", "Azure" };

        _mockParser.Setup(p => p.ParseCandidateFromFileAsync(
                It.IsAny<IFormFile>(), job.Id, job.Title, job.Description))
            .ReturnsAsync(parsedDto);

        _mockFileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("unique_filename.pdf");

        // Act
        var result = await _candidateService.UploadCandidateAsync(job.Id, mockFile);

        // Assert
        var profile = await _context.CandidateProfiles
            .FirstOrDefaultAsync(p => p.CandidateId == result.Id);

        profile.Should().NotBeNull();
        profile!.Name.Should().Be("Jane Smith");
        profile.MatchScore.Should().Be(92);
        profile.Email.Should().Be(parsedDto.Email);

        var skills = JsonSerializer.Deserialize<List<string>>(profile.Skills);
        skills.Should().Contain(new[] { "C#", "SQL", "Azure" });
    }

    [Fact]
    public async Task UploadCandidateAsync_WithNullFile_ThrowsArgumentException()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _candidateService.UploadCandidateAsync(job.Id, null!));
    }

    [Fact]
    public async Task UploadCandidateAsync_WithEmptyFile_ThrowsArgumentException()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _candidateService.UploadCandidateAsync(job.Id, mockFile.Object));
    }

    [Fact]
    public async Task UploadCandidateAsync_WithInvalidJobId_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _candidateService.UploadCandidateAsync(999, mockFile));
    }

    [Fact]
    public async Task UploadCandidateAsync_SavesFileWithCorrectFilename()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf");
        var parsedDto = TestHelpers.CreateTestCandidateProfileDTO(job.Id);

        _mockParser.Setup(p => p.ParseCandidateFromFileAsync(
                It.IsAny<IFormFile>(), job.Id, job.Title, job.Description))
            .ReturnsAsync(parsedDto);

        _mockFileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("guid_test_cv.pdf");

        // Act
        await _candidateService.UploadCandidateAsync(job.Id, mockFile);

        // Assert
        _mockFileStorage.Verify(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()), Times.Once);

        var profile = await _context.CandidateProfiles.FirstOrDefaultAsync();
        profile.Should().NotBeNull();
        profile!.FileName.Should().Be("guid_test_cv.pdf");
    }

    [Fact]
    public async Task UploadCandidateAsync_ParserFails_CandidateStillCreated()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf");

        _mockParser.Setup(p => p.ParseCandidateFromFileAsync(
                It.IsAny<IFormFile>(), job.Id, job.Title, job.Description))
            .ThrowsAsync(new Exception("Parser error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _candidateService.UploadCandidateAsync(job.Id, mockFile));

        // Verify candidate was created before parser failed
        var candidate = await _context.Candidates.FirstOrDefaultAsync();
        candidate.Should().NotBeNull();
        candidate!.Name.Should().Be("Parsing..."); // Initial value before parsing
    }

    [Fact]
    public async Task UploadCandidateAsync_WithParserReturningNull_HandlesGracefully()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf");

        _mockParser.Setup(p => p.ParseCandidateFromFileAsync(
                It.IsAny<IFormFile>(), job.Id, job.Title, job.Description))
            .ReturnsAsync((CandidateProfileDTO?)null);

        _mockFileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("unique_filename.pdf");

        // Act
        var result = await _candidateService.UploadCandidateAsync(job.Id, mockFile);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Parsing..."); // Should remain as initial value

        // No profile should be created
        var profile = await _context.CandidateProfiles.FirstOrDefaultAsync();
        profile.Should().BeNull();
    }

    [Fact]
    public async Task UploadCandidateAsync_LogsSuccessfulUpload()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var mockFile = TestHelpers.CreateMockFormFile("test_cv.pdf");
        var parsedDto = TestHelpers.CreateTestCandidateProfileDTO(job.Id);

        _mockParser.Setup(p => p.ParseCandidateFromFileAsync(
                It.IsAny<IFormFile>(), job.Id, job.Title, job.Description))
            .ReturnsAsync(parsedDto);

        _mockFileStorage.Setup(fs => fs.SaveFileAsync(It.IsAny<IFormFile>()))
            .ReturnsAsync("unique_filename.pdf");

        // Act
        await _candidateService.UploadCandidateAsync(job.Id, mockFile);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully uploaded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetCandidatesForJobAsync Tests

    [Fact]
    public async Task GetCandidatesForJobAsync_FiltersByJobId()
    {
        // Arrange
        var (job, candidates, profiles) = await TestHelpers.SeedTestDataAsync(_context);

        // Act
        var result = await _candidateService.GetCandidatesForJobAsync(job.Id);
        var candidateList = result.ToList();

        // Assert
        candidateList.Should().HaveCount(3);
        candidateList.Should().AllSatisfy(c => c.JobId.Should().Be(job.Id));
    }

    [Fact]
    public async Task GetCandidatesForJobAsync_ReturnsCorrectProperties()
    {
        // Arrange
        var (job, candidates, profiles) = await TestHelpers.SeedTestDataAsync(_context);

        // Act
        var result = await _candidateService.GetCandidatesForJobAsync(job.Id);
        var candidateList = result.ToList();

        // Assert
        var firstCandidate = candidateList.First();
        firstCandidate.Id.Should().BeGreaterThan(0);
        firstCandidate.JobId.Should().Be(job.Id);
        firstCandidate.Name.Should().NotBeNullOrEmpty();
        firstCandidate.MatchScore.Should().BeInRange(0, 100);
        firstCandidate.ExperienceYears.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetCandidatesForJobAsync_WithNoProfiles_ReturnsEmptyList()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Act
        var result = await _candidateService.GetCandidatesForJobAsync(job.Id);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetCandidatesWithMatchScoreAsync Tests

    [Fact]
    public async Task GetCandidatesWithMatchScoreAsync_ReturnsMatchScores()
    {
        // Arrange
        var (job, candidates, profiles) = await TestHelpers.SeedTestDataAsync(_context);

        // Act
        var result = await _candidateService.GetCandidatesWithMatchScoreAsync(job.Id);
        var candidateList = result.ToList();

        // Assert
        candidateList.Should().HaveCount(3);
        candidateList.Should().AllSatisfy(c =>
        {
            c.Id.Should().BeGreaterThan(0);
            c.MatchScore.Should().BeInRange(0, 100);
        });
    }

    [Fact]
    public async Task GetCandidatesWithMatchScoreAsync_WithCandidatesWithoutProfiles_ReturnsZeroMatchScore()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Add candidate without profile
        var candidate = TestHelpers.CreateTestCandidate(job.Id, "No Profile Candidate");
        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        // Act
        var result = await _candidateService.GetCandidatesWithMatchScoreAsync(job.Id);
        var candidateList = result.ToList();

        // Assert
        candidateList.Should().HaveCount(1);
        var firstCandidate = candidateList.First();
        firstCandidate.MatchScore.Should().Be(0);
    }

    #endregion

    #region GetProfileAsync Tests

    [Fact]
    public async Task GetProfileAsync_WithExistingProfile_ReturnsProfile()
    {
        // Arrange
        var (job, candidates, profiles) = await TestHelpers.SeedTestDataAsync(_context);
        var profileId = profiles[0].Id;

        // Act
        var result = await _candidateService.GetProfileAsync(profileId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(profileId);
        result.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistentProfile_ReturnsNull()
    {
        // Act
        var result = await _candidateService.GetProfileAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region SaveProfileAsync Tests

    [Fact]
    public async Task SaveProfileAsync_CreatesProfileFromDTO()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var dto = TestHelpers.CreateTestCandidateProfileDTO(job.Id, "New Candidate", 88);

        // Act
        var result = await _candidateService.SaveProfileAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Candidate");
        result.MatchScore.Should().Be(88);
        result.JobId.Should().Be(job.Id);

        // Verify in database
        var dbProfile = await _context.CandidateProfiles.FindAsync(result.Id);
        dbProfile.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveProfileAsync_SerializesListsToJsonb()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var dto = TestHelpers.CreateTestCandidateProfileDTO(job.Id);
        dto.Skills = new List<string> { "Python", "Django", "PostgreSQL" };
        dto.Strengths = new List<string> { "Strong problem solver" };
        dto.Weaknesses = new List<string> { "Limited frontend experience" };

        // Act
        var result = await _candidateService.SaveProfileAsync(dto);

        // Assert
        var skills = JsonSerializer.Deserialize<List<string>>(result.Skills);
        skills.Should().Contain(new[] { "Python", "Django", "PostgreSQL" });

        var strengths = JsonSerializer.Deserialize<List<string>>(result.Strengths);
        strengths.Should().Contain("Strong problem solver");

        var weaknesses = JsonSerializer.Deserialize<List<string>>(result.Weaknesses);
        weaknesses.Should().Contain("Limited frontend experience");
    }

    [Fact]
    public async Task SaveProfileAsync_LogsSaveOperation()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var dto = TestHelpers.CreateTestCandidateProfileDTO(job.Id, "Test User");

        // Act
        await _candidateService.SaveProfileAsync(dto);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Saved candidate profile")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetCandidateCvStreamAsync Tests

    [Fact]
    public async Task GetCandidateCvStreamAsync_WithValidProfile_ReturnsStream()
    {
        // Arrange
        var (job, candidates, profiles) = await TestHelpers.SeedTestDataAsync(_context);
        var profile = profiles[0];

        var mockStream = new MemoryStream();
        _mockFileStorage.Setup(fs => fs.GetFileStreamAsync(profile.FileName))
            .ReturnsAsync(mockStream);

        // Act
        var result = await _candidateService.GetCandidateCvStreamAsync(profile.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(mockStream);
        _mockFileStorage.Verify(fs => fs.GetFileStreamAsync(profile.FileName), Times.Once);
    }

    [Fact]
    public async Task GetCandidateCvStreamAsync_WithNonExistentProfile_ReturnsNull()
    {
        // Act
        var result = await _candidateService.GetCandidateCvStreamAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCandidateCvStreamAsync_WithEmptyFileName_ReturnsNull()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var candidate = TestHelpers.CreateTestCandidate(job.Id);
        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        var profile = new CandidateProfile
        {
            JobId = job.Id,
            CandidateId = candidate.Id,
            Name = "Test",
            FileName = "", // Empty filename
            Skills = "[]",
            Strengths = "[]",
            Weaknesses = "[]"
        };
        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _candidateService.GetCandidateCvStreamAsync(profile.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
