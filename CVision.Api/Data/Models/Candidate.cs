public class Candidate
{
    public int Id { get; set; }
    public string? Name { get; set; } 
    public string FileName {get; set;}
    public DateTime UploadedAt { get; set; }

    public int JobId { get; set; }
    public Job Job { get; set; }
}