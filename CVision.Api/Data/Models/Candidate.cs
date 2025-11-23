namespace CVision.Api.Data.Models;

public class Candidate
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string FileName { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ParsedAt { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public string? FileHash { get; set; }
    public string? ErrorMessage { get; set; }
    public string Language { get; set; } = "en";

    public int JobId { get; set; }
    public Job Job { get; set; }
}