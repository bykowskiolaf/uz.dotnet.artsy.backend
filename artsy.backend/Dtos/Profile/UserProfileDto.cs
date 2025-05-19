namespace artsy.backend.Dtos.Profile;

public class UserProfileDto
{
	public string UserId { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? FullName { get; set; }
	public string? Bio { get; set; }
}

