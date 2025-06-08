using System.Text.Json.Serialization;

namespace artsy.backend.Services.ExternalApis.Artsy.Dtos;

public class ArtsyListLinksDto
{
	[JsonPropertyName("self")] public ArtsyLinkDto? Self { get; set; }

	[JsonPropertyName("next")] public ArtsyLinkDto? Next { get; set; }
}
