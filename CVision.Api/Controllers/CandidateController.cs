namespace CVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CandidatesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ICvParserService _parser;
    public CandidatesController(AppDbContext context, IWebHostEnvironment env, ICvParserService parser)
    {
        _context = context;
        _env = env;
        _parser = parser;
    }

    [HttpGet("/api/jobs/{jobId}/candidates")]
    public async Task<ActionResult<IEnumerable<Candidate>>> GetCandidates(int jobId)
    {
        var profiles = await _context.CandidateProfiles
        .Where(p => p.JobId == jobId)
        .Select(p => new
        {
            p.Id,
            p.JobId,
            p.Name,
            p.MatchScore
        })
        .ToListAsync();

        return Ok(profiles);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] int jobId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null) return NotFound("Job not found.");

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

            var parsed = await _parser.ParseCandidateFromFileAsync(file, jobId, job.Title, job.Description);

            if (parsed != null)
            {
                var profile = new CandidateProfile
                {
                    JobId = parsed.JobId,
                    Name = parsed.Name,
                    Email = parsed.Email,
                    Phone = parsed.Phone,
                    Location = parsed.Location,
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
            return Ok(candidate);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Parsing failed", details = ex.Message });
        }
    }

    [HttpGet("profile/{id}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var profile = await _context.CandidateProfiles.FindAsync(id);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpPost("profile")]
    public async Task<IActionResult> SaveProfile([FromBody] CandidateProfileDTO dto)
    {
        var profile = new CandidateProfile
        {
            JobId = dto.JobId,
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone,
            Location = dto.Location,
            ProfileSummary = dto.ProfileSummary,
            MatchScore = dto.MatchScore,
            Skills = JsonSerializer.Serialize(dto.Skills),
            Strengths = JsonSerializer.Serialize(dto.Strengths),
            Weaknesses = JsonSerializer.Serialize(dto.Weaknesses),
            CreatedAt = DateTime.UtcNow
        };

        _context.CandidateProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return Ok(profile);
    }
}
