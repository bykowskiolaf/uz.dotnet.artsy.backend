using System.Text.Json.Serialization;

namespace artsy.backend.Services.ExternalApis.Artsy.Dtos;

public class ArtsyTokenResponseDto
{
	[JsonPropertyName("type")]
	public string? Type { get; set; }

	[JsonPropertyName("token")]
	public string? Token { get; set; }

	[JsonPropertyName("expires_at")]
	public DateTimeOffset? ExpiresAt { get; set; }
}