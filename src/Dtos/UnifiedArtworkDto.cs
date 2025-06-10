namespace artsy.backend.Dtos;

public class UnifiedArtworkDto
{
	public string Id { get; set; } = string.Empty;

	public string Title { get; set; } = string.Empty;

	public string? ArtistDisplayName { get; set; }

	public string? ArtistId { get; set; }

	public string? DateCreated { get; set; }

	public string? Medium { get; set; }

	public string? ImageUrl { get; set; }

	public string? Description { get; set; }

	public string SourceApiName { get; set; } = string.Empty;

	public string OriginalSourceId { get; set; } = string.Empty;

	public string? OriginalSourceUrl { get; set; }
}
