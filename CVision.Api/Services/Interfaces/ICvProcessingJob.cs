namespace CVision.Api.Services.Interfaces;

public interface ICvProcessingJob
{
    Task ProcessCvAsync(int candidateId);
}
