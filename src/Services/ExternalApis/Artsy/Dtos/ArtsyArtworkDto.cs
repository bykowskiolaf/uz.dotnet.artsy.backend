using System.Text.Json.Serialization;

namespace artsy.backend.Services.ExternalApis.Artsy.Dtos;

public class ArtsyArtworkDto
{
	[JsonPropertyName("id")] public string? Id { get; set; }

	[JsonPropertyName("slug")] public string? Slug { get; set; }

	[JsonPropertyName("title")] public string? Title { get; set; }

	[JsonPropertyName("category")] public string? Category { get; set; }

	[JsonPropertyName("medium")] public string? Medium { get; set; }

	[JsonPropertyName("date")] public string? Date { get; set; }

	[JsonPropertyName("dimensions")] public ArtsyDimensionsDto? Dimensions { get; set; }

	[JsonPropertyName("collecting_institution")]
	public string? CollectingInstitution { get; set; }

	[JsonPropertyName("image_versions")] public List<string>? ImageVersions { get; set; }

	[JsonPropertyName("blurb")] public string? Blurb { get; set; }

	[JsonPropertyName("_links")] public ArtsyArtworkLinksDto? Links { get; set; }
}

public class ArtsyDimensionsDto
{
	[JsonPropertyName("in")] public ArtsyDimensionUnitDto? Inches { get; set; }

	[JsonPropertyName("cm")] public ArtsyDimensionUnitDto? Centimeters { get; set; }
}

public class ArtsyDimensionUnitDto
{
	[JsonPropertyName("text")] public string? Text { get; set; }

	[JsonPropertyName("height")] public double? Height { get; set; }

	[JsonPropertyName("width")] public double? Width { get; set; }
}

public class ArtsyArtworkLinksDto
{
	[JsonPropertyName("thumbnail")] public ArtsyLinkDto? Thumbnail { get; set; }

	[JsonPropertyName("image")] public ArtsyImageLinkDto? Image { get; set; }

	[JsonPropertyName("permalink")] public ArtsyLinkDto? Permalink { get; set; }

	[JsonPropertyName("artists")] public ArtsyLinkDto? ArtistsLink { get; set; }
}
