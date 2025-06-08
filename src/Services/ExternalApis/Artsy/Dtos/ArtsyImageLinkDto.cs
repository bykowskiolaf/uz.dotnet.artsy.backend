using System.Text.Json.Serialization;

namespace artsy.backend.Services.ExternalApis.Artsy.Dtos;

public class ArtsyImageLinkDto : ArtsyLinkDto
{
	[JsonPropertyName("templated")] public bool Templated { get; set; }
}
