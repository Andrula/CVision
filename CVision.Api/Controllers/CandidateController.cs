using CVision.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateService _candidateService;

    public CandidatesController(ICandidateService candidateService)
    {
        _candidateService = candidateService;
    }

    [HttpGet("/api/jobs/{jobId}/candidates")]
    public async Task<IActionResult> GetCandidates(int jobId)
    {
        var candidates = await _candidateService.GetCandidatesForJobAsync(jobId);
        return Ok(candidates);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] int jobId, [FromForm] IFormFile file)
    {
        try
        {
            var candidate = await _candidateService.UploadCandidateAsync(jobId, file);
            return Ok(candidate);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Upload failed", details = ex.Message });
        }
    }

    [HttpGet("profile/{id}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var profile = await _candidateService.GetProfileAsync(id);
        if (profile == null) 
            return NotFound(new { error = "Profile not found" });
        
        return Ok(profile);
    }

    [HttpPost("profile")]
    public async Task<IActionResult> SaveProfile([FromBody] CandidateProfileDTO dto)
    {
        try
        {
            var profile = await _candidateService.SaveProfileAsync(dto);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to save profile", details = ex.Message });
        }
    }

    [HttpGet("/api/candidates/profile/{id}/cv")]
    public async Task<IActionResult> GetCandidateCV(int id)
    {
        var stream = await _candidateService.GetCandidateCvStreamAsync(id);
        if (stream == null) 
            return NotFound(new { error = "CV file not found" });

        return File(stream, "application/pdf", enableRangeProcessing: true);
    }
}