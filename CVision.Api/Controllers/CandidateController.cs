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
        var candidates = _context.Candidates.Where(c => c.JobId == jobId).ToList();
        return Ok(candidates);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] int jobId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var job = await _context.Jobs.FindAsync(jobId);
        if (job == null) return NotFound("Job not found.");

        var text = await _parser.ExtractTextAsync(file);
        var candidate = await _parser.ParseCandidateAsync(text, jobId);

        _context.Candidates.Add(candidate);
        await _context.SaveChangesAsync();

        return Ok(candidate);
    }
}
