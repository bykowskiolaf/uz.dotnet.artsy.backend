using System.Security.Claims;
using artsy.backend.Dtos.Auth;

namespace artsy.backend.Services.Auth;

// ...
public interface IAuthService
{
	Task<Models.User> RegisterAsync(RegisterDto registerDto);
	Task<TokenResponseDto> LoginAsync(LoginDto loginDto);
	Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequestDto);
	Task<bool> LogoutAsync(ClaimsPrincipal userPrincipal, string? refreshTokenValue);
}
// ...