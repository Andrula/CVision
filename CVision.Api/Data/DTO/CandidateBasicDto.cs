namespace CVision.Api.Data.DTO;

public class CandidateBasicDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public int ExperienceYears { get; set; }
}
