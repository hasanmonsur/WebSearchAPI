using Microsoft.AspNetCore.Mvc;
using WebSearchAPI.Models;
using WebSearchAPI.Services;

namespace WebSearchAPI.Controllers;

/// <summary>
/// API controller for web search operations.
/// Provides endpoints for AI-powered web search functionality.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IWebSearchService _webSearchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IWebSearchService webSearchService,
        ILogger<SearchController> logger)
    {
        _webSearchService = webSearchService;
        _logger = logger;
    }

    /// <summary>
    /// Performs a web search with the given query.
    /// </summary>
    /// <param name="request">The search request containing query and options.</param>
    /// <returns>Search results with optional AI summary.</returns>
    /// <response code="200">Search completed successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="500">Internal server error.</response>
    [HttpPost]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SearchResponse>> Search([FromBody] SearchRequest request)
    {
        // Validate the request
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            _logger.LogWarning("Search request received with empty query.");
            return BadRequest(new { error = "Query cannot be empty." });
        }

        if (request.MaxResults < 1 || request.MaxResults > 50)
        {
            _logger.LogWarning("Invalid maxResults: {MaxResults}", request.MaxResults);
            return BadRequest(new { error = "MaxResults must be between 1 and 50." });
        }

        try
        {
            _logger.LogInformation("Processing search request for query: {Query}", request.Query);
            
            var response = await _webSearchService.SearchAsync(
                request.Query, 
                request.MaxResults, 
                request.IncludeAiSummary);

            _logger.LogInformation("Search completed. Found {Count} results.", response.TotalResults);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing search request for query: {Query}", request.Query);
            return StatusCode(500, new { error = "An error occurred while processing your search request." });
        }
    }

    /// <summary>
    /// Performs a simple GET-based search.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="maxResults">Maximum number of results (default: 10).</param>
    /// <returns>Search results with optional AI summary.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SearchResponse>> SearchGet(
        [FromQuery] string query, 
        [FromQuery] int maxResults = 10)
    {
        // Validate the request
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required." });
        }

        if (maxResults < 1 || maxResults > 50)
        {
            return BadRequest(new { error = "MaxResults must be between 1 and 50." });
        }

        try
        {
            _logger.LogInformation("Processing GET search request for query: {Query}", query);
            
            var response = await _webSearchService.SearchAsync(query, maxResults, true);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GET search request for query: {Query}", query);
            return StatusCode(500, new { error = "An error occurred while processing your search request." });
        }
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    /// <returns>API status.</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new 
        { 
            status = "healthy", 
            service = "WebSearchAPI",
            timestamp = DateTime.UtcNow 
        });
    }
}
