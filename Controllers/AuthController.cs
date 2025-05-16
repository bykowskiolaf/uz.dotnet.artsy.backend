using artsy.backend.Dtos.Auth;
using Artsy.Backend.Dtos.Auth;
using artsy.backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Artsy.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;

	public AuthController(IAuthService authService)
	{
		_authService = authService;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register(RegisterDto registerDto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var user = await _authService.RegisterAsync(registerDto);

		if (user == null)
		{
			return Ok(new { message = "Registration failed. Username or email might be taken." });
		}

		return Ok(new { message = "User registered. Try logging in.", userId = user.Id });
	}
	
	[HttpPost("login")]
	public async Task<IActionResult> Login(LoginDto loginDto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var tokenResponse = await _authService.LoginAsync(loginDto);

		if (tokenResponse == null)
		{
			return Unauthorized(new { message = "Invalid credentials." });
		}

		var cookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Expires = tokenResponse.Expiration,
			Secure = true,
			SameSite = SameSiteMode.Strict,
		};
		
		Response.Cookies.Append("", tokenResponse.Token, cookieOptions);
		return Ok(tokenResponse);
	}
}
