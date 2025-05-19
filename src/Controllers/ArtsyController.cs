using artsy.backend.Services.ExternalApis.Artsy;
using Microsoft.AspNetCore.Mvc;

namespace artsy.backend.controllers;

[Route("api/artsy")]
[ApiController]
public class ArtsyDataController : ControllerBase
{
	private readonly IArtsyApiService _artsyApiService;

	public ArtsyDataController(IArtsyApiService artsyApiService)
	{
		_artsyApiService = artsyApiService;
	}

	[HttpGet("artists/{slug}")] // e.g., /api/artsy/artists/andy-warhol
	public async Task<IActionResult> GetArtistBySlug(string slug)
	{
		var artist = await _artsyApiService.GetArtistBySlugAsync(slug);
		if (artist == null)
		{
			return NotFound(new { message = $"Artist with slug '{slug}' not found on Artsy." });
		}
		return Ok(artist);
	}

	// Add more endpoints for artworks, search, etc.
	// [HttpGet("artworks/search")]
	// public async Task<IActionResult> SearchArtworks([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int size = 10)
	// {
	//     var results = await _artsyApiService.SearchArtworksAsync(query, page, size);
	//     if (results == null) // Or check results.Embedded.Results.Count
	//     {
	//         return Ok(new List<object>()); // Empty list
	//     }
	//     return Ok(results);
	// }
}
