using CVision.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace CVision.Api.Tests.Services;

/// <summary>
/// Unit tests for JobService, focusing on skill distribution and job management
/// </summary>
public class JobServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<JobService>> _mockLogger;
    private readonly JobService _jobService;

    public JobServiceTests()
    {
        _context = TestHelpers.CreateInMemoryDbContext();
        _mockLogger = new Mock<ILogger<JobService>>();
        _jobService = new JobService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetSkillDistributionAsync Tests

    [Fact]
    public async Task GetSkillDistributionAsync_WithMultipleCandidates_GroupsAndCountsSkillsCorrectly()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var candidate1 = TestHelpers.CreateTestCandidate(job.Id, "John Doe");
        var candidate2 = TestHelpers.CreateTestCandidate(job.Id, "Jane Smith");
        var candidate3 = TestHelpers.CreateTestCandidate(job.Id, "Bob Johnson");
        _context.Candidates.AddRange(candidate1, candidate2, candidate3);
        await _context.SaveChangesAsync();

        var profile1 = TestHelpers.CreateTestCandidateProfile(job.Id, candidate1.Id, "John Doe", new[] { "C#", "SQL", "JavaScript" }, 85);
        var profile2 = TestHelpers.CreateTestCandidateProfile(job.Id, candidate2.Id, "Jane Smith", new[] { "C#", "Python", "JavaScript" }, 78);
        var profile3 = TestHelpers.CreateTestCandidateProfile(job.Id, candidate3.Id, "Bob Johnson", new[] { "C#", "React", "SQL" }, 92);
        _context.CandidateProfiles.AddRange(profile1, profile2, profile3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetSkillDistributionAsync(job.Id);
        var skillList = result.ToList();

        // Assert
        skillList.Should().NotBeEmpty();
        skillList.Should().HaveCount(5); // C#, SQL, JavaScript, Python, React

        // C# should appear 3 times (most common)
        var csharpSkill = skillList.First();
        csharpSkill.Skill.Should().Be("C#");
        csharpSkill.Count.Should().Be(3);

        // SQL and JavaScript should appear 2 times each
        var sqlSkill = skillList.FirstOrDefault(s => s.Skill == "SQL");
        sqlSkill.Should().NotBeNull();
        sqlSkill!.Count.Should().Be(2);

        var jsSkill = skillList.FirstOrDefault(s => s.Skill == "JavaScript");
        jsSkill.Should().NotBeNull();
        jsSkill!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetSkillDistributionAsync_WithDuplicateSkills_CountsCorrectly()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var candidate1 = TestHelpers.CreateTestCandidate(job.Id);
        var candidate2 = TestHelpers.CreateTestCandidate(job.Id);
        _context.Candidates.AddRange(candidate1, candidate2);
        await _context.SaveChangesAsync();

        // Both candidates have "C#" skill
        var profile1 = TestHelpers.CreateTestCandidateProfile(job.Id, candidate1.Id, skills: new[] { "C#", "SQL" });
        var profile2 = TestHelpers.CreateTestCandidateProfile(job.Id, candidate2.Id, skills: new[] { "C#", "Python" });
        _context.CandidateProfiles.AddRange(profile1, profile2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetSkillDistributionAsync(job.Id);
        var skillList = result.ToList();

        // Assert
        var csharpSkill = skillList.FirstOrDefault(s => s.Skill == "C#");
        csharpSkill.Should().NotBeNull();
        csharpSkill!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetSkillDistributionAsync_WithNoProfiles_ReturnsEmptyList()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetSkillDistributionAsync(job.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkillDistributionAsync_WithNullSkills_HandlesGracefully()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var candidate = TestHelpers.CreateTestCandidate(job.Id);
        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        // Test with empty JSON array instead of null since Skills is required
        var profile = new CandidateProfile
        {
            JobId = job.Id,
            CandidateId = candidate.Id,
            Name = "Test",
            Skills = "[]", // Empty JSON array
            Strengths = "[]",
            Weaknesses = "[]"
        };
        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetSkillDistributionAsync(job.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkillDistributionAsync_WithEmptySkillsArray_ReturnsEmptyList()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var candidate = TestHelpers.CreateTestCandidate(job.Id);
        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        var profile = TestHelpers.CreateTestCandidateProfile(job.Id, candidate.Id, skills: Array.Empty<string>());
        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetSkillDistributionAsync(job.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkillDistributionAsync_SortsSkillsByCountDescending()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var candidate1 = TestHelpers.CreateTestCandidate(job.Id);
        var candidate2 = TestHelpers.CreateTestCandidate(job.Id);
        var candidate3 = TestHelpers.CreateTestCandidate(job.Id);
        _context.Candidates.AddRange(candidate1, candidate2, candidate3);
        await _context.SaveChangesAsync();

        // C# appears 3 times, SQL 2 times, Python 1 time
        var profile1 = TestHelpers.CreateTestCandidateProfile(job.Id, candidate1.Id, skills: new[] { "C#", "SQL" });
        var profile2 = TestHelpers.CreateTestCandidateProfile(job.Id, candidate2.Id, skills: new[] { "C#", "SQL" });
        var profile3 = TestHelpers.CreateTestCandidateProfile(job.Id, candidate3.Id, skills: new[] { "C#", "Python" });
        _context.CandidateProfiles.AddRange(profile1, profile2, profile3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetSkillDistributionAsync(job.Id);
        var skillList = result.ToList();

        // Assert
        skillList.Should().HaveCount(3);
        skillList[0].Skill.Should().Be("C#");
        skillList[0].Count.Should().Be(3);
        skillList[1].Skill.Should().Be("SQL");
        skillList[1].Count.Should().Be(2);
        skillList[2].Skill.Should().Be("Python");
        skillList[2].Count.Should().Be(1);
    }

    [Fact]
    public async Task GetSkillDistributionAsync_FiltersWhitespaceSkills()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var candidate = TestHelpers.CreateTestCandidate(job.Id);
        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        // Include whitespace and empty strings in skills
        var profile = new CandidateProfile
        {
            JobId = job.Id,
            CandidateId = candidate.Id,
            Name = "Test",
            Skills = JsonSerializer.Serialize(new[] { "C#", "  ", "", "SQL", "\t" }),
            Strengths = "[]",
            Weaknesses = "[]"
        };
        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetSkillDistributionAsync(job.Id);
        var skillList = result.ToList();

        // Assert
        skillList.Should().HaveCount(2); // Only "C#" and "SQL"
        skillList.Should().Contain(s => s.Skill == "C#");
        skillList.Should().Contain(s => s.Skill == "SQL");
    }

    #endregion

    #region GetAllJobsAsync Tests

    [Fact]
    public async Task GetAllJobsAsync_ReturnsAllJobsWithApplicantCounts()
    {
        // Arrange
        var job1 = TestHelpers.CreateTestJob("Developer", "Backend dev");
        var job2 = TestHelpers.CreateTestJob("Designer", "UI/UX designer");
        _context.Jobs.AddRange(job1, job2);
        await _context.SaveChangesAsync();

        // Add 3 candidates to job1, 1 to job2
        _context.Candidates.AddRange(
            TestHelpers.CreateTestCandidate(job1.Id),
            TestHelpers.CreateTestCandidate(job1.Id),
            TestHelpers.CreateTestCandidate(job1.Id),
            TestHelpers.CreateTestCandidate(job2.Id)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetAllJobsAsync();
        var jobs = result.ToList();

        // Assert
        jobs.Should().HaveCount(2);
        jobs.First(j => j.Id == job1.Id).ApplicantCount.Should().Be(3);
        jobs.First(j => j.Id == job2.Id).ApplicantCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAllJobsAsync_WithNoJobs_ReturnsEmptyList()
    {
        // Act
        var result = await _jobService.GetAllJobsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CreateJobAsync Tests

    [Fact]
    public async Task CreateJobAsync_WithValidData_CreatesJob()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("Software Engineer", "Seeking experienced engineer");

        // Act
        var result = await _jobService.CreateJobAsync(job);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Software Engineer");
        result.Description.Should().Be("Seeking experienced engineer");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify it's in the database
        var dbJob = await _context.Jobs.FindAsync(result.Id);
        dbJob.Should().NotBeNull();
        dbJob!.Title.Should().Be("Software Engineer");
    }

    [Fact]
    public async Task CreateJobAsync_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("", "Valid description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _jobService.CreateJobAsync(job));
    }

    [Fact]
    public async Task CreateJobAsync_WithNullTitle_ThrowsArgumentException()
    {
        // Arrange
        var job = new Job
        {
            Title = null!,
            Description = "Valid description"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _jobService.CreateJobAsync(job));
    }

    [Fact]
    public async Task CreateJobAsync_WithEmptyDescription_ThrowsArgumentException()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("Valid Title", "");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _jobService.CreateJobAsync(job));
    }

    [Fact]
    public async Task CreateJobAsync_WithWhitespaceTitle_ThrowsArgumentException()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("   ", "Valid description");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _jobService.CreateJobAsync(job));
    }

    [Fact]
    public async Task CreateJobAsync_LogsJobCreation()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();

        // Act
        await _jobService.CreateJobAsync(job);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created job")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region DeleteJobAsync Tests

    [Fact]
    public async Task DeleteJobAsync_WithExistingJob_DeletesAndReturnsTrue()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.DeleteJobAsync(job.Id);

        // Assert
        result.Should().BeTrue();

        // Verify it's deleted from database
        var dbJob = await _context.Jobs.FindAsync(job.Id);
        dbJob.Should().BeNull();
    }

    [Fact]
    public async Task DeleteJobAsync_WithNonExistentJob_ReturnsFalse()
    {
        // Act
        var result = await _jobService.DeleteJobAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteJobAsync_LogsJobDeletion()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Act
        await _jobService.DeleteJobAsync(job.Id);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deleted job")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetJobByIdAsync Tests

    [Fact]
    public async Task GetJobByIdAsync_WithExistingJob_ReturnsJob()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("Test Job", "Test Description");
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        // Act
        var result = await _jobService.GetJobByIdAsync(job.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
        result.Title.Should().Be("Test Job");
        result.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task GetJobByIdAsync_WithNonExistentJob_ReturnsNull()
    {
        // Act
        var result = await _jobService.GetJobByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
