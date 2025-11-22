using CVision.Api.Data.DTO;
using CVision.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVision.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO registerDto)
    {
        try
        {
            var response = await _authService.RegisterAsync(registerDto);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed for {Email}", registerDto.Email);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for {Email}", registerDto.Email);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO loginDto)
    {
        try
        {
            var response = await _authService.LoginAsync(loginDto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed for {Email}", loginDto.Email);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Email}", loginDto.Email);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}
