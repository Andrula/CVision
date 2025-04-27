namespace CVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _context;

    public JobsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobWithCountDto>>> GetJobs()
    {
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

        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Job>> GetJob(int id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null) return NotFound();
        return job;
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetCandidatesForJob(int jobId)
    {
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

        return Ok(candidates);
    }


    [HttpPost]
    public async Task<ActionResult<Job>> CreateJob(Job job)
    {
        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var job = await _context.Jobs.FindAsync(id);
        if (job == null) return NotFound();
        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{jobId}/skills")]
    public IActionResult GetSkillDistribution(int jobId)
    {
        var profiles = _context.CandidateProfiles
            .Where(p => p.JobId == jobId && p.Skills != null)
            .ToList();

        var allSkills = profiles
            .SelectMany(p => JsonSerializer.Deserialize<List<string>>(p.Skills))
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .ToList();

        var grouped = allSkills
            .GroupBy(s => s)
            .Select(g => new { Skill = g.Key, Count = g.Count() })
            .ToList();

        return Ok(grouped);
    }

}
