namespace CVision.Api.Services.Implementations;

public class JobService : IJobService
{
    private readonly AppDbContext _context;
    private readonly ILogger<JobService> _logger;

    public JobService(AppDbContext context, ILogger<JobService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<JobWithCountDto>> GetAllJobsAsync()
    {
        _logger.LogDebug("Fetching all jobs");
        
        var jobs = await _context.Jobs
            .Select(j => new JobWithCountDto
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                CreatedAt = j.CreatedAt,
                ApplicantCount = _context.Candidates.Count(c => c.JobId == j.Id)
            })
            .ToListAsync();
        
        _logger.LogInformation("Retrieved {Count} jobs", jobs.Count());
        
        return jobs;
    }

    public async Task<Job?> GetJobByIdAsync(int id)
    {
        _logger.LogDebug("Fetching job {JobId}", id);
        
        var job = await _context.Jobs.FindAsync(id);
        
        if (job == null)
            _logger.LogWarning("Job {JobId} not found", id);
        
        return job;
    }

    public async Task<Job> CreateJobAsync(Job job)
    {
        _logger.LogInformation("Creating new job: {JobTitle}", job.Title);
        
        if (string.IsNullOrWhiteSpace(job.Title))
        {
            _logger.LogWarning("Job creation attempted with empty title");
            throw new ArgumentException("Job title is required");
        }

        if (string.IsNullOrWhiteSpace(job.Description))
        {
            _logger.LogWarning("Job creation attempted with empty description");
            throw new ArgumentException("Job description is required");
        }

        job.CreatedAt = DateTime.UtcNow;
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created job: {JobTitle} (ID: {JobId})", job.Title, job.Id);
        return job;
    }

    public async Task<bool> DeleteJobAsync(int id)
    {
        _logger.LogInformation("Attempting to delete job {JobId}", id);
        
        var job = await _context.Jobs.FindAsync(id);
        if (job == null)
        {
            _logger.LogWarning("Delete attempted for non-existent job {JobId}", id);
            return false;
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted job {JobId}: {JobTitle}", id, job.Title);
        return true;
    }

    public async Task<IEnumerable<object>> GetSkillDistributionAsync(int jobId)
    {
        _logger.LogDebug("Fetching skill distribution for job {JobId}", jobId);
        
        var profiles = await _context.CandidateProfiles
            .Where(p => p.JobId == jobId && p.Skills != null)
            .ToListAsync();

        var allSkills = profiles
            .SelectMany(p => JsonSerializer.Deserialize<List<string>>(p.Skills) ?? new List<string>())
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .ToList();

        var grouped = allSkills
            .GroupBy(s => s)
            .Select(g => new { Skill = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        _logger.LogInformation("Retrieved skill distribution for job {JobId}: {SkillCount} unique skills from {CandidateCount} candidates", 
            jobId, grouped.Count, profiles.Count);

        return grouped;
    }
}