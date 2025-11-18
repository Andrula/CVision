namespace CVision.Api.Data.Models;

public enum LicenseType
{
    Free,
    Basic,
    Premium,
    Enterprise
}

public class License
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public LicenseType Type { get; set; } = LicenseType.Free;

    public int MaxUsers { get; set; } = 1;

    public int MaxJobPostings { get; set; } = 5;

    public bool IsActive { get; set; } = true;

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public Company? Company { get; set; }
}
