using artsy.backend.Dtos;
using artsy.backend.Services.ExternalApis.Artsy;
using artsy.backend.Services.ExternalApis.Artsy.Dtos;

namespace artsy.backend.Services.Aggregation;

public class ArtworkAggregationService : IArtworkAggregationService
{
	readonly static SemaphoreSlim _artistFetchSemaphore = new(4, 4);
	readonly IArtsyApiService _artsyApiService;
	readonly ILogger<ArtworkAggregationService> _logger;

	public ArtworkAggregationService(IArtsyApiService artsyApiService, ILogger<ArtworkAggregationService> logger)
	{
		_artsyApiService = artsyApiService;
		_logger = logger;
	}

	public async Task<IEnumerable<UnifiedArtworkDto>> GetArtworksAsync(int page = 1, int pageSize = 10)
	{
		if (page <= 0) page = 1;
		if (pageSize <= 0 || pageSize > 50) pageSize = 10;

		var offset = (page - 1) * pageSize;

		var artsyResponse = await _artsyApiService.GetArtworksAsync(pageSize, offset);
		var artsyArtworks = artsyResponse?.Embedded?.Artworks;

		if (artsyArtworks == null || !artsyArtworks.Any())
		{
			_logger.LogInformation("No artworks returned from Artsy for page {Page}.", page);

			return Enumerable.Empty<UnifiedArtworkDto>();
		}

		// Create tasks to fetch artist details and map artworks, but control their execution with a semaphore.
		var mappingTasks = artsyArtworks.Select(async artwork =>
		{
			if (artwork == null) return null;

			// Fetch the primary artist for this artwork, but throttle the calls.
			ArtsyArtistDto? primaryArtist = null;
			var artistsLink = artwork.Links?.ArtistsLink?.Href;

			if (!string.IsNullOrEmpty(artistsLink))
			{
				// Wait for an open "slot" from the semaphore before making the API call.
				await _artistFetchSemaphore.WaitAsync();
				try
				{
					// This block is now protected and will only run when one of the 4 "slots" is available.
					_logger.LogDebug("Fetching artist for artwork {ArtworkId} using link {ArtistLink}", artwork.Id, artistsLink);
					var artistListResponse = await _artsyApiService.GetArtistsByLinkAsync(artistsLink);
					primaryArtist = artistListResponse?.Embedded?.Artists?.FirstOrDefault();
				}
				catch (Exception ex)
				{
					// Log errors but don't let a single failed artist fetch stop the whole process.
					_logger.LogError(ex, "Failed to fetch artist details for artwork ID {ArtworkId}.", artwork.Id);
				}
				finally
				{
					// Always release the semaphore slot, even if the call fails.
					_artistFetchSemaphore.Release();
				}
			}

			return MapArtsyArtworkToUnified(artwork, primaryArtist);
		});

		var unifiedArtworksWithNulls = await Task.WhenAll(mappingTasks);

		return unifiedArtworksWithNulls.Where(dto => dto != null).Select(dto => dto!);
	}

	UnifiedArtworkDto MapArtsyArtworkToUnified(ArtsyArtworkDto artsyArtwork, ArtsyArtistDto? artist)
	{
		if (artsyArtwork == null) throw new ArgumentNullException(nameof(artsyArtwork));

		var imageUrl = artsyArtwork.Links?.Image?.Href?.Replace("{image_version}", "large")
		               ?? artsyArtwork.Links?.Thumbnail?.Href;

		return new UnifiedArtworkDto
		{
			Id = $"artsy-{artsyArtwork.Slug ?? artsyArtwork.Id}",
			Title = artsyArtwork.Title ?? "Untitled",
			ArtistDisplayName = artist?.Name ?? artsyArtwork.CollectingInstitution ?? "Unknown Artist",
			ArtistId = artist != null ? $"artsy-{artist.Slug ?? artist.Id}" : null,
			DateCreated = artsyArtwork.Date,
			Medium = artsyArtwork.Medium,
			ImageUrl = imageUrl,
			Description = artsyArtwork.Blurb,
			SourceApiName = "Artsy",
			OriginalSourceId = artsyArtwork.Id ?? string.Empty,
			OriginalSourceUrl = artsyArtwork.Links?.Permalink?.Href
		};
	}
}
