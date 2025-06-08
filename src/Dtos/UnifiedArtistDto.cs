namespace artsy.backend.Dtos;

public class UnifiedArtistDto
{
	public string Id { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Biography { get; set; }
	public string? BirthYear { get; set; }
	public string? DeathYear { get; set; }
	public string? Nationality { get; set; }
	public string? ThumbnailUrl { get; set; }
	public string SourceApiName { get; set; } = string.Empty;
	public string OriginalSourceId { get; set; } = string.Empty;
	public string? OriginalSourceUrl { get; set; }
}
