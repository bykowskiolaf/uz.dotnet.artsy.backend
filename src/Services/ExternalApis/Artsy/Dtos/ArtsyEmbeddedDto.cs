using System.Text.Json.Serialization;

namespace artsy.backend.Services.ExternalApis.Artsy.Dtos;

public class ArtsyEmbeddedDto<T>
{
	[JsonPropertyName("artists")] public List<T>? Artists { get; set; }

	[JsonPropertyName("artworks")] public List<T>? Artworks { get; set; }
}
