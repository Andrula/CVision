using System.ComponentModel.DataAnnotations;

namespace CVision.Api.Data.DTO;

public class RegisterDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    public string CompanyName { get; set; } = string.Empty;
}
