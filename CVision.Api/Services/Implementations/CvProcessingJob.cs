using System.Text.Json;
using CVision.Api.Data;
using CVision.Api.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CVision.Api.Services.Implementations;

public class CvProcessingJob : ICvProcessingJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CvProcessingJob> _logger;

    public CvProcessingJob(
        IServiceProvider serviceProvider,
        ILogger<CvProcessingJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ProcessCandidateAsync(int candidateId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var parser = scope.ServiceProvider.GetRequiredService<IPythonCvParserService>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        var candidate = await context.Candidates
            .Include(c => c.Job)
            .FirstOrDefaultAsync(c => c.Id == candidateId);

        if (candidate == null)
        {
            _logger.LogWarning("Candidate {CandidateId} not found for processing", candidateId);
            return;
        }

        try
        {
            // Update status to Processing
            candidate.Status = ProcessingStatus.Processing;
            await context.SaveChangesAsync();

            _logger.LogInformation("Processing candidate {CandidateId} with hash {FileHash}",
                candidateId, candidate.FileHash);

            // Check for cached result
            var cachedCandidate = await context.Candidates
                .Include(c => c.Job)
                .Where(c => c.FileHash == candidate.FileHash
                    && c.JobId == candidate.JobId
                    && c.Id != candidateId
                    && c.ParsedAt != null
                    && c.Status == ProcessingStatus.Completed)
                .FirstOrDefaultAsync();

            if (cachedCandidate != null)
            {
                _logger.LogInformation("Found cached result for candidate {CandidateId}, copying from candidate {CachedId}",
                    candidateId, cachedCandidate.Id);

                // Get the cached profile
                var cachedProfile = await context.CandidateProfiles
                    .FirstOrDefaultAsync(p => p.CandidateId == cachedCandidate.Id);

                if (cachedProfile != null)
                {
                    // Copy the profile
                    var newProfile = new CandidateProfile
                    {
                        JobId = candidate.JobId,
                        Name = cachedProfile.Name,
                        Email = cachedProfile.Email,
                        Phone = cachedProfile.Phone,
                        Location = cachedProfile.Location,
                        FileName = cachedProfile.FileName,
                        ExperienceYears = cachedProfile.ExperienceYears,
                        ProfileSummary = cachedProfile.ProfileSummary,
                        MatchScore = cachedProfile.MatchScore,
                        Skills = cachedProfile.Skills,
                        Strengths = cachedProfile.Strengths,
                        Weaknesses = cachedProfile.Weaknesses,
                        AnalysisSummary = cachedProfile.AnalysisSummary,
                        CreatedAt = DateTime.UtcNow,
                        CandidateId = candidate.Id
                    };

                    context.CandidateProfiles.Add(newProfile);

                    candidate.Name = cachedProfile.Name;
                    candidate.ParsedAt = DateTime.UtcNow;
                    candidate.Status = ProcessingStatus.Cached;

                    await context.SaveChangesAsync();

                    _logger.LogInformation("Successfully used cached result for candidate {CandidateId}", candidateId);
                    return;
                }
            }

            // No cache found, parse the CV
            _logger.LogInformation("No cache found, parsing CV for candidate {CandidateId}", candidateId);

            // Get the file from storage
            var filePath = Path.Combine("uploads", candidate.FileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CV file not found: {candidate.FileName}");
            }

            using var fileStream = File.OpenRead(filePath);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Create a FormFile from the stream
            var formFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", candidate.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var parsed = await parser.ParseCandidateFromFileAsync(
                formFile,
                candidate.JobId,
                candidate.Job.Title,
                candidate.Job.Description,
                candidate.Language);

            if (parsed != null)
            {
                // Save file if not already saved
                string uniqueFileName;
                try
                {
                    // Check if file already exists in storage
                    await fileStorage.GetFileStreamAsync(candidate.FileName);
                    uniqueFileName = candidate.FileName;
                }
                catch
                {
                    // File doesn't exist, save it
                    memoryStream.Position = 0;
                    var saveFormFile = new FormFile(memoryStream, 0, memoryStream.Length, "file", candidate.FileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = "application/pdf"
                    };
                    uniqueFileName = await fileStorage.SaveFileAsync(saveFormFile);
                }

                var profile = new CandidateProfile
                {
                    JobId = candidate.JobId,
                    Name = parsed.Name,
                    Email = parsed.Email,
                    Phone = parsed.Phone,
                    Location = parsed.Location,
                    FileName = uniqueFileName,
                    ExperienceYears = parsed.ExperienceYears,
                    ProfileSummary = parsed.ProfileSummary,
                    MatchScore = parsed.MatchScore,
                    Skills = JsonSerializer.Serialize(parsed.Skills),
                    Strengths = JsonSerializer.Serialize(parsed.Strengths),
                    Weaknesses = JsonSerializer.Serialize(parsed.Weaknesses),
                    AnalysisSummary = parsed.AnalysisSummary,
                    CreatedAt = DateTime.UtcNow,
                    CandidateId = candidate.Id
                };

                context.CandidateProfiles.Add(profile);

                candidate.Name = parsed.Name;
                candidate.ParsedAt = DateTime.UtcNow;
                candidate.Status = ProcessingStatus.Completed;
                candidate.ErrorMessage = null;

                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully processed candidate {CandidateId}", candidateId);
            }
            else
            {
                throw new InvalidOperationException("Parser returned null result");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing candidate {CandidateId}: {Error}",
                candidateId, ex.Message);

            candidate.Status = ProcessingStatus.Failed;
            candidate.ErrorMessage = ex.Message;

            await context.SaveChangesAsync();

            throw; // Re-throw to trigger Hangfire retry
        }
    }
}
