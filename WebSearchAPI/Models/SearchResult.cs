using System.Text.Json.Serialization;

namespace WebSearchAPI.Models;

/// <summary>
/// Represents a single search result from the web search API.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// The title of the search result.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the search result.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// A brief snippet or description of the search result.
    /// </summary>
    [JsonPropertyName("snippet")]
    public string Snippet { get; set; } = string.Empty;

    /// <summary>
    /// The source of the search result (e.g., "Google", "Bing").
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// The publish date of the content (if available).
    /// </summary>
    [JsonPropertyName("publishedDate")]
    public string? PublishedDate { get; set; }
}

/// <summary>
/// Represents the response from a search operation.
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// The original search query.
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Number of results returned.
    /// </summary>
    [JsonPropertyName("totalResults")]
    public int TotalResults { get; set; }

    /// <summary>
    /// List of search results.
    /// </summary>
    [JsonPropertyName("results")]
    public List<SearchResult> Results { get; set; } = new();

    /// <summary>
    /// Optional AI-generated summary of the results.
    /// </summary>
    [JsonPropertyName("aiSummary")]
    public string? AiSummary { get; set; }

    /// <summary>
    /// Timestamp of when the search was performed.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request model for search endpoint.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// The search query string.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return (default: 10).
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// Whether to include AI summary (default: true).
    /// </summary>
    public bool IncludeAiSummary { get; set; } = true;
}
