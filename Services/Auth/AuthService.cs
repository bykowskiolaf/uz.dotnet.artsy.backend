using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using artsy.backend.Data;
using artsy.backend.Dtos.Auth;
using artsy.backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using artsy.backend.Exceptions;

namespace artsy.backend.Services.Auth;

public class AuthService : IAuthService
{
	private readonly ApplicationDbContext _context;
	private readonly IPasswordHasher<Models.User> _passwordHasher;
	private readonly IConfiguration _configuration;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly ILogger<AuthService> _logger;

	public AuthService(
		ApplicationDbContext context,
		IPasswordHasher<Models.User> passwordHasher,
		IConfiguration configuration,
		IHttpContextAccessor httpContextAccessor,
		ILogger<AuthService> logger)
	{
		_context = context;
		_passwordHasher = passwordHasher;
		_configuration = configuration;
		_httpContextAccessor = httpContextAccessor;
		_logger = logger;
	}
	    public async Task<Models.User> RegisterAsync(RegisterDto registerDto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
        {
            throw new ConflictException($"Username '{registerDto.Username}' is already taken.");
        }
        if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
        {
            throw new ConflictException($"Email '{registerDto.Email}' is already registered.");
        }
        var user = new Models.User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("User registered successfully: {Username}", user.Username);
        return user;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password) == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed for email: {Email}", loginDto.Email);
            throw new UnauthorizedException("Invalid credentials.");
        }

        var (accessToken, accessTokenExpiration) = GenerateAccessToken(user);
        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id, GetIpAddressFromHttpContext());

        _logger.LogInformation("User logged in successfully: {Username}", user.Username);
        return new TokenResponseDto
        {
            AccessToken = accessToken,
            AccessTokenExpiration = accessTokenExpiration,
            RefreshToken = refreshToken.Token,
            UserId = user.Id.ToString(),
            Username = user.Username
        };
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto requestDto)
    {
        var principal = GetPrincipalFromExpiredToken(requestDto.ExpiredAccessToken);
        if (principal?.Identity?.Name == null)
        {
            _logger.LogWarning("Refresh token failed: Could not get principal from expired access token.");
            throw new UnauthorizedException("Invalid access token provided for refresh.");
        }

        var userIdString = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("Refresh token failed: Could not parse UserID from access token claims.");
            throw new UnauthorizedException("Invalid user identity in access token.");
        }

        var user = await _context.Users
                                 .Include(u => u.RefreshTokens)
                                 .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning("Refresh token failed: User {UserId} not found.", userId);
            throw new UnauthorizedException("User session not found.");
        };

        var existingRefreshToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == requestDto.RefreshToken);

        if (existingRefreshToken == null || !existingRefreshToken.IsActive)
        {
            _logger.LogWarning("Refresh token failed: Attempted use of invalid/revoked refresh token for User {UserId}.", userId);
            await RevokeAllUserRefreshTokensAsync(userId);
            throw new UnauthorizedException("Invalid or expired refresh session.");
        }

        var (newAccessToken, newAccessTokenExpiration) = GenerateAccessToken(user);
        var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id, GetIpAddressFromHttpContext());

        existingRefreshToken.Revoked = DateTime.UtcNow;
        existingRefreshToken.ReplacedByToken = newRefreshToken.Token;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Tokens refreshed successfully for User {UserId}", userId);
        return new TokenResponseDto
        {
            AccessToken = newAccessToken,
            AccessTokenExpiration = newAccessTokenExpiration,
            RefreshToken = newRefreshToken.Token,
            UserId = user.Id.ToString(),
            Username = user.Username
        };
    }

    public async Task<bool> LogoutAsync(ClaimsPrincipal userPrincipal, string? refreshTokenValue)
    {
        var userIdString = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            // This case should be rare if [Authorize] is used, but good to handle.
            _logger.LogWarning("Logout failed: Could not parse UserID from claims principal.");
            throw new BadRequestException("Invalid user session for logout.");
        }

        bool revokedSomething;
        if (!string.IsNullOrEmpty(refreshTokenValue))
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshTokenValue && rt.IsActive);

            if (refreshToken != null)
            {
                refreshToken.Revoked = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} logged out session with refresh token ending {TokenEnd}", userId, refreshTokenValue.Length > 4 ? refreshTokenValue.Substring(refreshTokenValue.Length - 4) : "****");
                revokedSomething = true;
            }
            else
            {
                 _logger.LogWarning("Logout failed: Active refresh token not found for User {UserId} with provided token value.", userId);
                 // Still return true as the goal is to end the session, and this specific one is already gone/invalid
                 return true;
            }
        }
        else
        {
            var activeRefreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            if (activeRefreshTokens.Any())
            {
                foreach (var token in activeRefreshTokens)
                {
                    token.Revoked = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("User {UserId} logged out from all active sessions.", userId);
                revokedSomething = true;
            } else {
                 _logger.LogInformation("User {UserId} attempted logout, but no active sessions found.", userId);
                 return true;
            }
        }
        return revokedSomething;
    }
	
	private (string token, DateTime expires) GenerateAccessToken(Models.User user)
	{
		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]
		                                  ?? throw new InvalidOperationException("JWT Key is not configured."));
		var issuer = _configuration["Jwt:Issuer"]
		             ?? throw new InvalidOperationException("JWT Issuer is not configured.");
		var audience = _configuration["Jwt:Audience"]
		               ?? throw new InvalidOperationException("JWT Audience is not configured.");
		var duration = Convert.ToDouble(_configuration["Jwt:DurationInHours"] ?? "1");

		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(new Claim[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.Username),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			}),
			Expires = DateTime.UtcNow.AddHours(duration),
			Issuer = issuer,
			Audience = audience,
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};

		var token = tokenHandler.CreateToken(tokenDescriptor);
		return (tokenHandler.WriteToken(token), tokenDescriptor.Expires.Value);
	}
	
	private async Task<RefreshToken> GenerateAndSaveRefreshTokenAsync(Guid userId, string? ipAddress)
	{
		var refreshTokenValue = GenerateSecureRandomString();
		var refreshToken = new RefreshToken
		{
			UserId = userId,
			Token = refreshTokenValue,
			Expires = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenTTLDays", 7)),
			CreatedByIp = ipAddress
		};

		_context.RefreshTokens.Add(refreshToken);
		await _context.SaveChangesAsync();
		return refreshToken;
	}
	
	private string GenerateSecureRandomString(int length = 64)
	{
		var randomNumber = new byte[length];
		using var rng = RandomNumberGenerator.Create();
		rng.GetBytes(randomNumber);

		return Convert.ToBase64String(randomNumber)
			.Replace("+", "-")
			.Replace("/", "_")
			.TrimEnd('=');
	}
	
	private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
	{
		var tokenValidationParameters = new TokenValidationParameters
		{
			ValidateAudience = true,
			ValidateIssuer = true,
			ValidAudience = _configuration["Jwt:Audience"],
			ValidIssuer = _configuration["Jwt:Issuer"],
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)),
			ValidateLifetime = false
		};

		var tokenHandler = new JwtSecurityTokenHandler();
		try
		{
			var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
			if (securityToken is not JwtSecurityToken jwtSecurityToken ||
			    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
			{
				return null;
			}
			return principal;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to validate expired token.");
			return null;
		}
	}

	private string? GetIpAddressFromHttpContext()
	{
		return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
	}

	private async Task RevokeAllUserRefreshTokensAsync(Guid userId)
	{
		var userRefreshTokens = await _context.RefreshTokens
			.Where(rt => rt.UserId == userId && rt.IsActive)
			.ToListAsync();

		foreach (var token in userRefreshTokens)
		{
			token.Revoked = DateTime.UtcNow;
		}
		await _context.SaveChangesAsync();
	}
}
