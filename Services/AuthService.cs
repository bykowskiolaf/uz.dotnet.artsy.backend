using artsy.backend.Data;
using artsy.backend.Dtos.Auth;
using artsy.backend.Models;
using Microsoft.EntityFrameworkCore;
using artsy.backend.Services;


namespace Artsy.Backend.Services;

using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

public class AuthService : IAuthService
{
	private readonly ApplicationDbContext _context;
	private readonly IPasswordHasher<User> _passwordHasher;

	public AuthService(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
	{
		_context = context;
		_passwordHasher = passwordHasher;
	}

	public async Task<User?> RegisterAsync(RegisterDto registerDto)
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

		var user = new User
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
}
