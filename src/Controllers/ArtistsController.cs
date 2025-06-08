using artsy.backend.Services.Aggregation;
using Microsoft.AspNetCore.Mvc;

namespace artsy.backend.controllers;

[Route("api/[controller]")]
[ApiController]
public class ArtistsController : ControllerBase
{
	readonly IArtistAggregationService _artistAggregationService;

	public ArtistsController(IArtistAggregationService artistAggregationService)
	{
		_artistAggregationService = artistAggregationService;
	}

	[HttpGet("{sourceApi}/{slug}")]
	public async Task<IActionResult> GetArtistBySourceAndSlug(string sourceApi, string slug)
	{
		var artist = await _artistAggregationService.GetArtistBySlugAsync(slug, sourceApi);
		if (artist == null)
		{
			return NotFound(new { message = $"Artist with slug '{slug}' from source '{sourceApi}' not found." });
		}

		return Ok(artist);
	}

	[HttpGet("search")]
	public async Task<IActionResult> SearchArtists([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return BadRequest(new { message = "Search query cannot be empty." });
		}

		var artists = await _artistAggregationService.SearchArtistsAsync(query, page, pageSize);

		return Ok(artists);
	}

	[HttpGet]
	public async Task<IActionResult> GetArtists([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
	{
		if (page < 1 || pageSize < 1 || pageSize > 100)
		{
			return BadRequest(new { message = "Invalid pagination parameters." });
		}

		var artists = await _artistAggregationService.GetArtistsAsync(page, pageSize);

		return Ok(artists);
	}
}
