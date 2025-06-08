using System.Text.Json.Serialization;

namespace artsy.backend.Services.ExternalApis.Artsy.Dtos;

public class ArtsyLinkDto
{
	[JsonPropertyName("href")] public string? Href { get; set; }
}
