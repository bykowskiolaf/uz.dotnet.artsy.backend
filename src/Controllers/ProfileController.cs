using artsy.backend.Dtos.Profile;
using artsy.backend.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace artsy.backend.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ProfileController : ControllerBase
{
	private readonly IUserService _userService;

	public ProfileController(IUserService userService)
	{
		_userService = userService;
	}

	[HttpGet("me")]
	public async Task<IActionResult> GetMyProfile()
	{
		var userProfile = await _userService.GetUserProfileAsync(User);

		if (userProfile == null)
		{
			return NotFound(new { message = "User profile not found." });
		}
		return Ok(userProfile);
	}

	[HttpPut("me")]
	public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto profileDto)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var success = await _userService.UpdateUserProfileAsync(User, profileDto);

		if (!success)
		{
			// Could be user not found, or update failed for other reasons
			return BadRequest(new { message = "Profile update failed." });
		}

		return Ok(new { message = "Profile updated successfully." });
	}
}
