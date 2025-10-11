namespace CVision.Api.Services.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file);
    Task<Stream?> GetFileStreamAsync(string fileName);
    Task DeleteFileAsync(string fileName);
    bool FileExists(string fileName);
}