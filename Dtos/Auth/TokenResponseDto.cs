namespace artsy.backend.Dtos.Auth;

public class TokenResponseDto
{
	public string Token { get; set; } = string.Empty;
	public DateTime Expiration { get; set; }
	public string UserId { get; set; } = string.Empty;
	public string Username { get; set; } = string.Empty;
}
