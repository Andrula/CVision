using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CVision.Api.Tests.Helpers;

/// <summary>
/// Provides helper methods and mock data for unit tests
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates an in-memory database context for testing
    /// </summary>
    public static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Seeds the database with test data
    /// </summary>
    public static async Task<(Job job, List<Candidate> candidates, List<CandidateProfile> profiles)> SeedTestDataAsync(AppDbContext context)
    {
        var job = CreateTestJob();
        context.Jobs.Add(job);
        await context.SaveChangesAsync();

        var candidates = new List<Candidate>
        {
            CreateTestCandidate(job.Id, "John Doe", "john_doe_cv.pdf"),
            CreateTestCandidate(job.Id, "Jane Smith", "jane_smith_cv.pdf"),
            CreateTestCandidate(job.Id, "Bob Johnson", "bob_johnson_cv.pdf")
        };
        context.Candidates.AddRange(candidates);
        await context.SaveChangesAsync();

        var profiles = new List<CandidateProfile>
        {
            CreateTestCandidateProfile(job.Id, candidates[0].Id, "John Doe", new[] { "C#", "ASP.NET", "SQL" }, 85),
            CreateTestCandidateProfile(job.Id, candidates[1].Id, "Jane Smith", new[] { "Python", "Django", "PostgreSQL" }, 78),
            CreateTestCandidateProfile(job.Id, candidates[2].Id, "Bob Johnson", new[] { "C#", "JavaScript", "React" }, 92)
        };
        context.CandidateProfiles.AddRange(profiles);
        await context.SaveChangesAsync();

        return (job, candidates, profiles);
    }

    /// <summary>
    /// Creates a test Job entity
    /// </summary>
    public static Job CreateTestJob(string title = "Software Developer", string description = "Looking for a skilled developer")
    {
        return new Job
        {
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test Candidate entity
    /// </summary>
    public static Candidate CreateTestCandidate(int jobId, string name = "Test Candidate", string fileName = "test_cv.pdf")
    {
        return new Candidate
        {
            Name = name,
            FileName = fileName,
            JobId = jobId,
            UploadedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test CandidateProfile entity
    /// </summary>
    public static CandidateProfile CreateTestCandidateProfile(
        int jobId,
        int candidateId,
        string name = "Test Candidate",
        string[]? skills = null,
        int matchScore = 80)
    {
        skills ??= new[] { "C#", ".NET", "SQL" };

        return new CandidateProfile
        {
            JobId = jobId,
            CandidateId = candidateId,
            Name = name,
            Email = $"{name.Replace(" ", ".").ToLower()}@example.com",
            Phone = "+45 12345678",
            Location = "Copenhagen, Denmark",
            FileName = $"{name.Replace(" ", "_").ToLower()}_cv.pdf",
            ExperienceYears = 5,
            ProfileSummary = $"Experienced developer with {matchScore}% match",
            MatchScore = matchScore,
            Skills = JsonSerializer.Serialize(skills),
            Strengths = JsonSerializer.Serialize(new[] { "Strong technical skills", "Good communication", "Team player" }),
            Weaknesses = JsonSerializer.Serialize(new[] { "Limited experience with cloud platforms", "Could improve testing skills" }),
            AnalysisSummary = "Strong candidate with relevant experience",
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test CandidateProfileDTO
    /// </summary>
    public static CandidateProfileDTO CreateTestCandidateProfileDTO(
        int jobId = 1,
        string name = "Test Candidate",
        int matchScore = 80)
    {
        return new CandidateProfileDTO
        {
            JobId = jobId,
            Name = name,
            Email = $"{name.Replace(" ", ".").ToLower()}@example.com",
            Phone = "+45 12345678",
            Location = "Copenhagen, Denmark",
            ExperienceYears = 5,
            ProfileSummary = "Experienced developer",
            MatchScore = matchScore,
            Skills = new List<string> { "C#", ".NET", "SQL" },
            Strengths = new List<string> { "Strong technical skills", "Good communication" },
            Weaknesses = new List<string> { "Limited cloud experience" },
            AnalysisSummary = "Strong candidate with relevant experience"
        };
    }

    /// <summary>
    /// Creates a mock IFormFile for testing file uploads
    /// </summary>
    public static IFormFile CreateMockFormFile(string fileName = "test.pdf", string content = "Mock PDF content")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(bytes.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream target, CancellationToken token) =>
            {
                stream.Position = 0;
                return stream.CopyToAsync(target, token);
            });

        return mockFile.Object;
    }

    /// <summary>
    /// Converts a string array to JSONB format (serialized JSON)
    /// </summary>
    public static string ToJsonb(params string[] items)
    {
        return JsonSerializer.Serialize(items);
    }

    /// <summary>
    /// Deserializes JSONB string to string array
    /// </summary>
    public static string[]? FromJsonb(string? jsonb)
    {
        if (string.IsNullOrEmpty(jsonb))
            return null;

        return JsonSerializer.Deserialize<string[]>(jsonb);
    }
}
