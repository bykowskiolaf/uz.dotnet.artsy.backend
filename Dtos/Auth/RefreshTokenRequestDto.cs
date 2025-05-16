using System.ComponentModel.DataAnnotations;

namespace artsy.backend.Dtos.Auth;

public class RefreshTokenRequestDto
{
	[Required]
	public string ExpiredAccessToken { get; set; } = string.Empty;
	[Required]
	public string RefreshToken { get; set; } = string.Empty;
}