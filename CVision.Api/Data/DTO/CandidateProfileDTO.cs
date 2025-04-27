public class CandidateProfileDTO
{
    public int JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ProfileSummary { get; set; } = string.Empty;
    public int MatchScore { get; set; }

    public List<string> Skills { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    
    public string AnalysisSummary { get; set; } = string.Empty;
}
