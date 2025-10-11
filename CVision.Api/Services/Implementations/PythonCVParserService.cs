using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using CVision.Api.Configuration;

namespace CVision.Api.Services.Implementations;

public class PythonCVParserService : IPythonCvParserService
{
    private readonly HttpClient _httpClient;
    private readonly CvParserSettings _settings;

    public PythonCVParserService(
        HttpClient httpClient, 
        IOptions<CvParserSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
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
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Parser service failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var candidate = JsonSerializer.Deserialize<CandidateProfileDTO>(json, 
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (candidate == null)
            throw new Exception("Parsed response was null");

        return candidate;
    }
}