using System.Security.Claims;
using CVision.Api.Data.DTO;
using CVision.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateService _candidateService;

    public CandidatesController(ICandidateService candidateService)
    {
        _candidateService = candidateService;
    }

    private int GetCompanyId()
    {
        var companyIdClaim = User.FindFirst("CompanyId")?.Value;
        if (string.IsNullOrEmpty(companyIdClaim) || !int.TryParse(companyIdClaim, out var companyId))
        {
            throw new UnauthorizedAccessException("Invalid company context");
        }
        return companyId;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User not authenticated");
    }

    [HttpGet("/api/jobs/{jobId}/candidates")]
    public async Task<IActionResult> GetCandidates(int jobId)
    {
        var companyId = GetCompanyId();
        var candidates = await _candidateService.GetCandidatesForJobAsync(jobId, companyId);
        return Ok(candidates);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] int jobId, [FromForm] IFormFile file)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            var candidate = await _candidateService.UploadCandidateAsync(jobId, companyId, file, userId);
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Upload failed", details = ex.Message });
        }
    }

    [HttpGet("profile/{id}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var companyId = GetCompanyId();
        var profile = await _candidateService.GetProfileAsync(id, companyId);
        if (profile == null)
            return NotFound(new { error = "Profile not found" });

        return Ok(profile);
    }

    [HttpPost("profile")]
    public async Task<IActionResult> SaveProfile([FromBody] CandidateProfileDTO dto)
    {
        try
        {
            var companyId = GetCompanyId();
            var userId = GetUserId();
            var profile = await _candidateService.SaveProfileAsync(dto, companyId, userId);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to save profile", details = ex.Message });
        }
    }

    [HttpGet("/api/candidates/profile/{id}/cv")]
    public async Task<IActionResult> GetCandidateCV(int id)
    {
        var companyId = GetCompanyId();
        var stream = await _candidateService.GetCandidateCvStreamAsync(id, companyId);
        if (stream == null)
            return NotFound(new { error = "CV file not found" });

        return File(stream, "application/pdf", enableRangeProcessing: true);
    }
}