using artsy.backend.Dtos;

namespace artsy.backend.Services.Aggregation;

public interface IArtistAggregationService
{
	Task<UnifiedArtistDto?> GetArtistBySlugAsync(string apiSlug, string sourceApiName = "artsy");
	Task<IEnumerable<UnifiedArtistDto>> SearchArtistsAsync(string query, int page = 1, int pageSize = 10);
	Task<IEnumerable<UnifiedArtistDto>> GetArtistsAsync(int page = 1, int pageSize = 10);
}
