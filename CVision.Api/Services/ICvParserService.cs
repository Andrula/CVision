public interface ICvParserService
{
    Task<string> ExtractTextAsync(IFormFile file);
    Task<Candidate> ParseCandidateAsync(string rawText, int jobId);
}
