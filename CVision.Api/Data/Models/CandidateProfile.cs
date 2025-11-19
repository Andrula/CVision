using System.ComponentModel.DataAnnotations.Schema;

public class CandidateProfile
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public int CandidateId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
<<<<<<< HEAD
    public string? FileHash { get; set; }
=======
>>>>>>> origin/feature/identity-authentication
    public int ExperienceYears { get; set; }

    public string ProfileSummary { get; set; } = string.Empty;
    public int MatchScore { get; set; }

    [Column(TypeName = "jsonb")]
    public string Skills { get; set; }

    [Column(TypeName = "jsonb")]
    public string Strengths { get; set; }

    [Column(TypeName = "jsonb")]
    public string Weaknesses { get; set; }

    public string AnalysisSummary { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
<<<<<<< HEAD
    public DateTime ParsedAt { get; set; }
=======
    public DateTime? UpdatedAt { get; set; }
>>>>>>> origin/feature/identity-authentication

    // Audit fields
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public Candidate? Candidate { get; set; }
    public Job? Job { get; set; }
}
