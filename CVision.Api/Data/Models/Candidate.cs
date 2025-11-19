public class Candidate
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string FileName {get; set;}
    public DateTime UploadedAt { get; set; }
    public DateTime? ParsedAt { get; set; }
    public string? FileHash { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public string? ErrorMessage { get; set; }
    public string Language { get; set; } = "da"; // Language for CV parsing (da/en)

    public int JobId { get; set; }
    public Job? Job { get; set; }

    // Audit fields
    public string? UploadedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<CandidateProfile> Profiles { get; set; } = new List<CandidateProfile>();
}