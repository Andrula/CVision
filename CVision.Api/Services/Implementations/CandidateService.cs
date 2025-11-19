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

    public async Task<IEnumerable<object>> GetCandidatesForJobAsync(int jobId, int companyId)
    {
        _logger.LogDebug("Fetching candidates for job {JobId}", jobId);
        

        return await _context.CandidateProfiles
            .Include(p => p.Job)
            .Where(p => p.JobId == jobId && p.Job!.CompanyId == companyId)
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

    public async Task<IEnumerable<object>> GetCandidatesWithMatchScoreAsync(int jobId, int companyId)
    {
        _logger.LogDebug("Fetching candidates with match scores for job {JobId}", jobId);

        return await _context.Candidates
            .Include(c => c.Job)
            .Where(c => c.JobId == jobId && c.Job!.CompanyId == companyId)

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

    public async Task<CandidateProfile?> GetProfileAsync(int id, int companyId)
    {
        _logger.LogDebug("Fetching profile {ProfileId}", id);
     

        return await _context.CandidateProfiles
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id && p.Job!.CompanyId == companyId);
    }

    public async Task<Candidate> UploadCandidateAsync(int jobId, int companyId, IFormFile file, string userId)
    {
        _logger.LogInformation("Starting CV upload for job {JobId}, file: {FileName}, size: {FileSize}KB", 
            jobId, file.FileName, file.Length / 1024);
        
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Upload attempted with empty file for job {JobId}", jobId);
            throw new ArgumentException("No file uploaded.");
        }

        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == companyId);
        if (job == null)
            throw new InvalidOperationException($"Job with ID {jobId} not found or access denied.");

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
            else
            {
                _logger.LogWarning("Parser returned null for candidate {CandidateId}", candidate.Id);
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
            _logger.LogError(ex, "Failed to upload candidate for job {JobId}, file: {FileName}", 
                jobId, file.FileName);
            throw;
        }
    }

    public async Task<CandidateProfile> SaveProfileAsync(CandidateProfileDTO dto, int companyId, string userId)
    {
        _logger.LogInformation("Saving candidate profile for job {JobId}: {Name}", dto.JobId, dto.Name);
        
        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == dto.JobId && j.CompanyId == companyId);
        if (job == null)
            throw new InvalidOperationException($"Job with ID {dto.JobId} not found or access denied.");

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
            CreatedAt = DateTime.UtcNow,
            ParsedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved candidate profile {ProfileId}: {Name}", profile.Id, profile.Name);
        return profile;
    }

    public async Task<Stream?> GetCandidateCvStreamAsync(int id, int companyId)
    {
        _logger.LogInformation("Retrieving CV file for profile {ProfileId}", id);

        var profile = await _context.CandidateProfiles
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id && p.Job!.CompanyId == companyId);

        if (profile == null || string.IsNullOrEmpty(profile.FileName))
        {
            _logger.LogWarning("CV file not found for profile {ProfileId}", id);
            return null;
        }

        return await _fileStorage.GetFileStreamAsync(profile.FileName);
    }

    public async Task<CandidateProfile> ReparseProfileAsync(int profileId)
    {
        _logger.LogInformation("Starting re-parse for profile {ProfileId}", profileId);
        
        var profile = await _context.CandidateProfiles
            .Include(p => p.Candidate)
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == profileId);
        
        if (profile == null)
        {
            _logger.LogWarning("Re-parse attempted for non-existent profile {ProfileId}", profileId);
            throw new InvalidOperationException($"Profile {profileId} not found");
        }

        if (profile.Job == null)
        {
            _logger.LogWarning("Re-parse attempted for profile {ProfileId} with missing job", profileId);
            throw new InvalidOperationException("Job information not found");
        }

        if (!_fileStorage.FileExists(profile.FileName))
        {
            _logger.LogWarning("Re-parse attempted for profile {ProfileId} but CV file not found", profileId);
            throw new InvalidOperationException("Original CV file not found");
        }

        var fileStream = await _fileStorage.GetFileStreamAsync(profile.FileName);
        if (fileStream == null)
        {
            throw new InvalidOperationException("Could not open CV file");
        }

        try
        {
            _logger.LogInformation("Re-parsing CV for profile {ProfileId}", profileId);
            var startTime = DateTime.UtcNow;

            using (fileStream)
            {
                var formFile = new FormFile(fileStream, 0, fileStream.Length, "file", profile.FileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/pdf"
                };

                var parsed = await _parser.ParseCandidateFromFileAsync(
                    formFile, 
                    profile.JobId, 
                    profile.Job.Title, 
                    profile.Job.Description);

                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                _logger.LogInformation("Re-parse completed in {Duration}s for profile {ProfileId}", duration, profileId);

                if (parsed != null)
                {
                    _logger.LogInformation("Re-parsed candidate: {Name}, Match: {MatchScore}%, Experience: {Years} years",
                        parsed.Name, parsed.MatchScore, parsed.ExperienceYears);

                    profile.Name = parsed.Name;
                    profile.Email = parsed.Email;
                    profile.Phone = parsed.Phone;
                    profile.Location = parsed.Location;
                    profile.ExperienceYears = parsed.ExperienceYears;
                    profile.ProfileSummary = parsed.ProfileSummary;
                    profile.MatchScore = parsed.MatchScore;
                    profile.Skills = JsonSerializer.Serialize(parsed.Skills);
                    profile.Strengths = JsonSerializer.Serialize(parsed.Strengths);
                    profile.Weaknesses = JsonSerializer.Serialize(parsed.Weaknesses);
                    profile.AnalysisSummary = parsed.AnalysisSummary;
                    profile.ParsedAt = DateTime.UtcNow;

                    _context.CandidateProfiles.Update(profile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Successfully re-parsed and updated profile {ProfileId}", profileId);
                }
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-parse profile {ProfileId}", profileId);
            throw;
        }
    }
}