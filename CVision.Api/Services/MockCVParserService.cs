public class MockCvParserService : ICvParserService
{
    public async Task<string> ExtractTextAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var text = await reader.ReadToEndAsync();
        return text;
    }

    public async Task<Candidate> ParseCandidateAsync(string rawText, int jobId)
    {
        // TODO: Add extraction through LLM 
        return new Candidate
        {
            JobId = jobId,
            FileName = $"ParsedCandidate_{Guid.NewGuid()}",
            UploadedAt = DateTime.UtcNow,
            Name = "Unknown Candidate"
        };
    }
}
