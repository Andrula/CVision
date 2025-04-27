public interface ICvParserService
{
 Task<CandidateProfileDTO?> ParseCandidateFromFileAsync(IFormFile file, int jobId, string jobTitle, string jobDescription);
}
