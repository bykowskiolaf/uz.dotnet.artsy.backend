using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using artsy.backend.Services.ExternalApis.Artsy.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace artsy.backend.Services.ExternalApis.Artsy;

public class ArtsyApiService : IArtsyApiService
{
	private const string ArtsyXappTokenCacheKey = "ArtsyXappToken";
	private readonly IConfiguration _configuration;
	private readonly HttpClient _httpClient;
	private readonly ILogger<ArtsyApiService> _logger;
	private readonly IMemoryCache _memoryCache;
	private string? _baseUrl;

	private string? _clientId;
	private string? _clientSecret;
	private string? _tokenUrl;


	public ArtsyApiService(
		IHttpClientFactory httpClientFactory,
		IConfiguration configuration,
		ILogger<ArtsyApiService> logger,
		IMemoryCache memoryCache)
	{
		_httpClient = httpClientFactory.CreateClient("ArtsyApiClient");
		_configuration = configuration;
		_logger = logger;
		_memoryCache = memoryCache;

		LoadConfiguration();
	}

	public async Task<ArtsyArtistDto?> GetArtistByIdAsync(string artistId)
	{
		if (string.IsNullOrEmpty(_baseUrl)) return null;

		return await GetAsync<ArtsyArtistDto>($"artists/{artistId}");
	}

	public async Task<ArtsyArtistDto?> GetArtistBySlugAsync(string artistSlug)
	{
		if (string.IsNullOrEmpty(_baseUrl)) return null;
		_logger.LogInformation("Fetching artist by slug: {ArtistSlug}", artistSlug);

		return await GetAsync<ArtsyArtistDto>($"artists/{artistSlug}");
	}

	public async Task<ArtsyListResponseDto<ArtsyArtistDto>?> GetArtistsAsync(int size = 10, int offset = 0, string? sortBy = null)
	{
		if (string.IsNullOrEmpty(_baseUrl)) return null;

		var queryParams = new List<string>
		{
			$"size={size}",
			$"offset={offset}",
			"total_count=1"
		};

		if (!string.IsNullOrEmpty(sortBy))
		{
			queryParams.Add($"sort={sortBy}");
		}

		string requestUri = $"artists?{string.Join("&", queryParams)}";
		_logger.LogInformation("Fetching artists from Artsy: {RequestUri}", requestUri);

		return await GetAsync<ArtsyListResponseDto<ArtsyArtistDto>>(requestUri);
	}

	private void LoadConfiguration()
	{
		_clientId = _configuration["ArtsyApiSettings:ClientId"];
		_clientSecret = _configuration["ArtsyApiSettings:ClientSecret"];
		_tokenUrl = _configuration["ArtsyApiSettings:TokenUrl"];
		_baseUrl = _configuration["ArtsyApiSettings:BaseUrl"];

		if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
		{
			_logger.LogError("Artsy Client ID or Client Secret is not configured. Please set ARTSY_CLIENT_ID and ARTSY_CLIENT_SECRET environment variables.");
		}

		if (string.IsNullOrEmpty(_tokenUrl) || string.IsNullOrEmpty(_baseUrl))
		{
			_logger.LogError("Artsy API TokenUrl or BaseUrl is not configured in ArtsyApiSettings.");
		}
	}

	private async Task<string?> GetXappTokenAsync()
	{
		if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret) || string.IsNullOrEmpty(_tokenUrl))
		{
			_logger.LogError("Artsy API client credentials or token URL are not configured.");

			return null;
		}

		// Try to get token from cache
		if (_memoryCache.TryGetValue(ArtsyXappTokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
		{
			_logger.LogDebug("Using cached Artsy XAPP token.");

			return cachedToken;
		}

		_logger.LogInformation("Fetching new Artsy XAPP token.");
		var request = new HttpRequestMessage(HttpMethod.Post, _tokenUrl)
		{
			Content = JsonContent.Create(new { client_id = _clientId, client_secret = _clientSecret })
		};

		try
		{
			var response = await _httpClient.SendAsync(request);
			response.EnsureSuccessStatusCode(); // Throws if not 2xx

			var tokenResponse = await response.Content.ReadFromJsonAsync<ArtsyTokenResponseDto>();
			if (tokenResponse?.Token == null || tokenResponse.ExpiresAt == null)
			{
				_logger.LogError("Failed to deserialize Artsy XAPP token response or token/expiry is missing.");

				return null;
			}

			// Cache the token with an expiration slightly before its actual expiry to be safe
			var cacheEntryOptions = new MemoryCacheEntryOptions()
				.SetAbsoluteExpiration(tokenResponse.ExpiresAt.Value.AddMinutes(-5)); // Cache for 5 mins less than actual expiry

			_memoryCache.Set(ArtsyXappTokenCacheKey, tokenResponse.Token, cacheEntryOptions);
			_logger.LogInformation("Successfully fetched and cached new Artsy XAPP token.");

			return tokenResponse.Token;
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Error fetching Artsy XAPP token. Status: {StatusCode}", ex.StatusCode);

			return null;
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, "Error deserializing Artsy XAPP token response.");

			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occurred while fetching Artsy XAPP token.");

			return null;
		}
	}

	private async Task<T?> GetAsync<T>(string requestUri) where T : class
	{
		var xappToken = await GetXappTokenAsync();
		if (string.IsNullOrEmpty(xappToken))
		{
			_logger.LogError("Cannot make Artsy API request without an XAPP token.");

			return null;
		}

		var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/{requestUri}");
		request.Headers.Add("X-Xapp-Token", xappToken);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

		try
		{
			var response = await _httpClient.SendAsync(request);
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				_logger.LogWarning("Artsy API request to {RequestUri} returned 404 Not Found.", requestUri);

				return null;
			}

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadFromJsonAsync<T>();
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Error making GET request to Artsy API {RequestUri}. Status: {StatusCode}", requestUri, ex.StatusCode);

			return null;
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, "Error deserializing response from Artsy API {RequestUri}.", requestUri);

			return null;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "An unexpected error occurred making GET request to Artsy API {RequestUri}.", requestUri);

			return null;
		}
	}
}
