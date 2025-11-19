public class Job
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Multi-tenancy
    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    // Audit fields
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    public ICollection<CandidateProfile> CandidateProfiles { get; set; } = new List<CandidateProfile>();
}
