using artsy.backend.Data;
using artsy.backend.Dtos.Profile;
using System.Security.Claims;

namespace artsy.backend.Services.User;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    private bool TryGetUserId(ClaimsPrincipal userPrincipal, out Guid userId)
    {
        userId = Guid.Empty;

        if (userPrincipal == null)
        {
            return false;
        }

        var userIdString = userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out userId);
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(ClaimsPrincipal userPrincipal)
    {
        if (!TryGetUserId(userPrincipal, out var userId))
        {
            return null;
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return null;
        }

        return new UserProfileDto()
        {
            UserId = user.Id.ToString(),
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Bio = user.Bio
        };
    }

    public async Task<bool> UpdateUserProfileAsync(ClaimsPrincipal userPrincipal, UpdateProfileDto updateProfileDto)
    {
        if (!TryGetUserId(userPrincipal, out var userId))
        {
            return false;
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (updateProfileDto.FullName != null && user.FullName != updateProfileDto.FullName) // Zmienione na && zamiast ||
        {
             user.FullName = updateProfileDto.FullName;
        }
        if (updateProfileDto.Bio != null && user.Bio != updateProfileDto.Bio) // Zmienione na && zamiast ||
        {
            user.Bio = updateProfileDto.Bio;
        }

        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Update(user);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }
}