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

    public async Task<IEnumerable<CandidateBasicDto>> GetCandidatesForJobAsync(int jobId)
    {
        return await _context.CandidateProfiles
            .Where(p => p.JobId == jobId)
            .Select(p => new CandidateBasicDto
            {
                Id = p.Id,
                JobId = p.JobId,
                Name = p.Name,
                MatchScore = p.MatchScore,
                ExperienceYears = p.ExperienceYears
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<CandidateWithMatchScoreDto>> GetCandidatesWithMatchScoreAsync(int jobId)
    {
        return await _context.Candidates
            .Where(c => c.JobId == jobId)
            .Select(c => new CandidateWithMatchScoreDto
            {
                Id = c.Id,
                JobId = c.JobId,
                Name = c.Name,
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
            var parsed = await _parser.ParseCandidateFromFileAsync(
                file, jobId, job.Title, job.Description);

            var uniqueFileName = await _fileStorage.SaveFileAsync(file);

            if (parsed != null)
            {
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

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully uploaded and parsed candidate for job {JobId}", jobId);

            return candidate;
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? "No inner exception";
            _logger.LogError(ex, "Database error while saving candidate: {Error}", inner);
            throw new Exception($"Database save error: {inner}", ex);
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