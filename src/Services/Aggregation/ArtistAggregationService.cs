using artsy.backend.Dtos;
using artsy.backend.Services.ExternalApis.Artsy;
using artsy.backend.Services.ExternalApis.Artsy.Dtos;

namespace artsy.backend.Services.Aggregation;

public class ArtistAggregationService : IArtistAggregationService
{
	private readonly IArtsyApiService _artsyApiService;
	private readonly ILogger<ArtistAggregationService> _logger;

	public ArtistAggregationService(
		IArtsyApiService artsyApiService,
		ILogger<ArtistAggregationService> logger)
	{
		_artsyApiService = artsyApiService;
		_logger = logger;
	}

	public async Task<UnifiedArtistDto?> GetArtistBySlugAsync(string apiSlug, string sourceApiName = "artsy")
	{
		if (sourceApiName.Equals("artsy", StringComparison.OrdinalIgnoreCase))
		{
			var artsyArtist = await _artsyApiService.GetArtistBySlugAsync(apiSlug);

			return artsyArtist != null ? MapArtsyArtistToUnified(artsyArtist) : null;
		}

		_logger.LogWarning("Unsupported source API '{SourceApiName}' for GetArtistBySlugAsync.", sourceApiName);

		return null;
	}

	public async Task<IEnumerable<UnifiedArtistDto>> SearchArtistsAsync(string query, int page = 1, int pageSize = 10)
	{
		var unifiedArtists = new List<UnifiedArtistDto>();
		var tasks = new List<Task>();

		var artsySearchTask = Task.Run(async () =>
		{
			var artsyArtist = await _artsyApiService.GetArtistBySlugAsync(query);
			if (artsyArtist != null)
			{
				lock (unifiedArtists)
				{
					unifiedArtists.Add(MapArtsyArtistToUnified(artsyArtist));
				}
			}
		});

		tasks.Add(artsySearchTask);

		await Task.WhenAll(tasks);

		return unifiedArtists.Skip((page - 1) * pageSize).Take(pageSize);
	}

	public async Task<IEnumerable<UnifiedArtistDto>> GetArtistsAsync(int page = 1, int pageSize = 10)
	{
		if (page <= 0) page = 1;
		if (pageSize <= 0) pageSize = 10;

		int offset = (page - 1) * pageSize;
		int size = pageSize;

		_logger.LogInformation("Fetching artists for page {Page}, size {Size} (offset {Offset})", page, size, offset);

		string sortBy = "-trending";

		var artsyResponse = await _artsyApiService.GetArtistsAsync(size, offset, sortBy);

		if (artsyResponse?.Embedded?.Artists == null)
		{
			_logger.LogWarning("No artists found in the embedded content from Artsy for page {Page}.", page);

			return Enumerable.Empty<UnifiedArtistDto>();
		}

		var unifiedArtists = artsyResponse.Embedded.Artists
			.Where(artist => artist != null)
			.Select(MapArtsyArtistToUnified);

		return unifiedArtists;
	}


	private UnifiedArtistDto MapArtsyArtistToUnified(ArtsyArtistDto artsyArtist)
	{
		if (artsyArtist == null) throw new ArgumentNullException(nameof(artsyArtist));

		string? thumbnailUrl = null;
		if (artsyArtist.Links?.Image?.Templated == true && !string.IsNullOrEmpty(artsyArtist.Links.Image.Href) && artsyArtist.ImageVersions != null && artsyArtist.ImageVersions.Contains("square"))
		{
			thumbnailUrl = artsyArtist.Links.Image.Href.Replace("{image_version}", "square");
		}
		else if (!string.IsNullOrEmpty(artsyArtist.Links?.Thumbnail?.Href))
		{
			thumbnailUrl = artsyArtist.Links.Thumbnail.Href;
		}

		return new UnifiedArtistDto
		{
			Id = $"artsy-{artsyArtist.Slug}",
			Name = artsyArtist.Name ?? "Unknown Artist",
			Biography = artsyArtist.Biography,
			BirthYear = artsyArtist.Birthday,
			DeathYear = artsyArtist.Deathday,
			Nationality = artsyArtist.Nationality,
			ThumbnailUrl = thumbnailUrl,
			SourceApiName = "Artsy",
			OriginalSourceId = artsyArtist.Id ?? string.Empty,
			OriginalSourceUrl = artsyArtist.Links?.Self?.Href
		};
	}
}
