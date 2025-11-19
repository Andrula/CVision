using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CVision.Api.Configuration;
using CVision.Api.Data;
using CVision.Api.Data.DTO;
using CVision.Api.Data.Models;
using CVision.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CVision.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext context,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Create a transaction for atomic operation
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create company
            var company = new Company
            {
                Name = registerDto.CompanyName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Create license (Free tier)
            var license = new License
            {
                CompanyId = company.Id,
                Type = LicenseType.Free,
                MaxUsers = 1,
                MaxJobPostings = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Licenses.Add(license);
            await _context.SaveChangesAsync();

            // Create user
            var user = new ApplicationUser
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                CompanyId = company.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Assign CompanyAdmin role
            await _userManager.AddToRoleAsync(user, "CompanyAdmin");

            await transaction.CommitAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user, new List<string> { "CompanyAdmin" });
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

            return new AuthResponseDTO
            {
                Token = token,
                Email = user.Email!,
                FullName = user.FullName,
                CompanyId = user.CompanyId,
                CompanyName = company.Name,
                Roles = new List<string> { "CompanyAdmin" },
                ExpiresAt = expiresAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);

        // Get company info
        var company = await _context.Companies.FindAsync(user.CompanyId);
        if (company == null || !company.IsActive)
        {
            throw new UnauthorizedAccessException("Company is not active");
        }

        // Generate JWT token
        var token = GenerateJwtToken(user, roles.ToList());
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes);

        return new AuthResponseDTO
        {
            Token = token,
            Email = user.Email!,
            FullName = user.FullName,
            CompanyId = user.CompanyId,
            CompanyName = company.Name,
            Roles = roles.ToList(),
            ExpiresAt = expiresAt
        };
    }

    private string GenerateJwtToken(ApplicationUser user, List<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new("CompanyId", user.CompanyId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles as claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
