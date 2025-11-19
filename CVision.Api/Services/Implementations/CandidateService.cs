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

    public async Task<IEnumerable<object>> GetCandidatesForJobAsync(int jobId, int companyId)
    {
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
        return await _context.Candidates
            .Include(c => c.Job)
            .Where(c => c.JobId == jobId && c.Job!.CompanyId == companyId)
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
    }

    public async Task<CandidateProfile?> GetProfileAsync(int id, int companyId)
    {
        return await _context.CandidateProfiles
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id && p.Job!.CompanyId == companyId);
    }

    public async Task<Candidate> UploadCandidateAsync(int jobId, int companyId, IFormFile file, string userId)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file uploaded.");

        var job = await _context.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == companyId);
        if (job == null)
            throw new InvalidOperationException($"Job with ID {jobId} not found or access denied.");

        var candidate = new Candidate
        {
            JobId = jobId,
            FileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            UploadedBy = userId,
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
                    CreatedBy = userId,
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

    public async Task<CandidateProfile> SaveProfileAsync(CandidateProfileDTO dto, int companyId, string userId)
    {
        // Verify job belongs to company
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
            CreatedBy = userId
        };

        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved candidate profile: {Name}", profile.Name);
        return profile;
    }

    public async Task<Stream?> GetCandidateCvStreamAsync(int id, int companyId)
    {
        var profile = await _context.CandidateProfiles
            .Include(p => p.Job)
            .FirstOrDefaultAsync(p => p.Id == id && p.Job!.CompanyId == companyId);
        if (profile == null || string.IsNullOrEmpty(profile.FileName))
            return null;

        return await _fileStorage.GetFileStreamAsync(profile.FileName);
    }
}