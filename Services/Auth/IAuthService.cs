using artsy.backend.Dtos.Auth;

namespace artsy.backend.Services.Auth;

public interface IAuthService
{
	Task<Models.User?> RegisterAsync(RegisterDto registerDto);
	Task<TokenResponseDto?> LoginAsync(LoginDto loginDto);
}
