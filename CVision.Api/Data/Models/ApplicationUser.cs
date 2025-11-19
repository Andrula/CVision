using Microsoft.AspNetCore.Identity;

namespace CVision.Api.Data.Models;

public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }

    public int CompanyId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public Company? Company { get; set; }
}
