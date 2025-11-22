using Microsoft.AspNetCore.Identity;

namespace CVision.Api.Data.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
