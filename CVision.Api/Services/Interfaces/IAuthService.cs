using CVision.Api.Data.DTO;

namespace CVision.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDTO> RegisterAsync(RegisterDTO registerDto);
    Task<AuthResponseDTO> LoginAsync(LoginDTO loginDto);
}
