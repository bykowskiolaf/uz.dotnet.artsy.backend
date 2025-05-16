using System.Security.Claims;
using artsy.backend.Dtos.Profile;

namespace artsy.backend.Services.User;

public interface IUserService
{
	Task<UserProfileDto?> GetUserProfileAsync(ClaimsPrincipal userPrincipal);
	Task<bool> UpdateUserProfileAsync(ClaimsPrincipal userPrincipal, UpdateProfileDto updateProfileDto);
}
