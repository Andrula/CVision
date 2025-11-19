using CVision.Api.Utils;
using Hangfire;

namespace CVision.Api.Services.Implementations;

public class CandidateService : ICandidateService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IPythonCvParserService _parser;
    private readonly ILogger<CandidateService> _logger;
    private readonly IBackgroundJobClient _backgroundJobs;

    public CandidateService(
        AppDbContext context,
        IFileStorageService fileStorage,
        IPythonCvParserService parser,
        ILogger<CandidateService> logger,
        IBackgroundJobClient backgroundJobs)
    {
        _context = context;
        _fileStorage = fileStorage;
        _parser = parser;
        _logger = logger;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<IEnumerable<object>> GetCandidatesForJobAsync(int jobId)
    {
        return await _context.CandidateProfiles
            .Where(p => p.JobId == jobId)
            .Select(p => new
            {
                p.Id,
                p.JobId,
                p.Name,
                p.MatchScore,
                p.ExperienceYears
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<object>> GetCandidatesWithMatchScoreAsync(int jobId)
    {
        return await _context.Candidates
            .Where(c => c.JobId == jobId)
            .Select(c => new
            {
                c.Id,
                c.JobId,
                c.Name,
                c.Status,
                c.ErrorMessage,
                MatchScore = _context.CandidateProfiles
                    .Where(p => p.CandidateId == c.Id)
                    .Select(p => p.MatchScore)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<CandidateProfile?> GetProfileAsync(int id)
    {
        return await _context.CandidateProfiles.FindAsync(id);
    }

    public async Task<Candidate> UploadCandidateAsync(int jobId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file uploaded.");

        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null)
            throw new InvalidOperationException($"Job with ID {jobId} not found.");

        try
        {
            // Calculate file hash for caching
            string fileHash;
            using (var stream = file.OpenReadStream())
            {
                fileHash = await FileHashHelper.ComputeFileHashAsync(stream);
            }

            _logger.LogInformation("Uploading candidate for job {JobId} with hash {FileHash}", jobId, fileHash);

            // Save file to disk immediately
            var uploadsDir = "uploads";
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var filePath = Path.Combine(uploadsDir, file.FileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Create candidate record with Pending status
            var candidate = new Candidate
            {
                JobId = jobId,
                FileName = file.FileName,
                UploadedAt = DateTime.UtcNow,
                FileHash = fileHash,
                Status = ProcessingStatus.Pending,
                Name = "Processing..."
            };

            _context.Candidates.Add(candidate);
            await _context.SaveChangesAsync();

            // Queue background job for processing
            var jobId = _backgroundJobs.Enqueue<ICvProcessingJob>(
                job => job.ProcessCandidateAsync(candidate.Id));

            _logger.LogInformation("Queued candidate {CandidateId} for processing with job ID {JobId}",
                candidate.Id, jobId);

            return candidate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading candidate for job {JobId}", jobId);
            throw;
        }
    }

    public async Task<CandidateProfile> SaveProfileAsync(CandidateProfileDTO dto)
    {
        var profile = new CandidateProfile
        {
            JobId = dto.JobId,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Location = dto.Location,
            ExperienceYears = dto.ExperienceYears,
            ProfileSummary = dto.ProfileSummary,
            MatchScore = dto.MatchScore,
            Skills = JsonSerializer.Serialize(dto.Skills),
            Strengths = JsonSerializer.Serialize(dto.Strengths),
            Weaknesses = JsonSerializer.Serialize(dto.Weaknesses),
            AnalysisSummary = dto.AnalysisSummary,
            CreatedAt = DateTime.UtcNow
        };

        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved candidate profile: {Name}", profile.Name);
        return profile;
    }

    public async Task<Stream?> GetCandidateCvStreamAsync(int id)
    {
        var profile = await _context.CandidateProfiles.FindAsync(id);
        if (profile == null || string.IsNullOrEmpty(profile.FileName))
            return null;

        return await _fileStorage.GetFileStreamAsync(profile.FileName);
    }
}