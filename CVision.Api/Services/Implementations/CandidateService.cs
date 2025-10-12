namespace CVision.Api.Services.Implementations;

public class CandidateService : ICandidateService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly IPythonCvParserService _parser;
    private readonly ILogger<CandidateService> _logger;

    public CandidateService(
        AppDbContext context,
        IFileStorageService fileStorage,
        IPythonCvParserService parser,
        ILogger<CandidateService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _parser = parser;
        _logger = logger;
    }

    public async Task<IEnumerable<object>> GetCandidatesForJobAsync(int jobId)
    {
        _logger.LogDebug("Fetching candidates for job {JobId}", jobId);
        
        var candidates = await _context.CandidateProfiles
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
        
        _logger.LogInformation("Retrieved {Count} candidates for job {JobId}", candidates.Count(), jobId);
        
        return candidates;
    }

    public async Task<IEnumerable<object>> GetCandidatesWithMatchScoreAsync(int jobId)
    {
        _logger.LogDebug("Fetching candidates with match scores for job {JobId}", jobId);
        
        var candidates = await _context.Candidates
            .Where(c => c.JobId == jobId)
            .Select(c => new
            {
                c.Id,
                c.JobId,
                c.Name,
                MatchScore = _context.CandidateProfiles
                    .Where(p => p.CandidateId == c.Id)
                    .Select(p => p.MatchScore)
                    .FirstOrDefault()
            })
            .ToListAsync();
        
        _logger.LogInformation("Retrieved {Count} candidates with match scores for job {JobId}", candidates.Count(), jobId);
        
        return candidates;
    }

    public async Task<CandidateProfile?> GetProfileAsync(int id)
    {
        _logger.LogDebug("Fetching profile {ProfileId}", id);
        
        var profile = await _context.CandidateProfiles.FindAsync(id);
        
        if (profile == null)
            _logger.LogWarning("Profile {ProfileId} not found", id);
        
        return profile;
    }

    public async Task<Candidate> UploadCandidateAsync(int jobId, IFormFile file)
    {
        _logger.LogInformation("Starting CV upload for job {JobId}, file: {FileName}, size: {FileSize}KB", 
            jobId, file.FileName, file.Length / 1024);
        
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Upload attempted with empty file for job {JobId}", jobId);
            throw new ArgumentException("No file uploaded.");
        }

        var job = await _context.Jobs.FindAsync(jobId);

        if (job == null)
        {
            _logger.LogWarning("Upload attempted for non-existent job {JobId}", jobId);
            throw new InvalidOperationException($"Job with ID {jobId} not found.");
        }

        var candidate = new Candidate
        {
            JobId = jobId,
            FileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            Name = "Parsing..."
        };

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        try
        {
            _logger.LogInformation("Calling parser service for candidate {CandidateId}", candidate.Id);
            var startTime = DateTime.UtcNow;
            
            var parsed = await _parser.ParseCandidateFromFileAsync(
                file, jobId, job.Title, job.Description);

            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation("Parser completed in {Duration}s for candidate {CandidateId}", 
                duration, candidate.Id);

            var uniqueFileName = await _fileStorage.SaveFileAsync(file);

            if (parsed != null)
            {
                _logger.LogInformation("Parsed candidate: {Name}, Match: {MatchScore}%, Experience: {Years} years",
                    parsed.Name, parsed.MatchScore, parsed.ExperienceYears);
                
                var profile = new CandidateProfile
                {
                    JobId = jobId,
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

                _context.CandidateProfiles.Add(profile);

                candidate.Name = parsed.Name;
                _context.Candidates.Update(candidate);
            }
            else
            {
                _logger.LogWarning("Parser returned null for candidate {CandidateId}", candidate.Id);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully saved candidate {CandidateId} ({Name}) for job {JobId}", 
                candidate.Id, candidate.Name, jobId);

            return candidate;
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? "No inner exception";
            _logger.LogError(ex, "Database error saving candidate for job {JobId}: {Error}", jobId, inner);
            throw new Exception($"Database save error: {inner}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload candidate for job {JobId}, file: {FileName}", 
                jobId, file.FileName);
            throw;
        }
    }

    public async Task<CandidateProfile> SaveProfileAsync(CandidateProfileDTO dto)
    {
        _logger.LogInformation("Saving candidate profile for job {JobId}: {Name}", dto.JobId, dto.Name);
        
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

        _logger.LogInformation("Saved candidate profile {ProfileId}: {Name}", profile.Id, profile.Name);
        return profile;
    }

    public async Task<Stream?> GetCandidateCvStreamAsync(int id)
    {
        _logger.LogInformation("Retrieving CV file for profile {ProfileId}", id);
        
        var profile = await _context.CandidateProfiles.FindAsync(id);
        if (profile == null || string.IsNullOrEmpty(profile.FileName))
        {
            _logger.LogWarning("CV file not found for profile {ProfileId}", id);
            return null;
        }

        return await _fileStorage.GetFileStreamAsync(profile.FileName);
    }
}