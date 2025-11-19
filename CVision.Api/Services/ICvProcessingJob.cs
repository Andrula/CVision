namespace CVision.Api.Services;

public interface ICvProcessingJob
{
    Task ProcessCandidateAsync(int candidateId);
}
