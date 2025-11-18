using CVision.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CVision.Api.Tests.Controllers;

/// <summary>
/// Unit tests for JobsController, focusing on HTTP responses and validation
/// </summary>
public class JobsControllerTests
{
    private readonly Mock<IJobService> _mockJobService;
    private readonly Mock<ICandidateService> _mockCandidateService;
    private readonly JobsController _controller;

    public JobsControllerTests()
    {
        _mockJobService = new Mock<IJobService>();
        _mockCandidateService = new Mock<ICandidateService>();
        _controller = new JobsController(_mockJobService.Object, _mockCandidateService.Object);
    }

    #region GetJobs Tests

    [Fact]
    public async Task GetJobs_ReturnsOkResultWithJobs()
    {
        // Arrange
        var jobs = new List<JobWithCountDto>
        {
            new JobWithCountDto { Id = 1, Title = "Developer", ApplicantCount = 5 },
            new JobWithCountDto { Id = 2, Title = "Designer", ApplicantCount = 3 }
        };

        _mockJobService
            .Setup(s => s.GetAllJobsAsync())
            .ReturnsAsync(jobs);

        // Act
        var result = await _controller.GetJobs();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(jobs);
    }

    [Fact]
    public async Task GetJobs_WithNoJobs_ReturnsEmptyList()
    {
        // Arrange
        _mockJobService
            .Setup(s => s.GetAllJobsAsync())
            .ReturnsAsync(new List<JobWithCountDto>());

        // Act
        var result = await _controller.GetJobs();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var jobs = okResult!.Value as IEnumerable<JobWithCountDto>;
        jobs.Should().BeEmpty();
    }

    #endregion

    #region GetJob Tests

    [Fact]
    public async Task GetJob_WithExistingJob_ReturnsOkResult()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("Developer", "Backend position");

        _mockJobService
            .Setup(s => s.GetJobByIdAsync(1))
            .ReturnsAsync(job);

        // Act
        var result = await _controller.GetJob(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(job);
    }

    [Fact]
    public async Task GetJob_WithNonExistentJob_ReturnsNotFound()
    {
        // Arrange
        _mockJobService
            .Setup(s => s.GetJobByIdAsync(999))
            .ReturnsAsync((Job?)null);

        // Act
        var result = await _controller.GetJob(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result as NotFoundObjectResult;
        notFound!.Value.Should().BeEquivalentTo(new { error = "Job not found" });
    }

    #endregion

    #region GetCandidatesForJob Tests

    [Fact]
    public async Task GetCandidatesForJob_ReturnsOkResultWithCandidates()
    {
        // Arrange
        var candidates = new List<CandidateWithMatchScoreDto>
        {
            new CandidateWithMatchScoreDto { Id = 1, Name = "John Doe", MatchScore = 85, JobId = 1 },
            new CandidateWithMatchScoreDto { Id = 2, Name = "Jane Smith", MatchScore = 92, JobId = 1 }
        };

        _mockCandidateService
            .Setup(s => s.GetCandidatesWithMatchScoreAsync(1))
            .ReturnsAsync(candidates);

        // Act
        var result = await _controller.GetCandidatesForJob(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(candidates);
    }

    [Fact]
    public async Task GetCandidatesForJob_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        _mockCandidateService
            .Setup(s => s.GetCandidatesWithMatchScoreAsync(1))
            .ReturnsAsync(new List<CandidateWithMatchScoreDto>());

        // Act
        var result = await _controller.GetCandidatesForJob(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var candidates = okResult!.Value as IEnumerable<object>;
        candidates.Should().BeEmpty();
    }

    #endregion

    #region CreateJob Tests

    [Fact]
    public async Task CreateJob_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("Software Engineer", "Full-stack developer");
        job.Id = 1; // Simulate database assignment

        _mockJobService
            .Setup(s => s.CreateJobAsync(It.IsAny<Job>()))
            .ReturnsAsync(job);

        // Act
        var result = await _controller.CreateJob(job);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.ActionName.Should().Be(nameof(JobsController.GetJob));
        createdResult.RouteValues!["id"].Should().Be(1);
        createdResult.Value.Should().Be(job);
    }

    [Fact]
    public async Task CreateJob_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("", "Valid description");

        _mockJobService
            .Setup(s => s.CreateJobAsync(It.IsAny<Job>()))
            .ThrowsAsync(new ArgumentException("Job title is required"));

        // Act
        var result = await _controller.CreateJob(job);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeEquivalentTo(new { error = "Job title is required" });
    }

    [Fact]
    public async Task CreateJob_WithEmptyDescription_ReturnsBadRequest()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("Valid Title", "");

        _mockJobService
            .Setup(s => s.CreateJobAsync(It.IsAny<Job>()))
            .ThrowsAsync(new ArgumentException("Job description is required"));

        // Act
        var result = await _controller.CreateJob(job);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result as BadRequestObjectResult;
        badRequest!.Value.Should().BeEquivalentTo(new { error = "Job description is required" });
    }

    [Fact]
    public async Task CreateJob_WithServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob();

        _mockJobService
            .Setup(s => s.CreateJobAsync(It.IsAny<Job>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateJob(job);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateJob_CallsServiceWithCorrectJob()
    {
        // Arrange
        var job = TestHelpers.CreateTestJob("Test Job", "Test Description");
        job.Id = 1;

        _mockJobService
            .Setup(s => s.CreateJobAsync(It.IsAny<Job>()))
            .ReturnsAsync(job);

        // Act
        await _controller.CreateJob(job);

        // Assert
        _mockJobService.Verify(
            s => s.CreateJobAsync(It.Is<Job>(j =>
                j.Title == "Test Job" &&
                j.Description == "Test Description")),
            Times.Once);
    }

    #endregion

    #region DeleteJob Tests

    [Fact]
    public async Task DeleteJob_WithExistingJob_ReturnsNoContent()
    {
        // Arrange
        _mockJobService
            .Setup(s => s.DeleteJobAsync(1))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteJob(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteJob_WithNonExistentJob_ReturnsNotFound()
    {
        // Arrange
        _mockJobService
            .Setup(s => s.DeleteJobAsync(999))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteJob(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFound = result as NotFoundObjectResult;
        notFound!.Value.Should().BeEquivalentTo(new { error = "Job not found" });
    }

    [Fact]
    public async Task DeleteJob_CallsServiceWithCorrectId()
    {
        // Arrange
        _mockJobService
            .Setup(s => s.DeleteJobAsync(42))
            .ReturnsAsync(true);

        // Act
        await _controller.DeleteJob(42);

        // Assert
        _mockJobService.Verify(
            s => s.DeleteJobAsync(42),
            Times.Once);
    }

    #endregion

    #region GetSkillDistribution Tests

    [Fact]
    public async Task GetSkillDistribution_ReturnsOkResultWithSkills()
    {
        // Arrange
        var skills = new List<SkillDistributionDto>
        {
            new SkillDistributionDto { Skill = "C#", Count = 5 },
            new SkillDistributionDto { Skill = "SQL", Count = 3 },
            new SkillDistributionDto { Skill = "React", Count = 2 }
        };

        _mockJobService
            .Setup(s => s.GetSkillDistributionAsync(1))
            .ReturnsAsync(skills);

        // Act
        var result = await _controller.GetSkillDistribution(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().Be(skills);
    }

    [Fact]
    public async Task GetSkillDistribution_WithNoSkills_ReturnsEmptyList()
    {
        // Arrange
        _mockJobService
            .Setup(s => s.GetSkillDistributionAsync(1))
            .ReturnsAsync(new List<SkillDistributionDto>());

        // Act
        var result = await _controller.GetSkillDistribution(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var skills = okResult!.Value as IEnumerable<SkillDistributionDto>;
        skills.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSkillDistribution_CallsServiceWithCorrectJobId()
    {
        // Arrange
        _mockJobService
            .Setup(s => s.GetSkillDistributionAsync(42))
            .ReturnsAsync(new List<SkillDistributionDto>());

        // Act
        await _controller.GetSkillDistribution(42);

        // Assert
        _mockJobService.Verify(
            s => s.GetSkillDistributionAsync(42),
            Times.Once);
    }

    #endregion
}
