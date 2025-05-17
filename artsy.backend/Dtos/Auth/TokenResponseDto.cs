namespace artsy.backend.Dtos.Auth;

public class TokenResponseDto
{
	public string AccessToken { get; set; } = string.Empty;
	public DateTime AccessTokenExpiration { get; set; }
	public string RefreshToken { get; set; } = string.Empty;
	public string UserId { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
}
