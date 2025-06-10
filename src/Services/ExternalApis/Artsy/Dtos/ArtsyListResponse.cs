using System.Text.Json.Serialization;

namespace artsy.backend.Services.ExternalApis.Artsy.Dtos;

public class ArtsyListResponseDto<T>
{
	[JsonPropertyName("total_count")] public int? TotalCount { get; set; }

	[JsonPropertyName("_links")] public ArtsyListLinksDto? Links { get; set; }

	[JsonPropertyName("_embedded")] public ArtsyEmbeddedDto<T>? Embedded { get; set; }
}
