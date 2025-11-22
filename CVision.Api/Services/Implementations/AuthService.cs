using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CVision.Api.Configuration;
using CVision.Api.Data.DTO;
using CVision.Api.Data.Models;
using CVision.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CVision.Api.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var user = new ApplicationUser
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FullName = registerDto.FullName
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        return await GenerateTokenResponse(user);
    }

    public async Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        return await GenerateTokenResponse(user);
    }

    private async Task<AuthResponseDTO> GenerateTokenResponse(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim(ClaimTypes.Name, user.FullName ?? user.Email!)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponseDTO
        {
            Token = tokenString,
            Email = user.Email!,
            FullName = user.FullName ?? user.Email!,
            ExpiresAt = expiresAt
        };
    }
}
