using System.Security.Claims;
using CVision.Api.Data.Models;
using CVision.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ICandidateService _candidateService;

    public JobsController(IJobService jobService, ICandidateService candidateService)
    {
        _jobService = jobService;
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

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        var companyId = GetCompanyId();
        var jobs = await _jobService.GetAllJobsAsync(companyId);
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(int id)
    {
        var companyId = GetCompanyId();
        var job = await _jobService.GetJobByIdAsync(id, companyId);
        if (job == null)
            return NotFound(new { error = "Job not found" });

        return Ok(job);
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetCandidatesForJob(int jobId)
    {
        var companyId = GetCompanyId();
        var candidates = await _candidateService.GetCandidatesWithMatchScoreAsync(jobId, companyId);
        return Ok(candidates);
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] Job job)
    {
        try
        {
            // Set company context and audit fields
            job.CompanyId = GetCompanyId();
            job.CreatedBy = GetUserId();

            var createdJob = await _jobService.CreateJobAsync(job);
            return CreatedAtAction(nameof(GetJob), new { id = createdJob.Id }, createdJob);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to create job", details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var companyId = GetCompanyId();
        var deleted = await _jobService.DeleteJobAsync(id, companyId);
        if (!deleted)
            return NotFound(new { error = "Job not found" });

        return NoContent();
    }

    [HttpGet("{jobId}/skills")]
    public async Task<IActionResult> GetSkillDistribution(int jobId)
    {
        var companyId = GetCompanyId();
        var skills = await _jobService.GetSkillDistributionAsync(jobId, companyId);
        return Ok(skills);
    }
}
