namespace CVision.Api.Services.Implementations;

public class PythonCVParserService : IPythonCvParserService
{
    private readonly HttpClient _httpClient;
    private readonly CvParserSettings _settings;
    private readonly ILogger<PythonCVParserService> _logger;

    public PythonCVParserService(
        HttpClient httpClient, 
        IOptions<CvParserSettings> settings,
        ILogger<PythonCVParserService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CandidateProfileDTO?> ParseCandidateFromFileAsync(
        IFormFile file, 
        int jobId, 
        string jobTitle, 
        string jobDescription)
    {
        var content = new MultipartFormDataContent();

        var cvStream = new StreamContent(file.OpenReadStream());
        cvStream.Headers.ContentType = new MediaTypeHeaderValue(
            file.ContentType ?? "application/octet-stream");
        content.Add(cvStream, "cv_file", file.FileName);

        var jobDescBytes = System.Text.Encoding.UTF8.GetBytes(jobDescription);
        var jobStream = new MemoryStream(jobDescBytes);
        var jobFileContent = new StreamContent(jobStream);
        jobFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(jobFileContent, "job_file", "job.txt");

        var url = $"{_settings.ServiceUrl}/parse-cv/";
        
        _logger.LogInformation("Calling OpenAI parser at {Url} for job {JobId}, file: {FileName}", 
            url, jobId, file.FileName);
        var startTime = DateTime.UtcNow;
        
        var response = await _httpClient.PostAsync(url, content);

        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Parser service failed after {Duration}s: {Error}", duration, error);
            throw new Exception($"Parser service failed: {error}");
        }

        _logger.LogInformation("Parser service succeeded in {Duration}s for job {JobId}", duration, jobId);

        var json = await response.Content.ReadAsStringAsync();
        var candidate = JsonSerializer.Deserialize<CandidateProfileDTO>(json, 
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (candidate == null)
        {
            _logger.LogWarning("Parser returned null candidate for job {JobId}", jobId);
            throw new Exception("Parsed response was null");
        }

        _logger.LogInformation("Parsed candidate: {Name}, Match: {MatchScore}%, Experience: {Years} years", 
            candidate.Name, candidate.MatchScore, candidate.ExperienceYears);

        return candidate;
    }
}