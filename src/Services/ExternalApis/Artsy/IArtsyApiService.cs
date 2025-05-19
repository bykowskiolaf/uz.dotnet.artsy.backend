using artsy.backend.Services.ExternalApis.Artsy.Dtos;

namespace artsy.backend.Services.ExternalApis.Artsy;

public interface IArtsyApiService
{
	Task<ArtsyArtistDto?> GetArtistByIdAsync(string artistId);
	Task<ArtsyArtistDto?> GetArtistBySlugAsync(string artistSlug); 
}
