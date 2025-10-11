namespace CVision.Api.Services.Interfaces;

public interface IPythonCvParserService
{
    Task<CandidateProfileDTO?> ParseCandidateFromFileAsync(IFormFile file, int jobId, string jobTitle, string jobDescription);
}
