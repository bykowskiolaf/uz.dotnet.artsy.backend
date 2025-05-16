using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using artsy.backend.Data;
using artsy.backend.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;

namespace artsy.backend.Services.Auth;

public class AuthService : IAuthService
{
	private readonly ApplicationDbContext _context;
	private readonly IPasswordHasher<Models.User> _passwordHasher;
	private readonly IConfiguration _configuration;
	
	public AuthService(
		ApplicationDbContext context,
		IPasswordHasher<Models.User> passwordHasher,
		IConfiguration configuration)
	{
		_context = context;
		_passwordHasher = passwordHasher;
		_configuration = configuration;
	}
	public async Task<Models.User?> RegisterAsync(RegisterDto registerDto)
	{
		// Check if username or email already exists
		if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
		{
			// TODO: Throw custom exception or return a specific error
			return null;
		}

		if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
		{
			// TODO: Throw custom exception or return a specific error
			return null;
		}

		var user = new Models.User
		{
			Username = registerDto.Username,
			Email = registerDto.Email,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		// Hash the password
		user.PasswordHash = _passwordHasher.HashPassword(user, registerDto.Password);

		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		return user;
	}
	
	public async Task<TokenResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
        {
	        // TODO: Throw custom exception or return a specific error
            return null;
        }

        var passwordVerificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
        if (passwordVerificationResult == PasswordVerificationResult.Failed)
        {
	        // TODO: Throw custom exception or return a specific error
            return null;
        }

        // Password is valid, generate JWT
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured."));

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:DurationInHours"] ?? "1")),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new TokenResponseDto
        {
            Token = tokenString,
            Expiration = tokenDescriptor.Expires.Value,
            UserId = user.Id.ToString(),
            Username = user.Username
        };
    }
}
