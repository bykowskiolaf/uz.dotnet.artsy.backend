using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using artsy.backend.exceptions;
using artsy.backend.Exceptions;
using artsy.backend.Services.ExternalApis.Artsy.Dtos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace artsy.backend.Services.ExternalApis.Artsy;

public class ArtsyApiSettings
{
	public const string SectionName = "ArtsyApiSettings";
	public string? BaseUrl { get; set; }
	public string? TokenUrl { get; set; }
	public string? ClientId { get; set; }
	public string? ClientSecret { get; set; }
}

public class ArtsyApiService : IArtsyApiService
{
	const string ArtsyXappTokenCacheKey = "ArtsyXappToken";
	readonly HttpClient _httpClient;
	readonly ILogger<ArtsyApiService> _logger;
	readonly IMemoryCache _memoryCache;
	readonly ArtsyApiSettings _settings;

	public ArtsyApiService(
		IHttpClientFactory httpClientFactory,
		IOptions<ArtsyApiSettings> artsyApiOptions,
		ILogger<ArtsyApiService> logger,
		IMemoryCache memoryCache)
	{
		_httpClient = httpClientFactory.CreateClient("ArtsyApiClient");
		_settings = artsyApiOptions.Value;
		_logger = logger;
		_memoryCache = memoryCache;

		if (string.IsNullOrEmpty(_settings.ClientId) || string.IsNullOrEmpty(_settings.ClientSecret))
			_logger.LogError("Artsy Client ID or Client Secret is not configured. Check configuration for ArtsyApiSettings or ARTSY_CLIENT_ID/SECRET environment variables.");

		if (string.IsNullOrEmpty(_settings.TokenUrl) || string.IsNullOrEmpty(_settings.BaseUrl))
			_logger.LogError("Artsy API TokenUrl or BaseUrl is not configured in ArtsyApiSettings.");
	}

	public Task<ArtsyArtistDto> GetArtistByIdAsync(string artistId)
	{
		return GetAsync<ArtsyArtistDto>($"artists/{artistId}");
	}


	public Task<ArtsyArtistDto> GetArtistBySlugAsync(string artistSlug)
	{
		_logger.LogInformation("Fetching artist by slug: {ArtistSlug}", artistSlug);

		return GetAsync<ArtsyArtistDto>($"artists/{artistSlug}");
	}

	public Task<ArtsyListResponseDto<ArtsyArtistDto>> GetArtistsAsync(int size = 10, int offset = 0, string? sortBy = null)
	{
		var queryParams = new List<string> { $"size={size}", $"offset={offset}", "total_count=1" };
		if (!string.IsNullOrEmpty(sortBy)) queryParams.Add($"sort={sortBy}");

		var requestUri = $"artists?{string.Join("&", queryParams)}";
		_logger.LogInformation("Fetching artists from Artsy: {RequestUri}", requestUri);

		return GetAsync<ArtsyListResponseDto<ArtsyArtistDto>>(requestUri);
	}

	public Task<ArtsyListResponseDto<ArtsyArtistDto>> GetArtistsByLinkAsync(string fullUrl)
	{
		_logger.LogInformation("Fetching artists by absolute link: {FullUrl}", fullUrl);

		return SendRequestAsync<ArtsyListResponseDto<ArtsyArtistDto>>(new HttpRequestMessage(HttpMethod.Get, fullUrl));
	}

	public Task<ArtsyListResponseDto<ArtsyArtworkDto>> GetArtworksAsync(int size = 10, int offset = 0)
	{
		var queryParams = new List<string> { $"size={size}", $"offset={offset}", "total_count=1" };

		var requestUri = $"artworks?{string.Join("&", queryParams)}";
		_logger.LogInformation("Fetching artworks from Artsy: {RequestUri}", requestUri);

		return GetAsync<ArtsyListResponseDto<ArtsyArtworkDto>>(requestUri);
	}

	Task<T?> GetAsync<T>(string relativeUri) where T : class
	{
		if (string.IsNullOrEmpty(_settings.BaseUrl))
		{
			_logger.LogError("Cannot make relative GET request because BaseUrl is not configured.");

			return Task.FromResult<T?>(null);
		}

		var request = new HttpRequestMessage(HttpMethod.Get, $"{_settings.BaseUrl}/{relativeUri}");

		return SendRequestAsync<T>(request);
	}

	async Task<string> GetXappTokenAsync() // Returns string, not string?
	{
		if (_memoryCache.TryGetValue(ArtsyXappTokenCacheKey, out string? cachedToken))
		{
			_logger.LogDebug("Using cached Artsy XAPP token.");

			return cachedToken;
		}

		if (string.IsNullOrEmpty(_settings.ClientId) || string.IsNullOrEmpty(_settings.ClientSecret) || string.IsNullOrEmpty(_settings.TokenUrl))
		{
			_logger.LogError("Cannot fetch Artsy XAPP token: client credentials or token URL are not configured.");

			throw new InvalidOperationException("Artsy API client credentials or token URL are not configured.");
		}

		_logger.LogInformation("Fetching new Artsy XAPP token.");
		var request = new HttpRequestMessage(HttpMethod.Post, _settings.TokenUrl)
		{
			Content = JsonContent.Create(new { client_id = _settings.ClientId, client_secret = _settings.ClientSecret })
		};

		try
		{
			var response = await _httpClient.SendAsync(request);
			response.EnsureSuccessStatusCode();

			var tokenResponse = await response.Content.ReadFromJsonAsync<ArtsyTokenResponseDto>();
			if (tokenResponse?.Token == null || tokenResponse.ExpiresAt == null)
			{
				_logger.LogError("Failed to deserialize Artsy XAPP token response or token/expiry is missing.");

				throw new ExternalApiException("Artsy", "Received an invalid token response from Artsy API.");
			}

			var cacheEntryOptions = new MemoryCacheEntryOptions()
				.SetAbsoluteExpiration(tokenResponse.ExpiresAt.Value.AddMinutes(-5));

			_memoryCache.Set(ArtsyXappTokenCacheKey, tokenResponse.Token, cacheEntryOptions);
			_logger.LogInformation("Successfully fetched and cached new Artsy XAPP token.");

			return tokenResponse.Token;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to acquire Artsy XAPP token.");

			throw new ExternalApiException("Artsy", "Could not acquire authentication token from Artsy.", null, ex);
		}
	}

	async Task<T> SendRequestAsync<T>(HttpRequestMessage request) where T : class
	{
		var xappToken = await GetXappTokenAsync();

		request.Headers.Add("X-Xapp-Token", xappToken);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		HttpResponseMessage response;
		try
		{
			response = await _httpClient.SendAsync(request);

			if (response.StatusCode == HttpStatusCode.NotFound)
				throw new NotFoundException($"The requested resource was not found on the Artsy API: {request.RequestUri}");

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				_logger.LogError("Artsy API returned a non-success status code {StatusCode}. Content: {Content}", response.StatusCode, errorContent);

				throw new ExternalApiException("Artsy", "Received an error from the Artsy API.", response.StatusCode);
			}

			if (response.Content.Headers.ContentLength == 0)
				throw new ExternalApiException("Artsy", "Received a successful but empty response from Artsy API.", response.StatusCode);

			var result = await response.Content.ReadFromJsonAsync<T>();

			if (result == null)
				throw new ExternalApiException("Artsy", "Failed to deserialize a valid response from Artsy API.", response.StatusCode);

			return result;
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, "JSON deserialization error from Artsy API {RequestUri}.", request.RequestUri);

			throw new ExternalApiException("Artsy", "Failed to parse response from Artsy.", null, ex);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occurred making request to Artsy API {RequestUri}.", request.RequestUri);

			throw new ExternalApiException("Artsy", "An unexpected error occurred while communicating with the Artsy API.", null, ex);
		}
	}
}
