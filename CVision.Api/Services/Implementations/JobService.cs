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
        return await _context.Jobs
            .Select(j => new JobWithCountDto
            {
                Id = j.Id,
                Title = j.Title,
                Description = j.Description,
                CreatedAt = j.CreatedAt,
                ApplicantCount = _context.Candidates.Count(c => c.JobId == j.Id)
            })
            .ToListAsync();
    }

    public async Task<Job?> GetJobByIdAsync(int id)
    {
        return await _context.Jobs.FindAsync(id);
    }

    public async Task<Job> CreateJobAsync(Job job)
    {
        if (string.IsNullOrWhiteSpace(job.Title))
            throw new ArgumentException("Job title is required");

        if (string.IsNullOrWhiteSpace(job.Description))
            throw new ArgumentException("Job description is required");

        job.CreatedAt = DateTime.UtcNow;
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created job: {JobTitle} (ID: {JobId})", job.Title, job.Id);
        return job;
    }

    public async Task<bool> DeleteJobAsync(int id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null)
            return false;

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted job: {JobId}", id);
        return true;
    }

    public async Task<IEnumerable<object>> GetSkillDistributionAsync(int jobId)
    {
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

        return grouped;
    }
}