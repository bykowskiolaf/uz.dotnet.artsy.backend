using System.Text.Json.Serialization;

namespace artsy.backend.Services.ExternalApis.Artsy.Dtos;

public class ArtsyArtistDto
{
	[JsonPropertyName("id")] public string? Id { get; set; }

	[JsonPropertyName("slug")] public string? Slug { get; set; }

	[JsonPropertyName("name")] public string? Name { get; set; }

	[JsonPropertyName("sortable_name")] public string? SortableName { get; set; }

	[JsonPropertyName("gender")] public string? Gender { get; set; }

	[JsonPropertyName("biography")] public string? Biography { get; set; }

	[JsonPropertyName("birthday")] public string? Birthday { get; set; }

	[JsonPropertyName("deathday")] public string? Deathday { get; set; }

	[JsonPropertyName("hometown")] public string? Hometown { get; set; }

	[JsonPropertyName("location")] public string? Location { get; set; }

	[JsonPropertyName("nationality")] public string? Nationality { get; set; }

	[JsonPropertyName("image_versions")] public List<string>? ImageVersions { get; set; }

	[JsonPropertyName("_links")] public ArtsyArtistLinksDto? Links { get; set; }
}

public class ArtsyArtistLinksDto
{
	[JsonPropertyName("thumbnail")] public ArtsyLinkDto? Thumbnail { get; set; }

	[JsonPropertyName("image")] public ArtsyImageLinkDto? Image { get; set; }

	[JsonPropertyName("artworks")] public ArtsyLinkDto? Artworks { get; set; }

	[JsonPropertyName("self")] public ArtsyLinkDto? Self { get; set; }
}
