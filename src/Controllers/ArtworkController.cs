using artsy.backend.Services.Aggregation;
using Microsoft.AspNetCore.Mvc;

namespace artsy.backend.controllers;

[Route("api/[controller]")]
[ApiController]
public class ArtworksController : ControllerBase
{
	private readonly IArtworkAggregationService _artworkAggregationService;

	public ArtworksController(IArtworkAggregationService artworkAggregationService)
	{
		_artworkAggregationService = artworkAggregationService;
	}

	[HttpGet]
	public async Task<IActionResult> GetArtworks([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
	{
		if (page < 1 || pageSize < 1 || pageSize > 100)
		{
			return BadRequest(new { message = "Invalid pagination parameters." });
		}

		var artworks = await _artworkAggregationService.GetArtworksAsync(page, pageSize);

		return Ok(artworks);
	}
}
