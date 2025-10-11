namespace CVision.Api.Configuration;

public class FileStorageSettings
{
    public string UploadPath { get; set; } = string.Empty;
    public int MaxFileSizeInMB { get; set; } = 10;
    public List<string> AllowedExtensions { get; set; } = new();
}