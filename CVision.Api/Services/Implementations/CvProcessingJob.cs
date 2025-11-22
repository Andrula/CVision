using CVision.Api.Services.Interfaces;

namespace CVision.Api.Services.Implementations;

public class CvProcessingJob : ICvProcessingJob
{
    private readonly ILogger<CvProcessingJob> _logger;

    public CvProcessingJob(ILogger<CvProcessingJob> logger)
    {
        _logger = logger;
    }

    public async Task ProcessCvAsync(int candidateId)
    {
        _logger.LogInformation("Processing CV for candidate {CandidateId}", candidateId);

        // TODO: Implement CV processing logic
        await Task.CompletedTask;

        _logger.LogInformation("Completed processing CV for candidate {CandidateId}", candidateId);
    }
}
