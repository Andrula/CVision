
public class CandidateProfile
{
    public int Id { get; set; }
    public int JobId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

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

    public int CandidateId { get; set; }
    public Candidate Candidate { get; set; }
    public Job? Job { get; set; }
}
