
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
        content.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
        content.Add(new StringContent(jobTitle), "jobTitle");
        content.Add(new StringContent(jobDescription), "jobDescription");
        content.Add(new StringContent(jobId.ToString()), "jobId");

        var response = await _httpClient.PostAsync("http://localhost:5002/parse", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Parser failed: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var wrapper = JsonSerializer.Deserialize<Dictionary<string, CandidateProfileDTO>>(json);
        return wrapper?["saved"];
    }
}
