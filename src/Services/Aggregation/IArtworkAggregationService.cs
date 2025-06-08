using artsy.backend.Dtos;

namespace artsy.backend.Services.Aggregation;

public interface IArtworkAggregationService
{
	Task<IEnumerable<UnifiedArtworkDto>> GetArtworksAsync(int page = 1, int pageSize = 10);
}
