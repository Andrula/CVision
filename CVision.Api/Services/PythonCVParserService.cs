
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

public class PythonCVParserService : ICvParserService
{
    private readonly HttpClient _httpClient;

    public PythonCVParserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CandidateProfileDTO?> ParseCandidateFromFileAsync(IFormFile file, int jobId, string jobTitle, string jobDescription)
    {
        var content = new MultipartFormDataContent();

        var cvStream = new StreamContent(file.OpenReadStream());
        cvStream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(cvStream, "cv_file", file.FileName);

        var jobDescBytes = System.Text.Encoding.UTF8.GetBytes(jobDescription);
        var jobStream = new MemoryStream(jobDescBytes);
        var jobFileContent = new StreamContent(jobStream);
        jobFileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(jobFileContent, "job_file", "job.txt");

        var response = await _httpClient.PostAsync("http://localhost:5002/parse-cv/", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Parser failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var candidate = JsonSerializer.Deserialize<CandidateProfileDTO>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (candidate == null)
            throw new Exception("Parsed response was null");

        return candidate;
    }
}

