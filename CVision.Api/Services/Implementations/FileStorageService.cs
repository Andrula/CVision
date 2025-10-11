namespace CVision.Api.Services.Implementations;

public class FileStorageService : IFileStorageService
{
    private readonly FileStorageSettings _settings;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(
        IOptions<FileStorageSettings> settings,
        ILogger<FileStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        Directory.CreateDirectory(_settings.UploadPath);
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        var originalFileName = Path.GetFileName(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
        var filePath = Path.Combine(_settings.UploadPath, uniqueFileName);

        using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        _logger.LogInformation("Saved file {FileName} to {FilePath}", uniqueFileName, filePath);
        return uniqueFileName;
    }

    public Task<Stream?> GetFileStreamAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return Task.FromResult<Stream?>(null);

        var filePath = Path.Combine(_settings.UploadPath, fileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(filePath));
    }

    public Task DeleteFileAsync(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return Task.CompletedTask;

        var filePath = Path.Combine(_settings.UploadPath, fileName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Deleted file: {FileName}", fileName);
        }

        return Task.CompletedTask;
    }

    public bool FileExists(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var filePath = Path.Combine(_settings.UploadPath, fileName);
        return File.Exists(filePath);
    }
}