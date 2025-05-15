using artsy.backend.Dtos.Auth;
using artsy.backend.Models;

namespace artsy.backend.Services;

public interface IAuthService
{
	Task<User?> RegisterAsync(RegisterDto registerDto);
}
