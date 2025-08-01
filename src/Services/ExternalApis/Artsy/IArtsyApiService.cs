using artsy.backend.Services.ExternalApis.Artsy.Dtos;

namespace artsy.backend.Services.ExternalApis.Artsy;

public interface IArtsyApiService
{
	Task<ArtsyArtistDto?> GetArtistByIdAsync(string artistId);
	Task<ArtsyArtistDto?> GetArtistBySlugAsync(string artistSlug);
	Task<ArtsyListResponseDto<ArtsyArtistDto>?> GetArtistsByLinkAsync(string fullUrl);
	Task<ArtsyListResponseDto<ArtsyArtistDto>?> GetArtistsAsync(int size = 10, int offset = 0, string? sortBy = null);
	Task<ArtsyListResponseDto<ArtsyArtworkDto>?> GetArtworksAsync(int size = 10, int offset = 0);
}
