using CVision.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ICandidateService _candidateService;

    public JobsController(IJobService jobService, ICandidateService candidateService)
    {
        _jobService = jobService;
        _candidateService = candidateService;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs()
    {
        var jobs = await _jobService.GetAllJobsAsync();
        return Ok(jobs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetJob(int id)
    {
        var job = await _jobService.GetJobByIdAsync(id);
        if (job == null) 
            return NotFound(new { error = "Job not found" });
        
        return Ok(job);
    }

    [HttpGet("job/{jobId}")]
    public async Task<IActionResult> GetCandidatesForJob(int jobId)
    {
        var candidates = await _candidateService.GetCandidatesWithMatchScoreAsync(jobId);
        return Ok(candidates);
    }

    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] Job job)
    {
        try
        {
            var createdJob = await _jobService.CreateJobAsync(job);
            return CreatedAtAction(nameof(GetJob), new { id = createdJob.Id }, createdJob);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to create job", details = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var deleted = await _jobService.DeleteJobAsync(id);
        if (!deleted) 
            return NotFound(new { error = "Job not found" });
        
        return NoContent();
    }

    [HttpGet("{jobId}/skills")]
    public async Task<IActionResult> GetSkillDistribution(int jobId)
    {
        var skills = await _jobService.GetSkillDistributionAsync(jobId);
        return Ok(skills);
    }
}