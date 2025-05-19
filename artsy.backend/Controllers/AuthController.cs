using artsy.backend.Dtos.Auth;
using artsy.backend.Exceptions;
using artsy.backend.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace artsy.backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;
	private readonly IConfiguration _configuration;

	public AuthController(IAuthService authService, IConfiguration configuration)
	{
		_authService = authService;
		_configuration = configuration;
	}
	
	[HttpPost("register")]
public async Task<IActionResult> Register(RegisterDto registerDto)
{
    if (!ModelState.IsValid) return BadRequest(ModelState);

    var user = await _authService.RegisterAsync(registerDto);
    return Ok(new { message = "User registered. Try logging in.", userId = user.Id });
}
	
	[HttpPost("login")]
	public async Task<IActionResult> Login(LoginDto loginDto)
	{
		if (!ModelState.IsValid) return BadRequest(ModelState);

		// No null check needed here
		var tokenResponse = await _authService.LoginAsync(loginDto);
		SetTokenCookies(tokenResponse);
		return Ok(new { message = "Login successful.", userId = tokenResponse.UserId, username = tokenResponse.Username });
	}

	[HttpPost("refresh")]
	public async Task<IActionResult> Refresh()
	{
		string? receivedAccessToken = Request.Cookies["x-access-token"];
		string? receivedRefreshToken = Request.Cookies["x-refresh-token"];

		if (string.IsNullOrEmpty(receivedAccessToken) || string.IsNullOrEmpty(receivedRefreshToken))
		{ 
			throw new BadRequestException("Tokens are required for refresh.");
		}

		var refreshTokenServiceRequest = new RefreshTokenRequestDto
		{
			ExpiredAccessToken = receivedAccessToken,
			RefreshToken = receivedRefreshToken
		};

		var tokenResponse = await _authService.RefreshTokenAsync(refreshTokenServiceRequest);
		SetTokenCookies(tokenResponse);
		return Ok(new { message = "Tokens refreshed successfully.", userId = tokenResponse.UserId, username = tokenResponse.Username });
	}

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        string? refreshTokenValue = Request.Cookies["x-refresh-token"];

        await _authService.LogoutAsync(User, refreshTokenValue);

        ClearTokenCookies();

        return Ok(new { message = "Logout successful." });
    }

    private void SetTokenCookies(TokenResponseDto tokenResponse)
    {
        var accessTokenCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = tokenResponse.AccessTokenExpiration,
            Secure = _configuration.GetValue<bool>("CookieSettings:Secure", true),
            SameSite = Enum.Parse<SameSiteMode>(_configuration.GetValue<string>("CookieSettings:SameSiteAccessToken", "Strict")!),
            Path = _configuration.GetValue<string>("CookieSettings:PathAccessToken", "/api")
        };
        Response.Cookies.Append("x-access-token", tokenResponse.AccessToken, accessTokenCookieOptions);

        var refreshTokenCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenTTLDays", 7)),
            Secure = _configuration.GetValue<bool>("CookieSettings:Secure", true),
            SameSite = Enum.Parse<SameSiteMode>(_configuration.GetValue<string>("CookieSettings:SameSiteRefreshToken", "Strict")!),
            Path = _configuration.GetValue<string>("CookieSettings:PathRefreshToken", "/api/auth/refresh")
        };
        Response.Cookies.Append("x-refresh-token", tokenResponse.RefreshToken, refreshTokenCookieOptions);
    }

    private void ClearTokenCookies()
    {
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var accessTokenCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = pastDate,
            Secure = _configuration.GetValue<bool>("CookieSettings:Secure", true),
            SameSite = Enum.Parse<SameSiteMode>(_configuration.GetValue<string>("CookieSettings:SameSiteAccessToken", "Lax")!),
            Path = _configuration.GetValue<string>("CookieSettings:PathAccessToken", "/api")
        };
        Response.Cookies.Append("x-access-token", "", accessTokenCookieOptions);

        var refreshTokenCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Expires = pastDate,
            Secure = _configuration.GetValue<bool>("CookieSettings:Secure", true),
            SameSite = Enum.Parse<SameSiteMode>(_configuration.GetValue<string>("CookieSettings:SameSiteRefreshToken", "Strict")!),
            Path = _configuration.GetValue<string>("CookieSettings:PathRefreshToken", "/api/auth/refresh")
        };
        Response.Cookies.Append("x-refresh-token", "", refreshTokenCookieOptions);
    }
}
