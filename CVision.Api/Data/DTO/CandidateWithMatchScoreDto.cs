namespace CVision.Api.Data.DTO;

public class CandidateWithMatchScoreDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MatchScore { get; set; }
}
