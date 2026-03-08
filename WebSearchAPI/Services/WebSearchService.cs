using System.Text.Json;
using WebSearchAPI.Models;

namespace WebSearchAPI.Services;

/// <summary>
/// Interface for web search operations.
/// </summary>
public interface IWebSearchService
{
    /// <summary>
    /// Performs a web search with the given query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="includeAiSummary">Whether to include AI-generated summary.</param>
    /// <returns>Search response with results.</returns>
    Task<SearchResponse> SearchAsync(string query, int maxResults = 10, bool includeAiSummary = true);
}

/// <summary>
/// Service for performing web searches using various search providers.
/// This implementation supports Tavily, Perplexity, and CrustData APIs.
/// </summary>
public class WebSearchService : IWebSearchService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebSearchService> _logger;

    // Configuration keys
    private const string ApiKeyConfigKey = "WebSearch:ApiKey";
    private const string ProviderConfigKey = "WebSearch:Provider";
    private const string DefaultProvider = "tavily";

    public WebSearchService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WebSearchService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SearchResponse> SearchAsync(string query, int maxResults = 10, bool includeAiSummary = true)
    {
        _logger.LogInformation("Processing search query: {Query}", query);

        var apiKey = _configuration[ApiKeyConfigKey];
        var provider = _configuration[ProviderConfigKey] ?? DefaultProvider;

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No API key configured. Returning mock results for demonstration.");
            return await GetMockSearchResultsAsync(query, maxResults, includeAiSummary);
        }

        try
        {
            // Route to the appropriate search provider
            return provider.ToLower() switch
            {
                "tavily" => await SearchWithTavilyAsync(query, apiKey, maxResults, includeAiSummary),
                "perplexity" => await SearchWithPerplexityAsync(query, apiKey, maxResults, includeAiSummary),
                "crustdata" => await SearchWithCrustDataAsync(query, apiKey, maxResults, includeAiSummary),
                _ => await GetMockSearchResultsAsync(query, maxResults, includeAiSummary)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search with provider {Provider}", provider);
            // Fallback to mock results on error
            return await GetMockSearchResultsAsync(query, maxResults, includeAiSummary);
        }
    }

    /// <summary>
    /// Searches using Tavily API.
    /// Tavily is a search engine optimized for AI agents with real-time information.
    /// </summary>
    private async Task<SearchResponse> SearchWithTavilyAsync(
        string query, string apiKey, int maxResults, bool includeAiSummary)
    {
        var client = _httpClientFactory.CreateClient();
        
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/search");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        
        var requestBody = new
        {
            query = query,
            max_results = maxResults,
            include_answer = includeAiSummary,
            include_raw_content = false
        };
        
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var tavilyResponse = JsonSerializer.Deserialize<TavilySearchResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var searchResponse = new SearchResponse
        {
            Query = query,
            TotalResults = tavilyResponse?.Results?.Count ?? 0,
            Results = tavilyResponse?.Results?.Select(r => new SearchResult
            {
                Title = r.Title ?? string.Empty,
                Url = r.Url ?? string.Empty,
                Snippet = r.Content ?? string.Empty,
                Source = "Tavily"
            }).ToList() ?? new List<SearchResult>()
        };

        if (includeAiSummary && !string.IsNullOrEmpty(tavilyResponse?.Answer))
        {
            searchResponse.AiSummary = tavilyResponse.Answer;
        }
        else if (includeAiSummary)
        {
            // Generate AI summary using our simulated method
            searchResponse.AiSummary = await GenerateAiSummaryAsync(searchResponse.Results);
        }

        return searchResponse;
    }

    /// <summary>
    /// Searches using Perplexity API.
    /// Perplexity provides conversational search results.
    /// </summary>
    private async Task<SearchResponse> SearchWithPerplexityAsync(
        string query, string apiKey, int maxResults, bool includeAiSummary)
    {
        var client = _httpClientFactory.CreateClient();
        
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.perplexity.ai/search");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        
        var requestBody = new
        {
            query = query,
            max_results = maxResults
        };
        
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var perplexityResponse = JsonSerializer.Deserialize<PerplexitySearchResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var searchResponse = new SearchResponse
        {
            Query = query,
            TotalResults = perplexityResponse?.Results?.Count ?? 0,
            Results = perplexityResponse?.Results?.Select(r => new SearchResult
            {
                Title = r.Title ?? string.Empty,
                Url = r.Url ?? string.Empty,
                Snippet = r.Snippet ?? string.Empty,
                Source = "Perplexity"
            }).ToList() ?? new List<SearchResult>()
        };

        if (includeAiSummary)
        {
            searchResponse.AiSummary = await GenerateAiSummaryAsync(searchResponse.Results);
        }

        return searchResponse;
    }

    /// <summary>
    /// Searches using CrustData API.
    /// CrustData provides real-time data including web search.
    /// </summary>
    private async Task<SearchResponse> SearchWithCrustDataAsync(
        string query, string apiKey, int maxResults, bool includeAiSummary)
    {
        var client = _httpClientFactory.CreateClient();
        
        var request = new HttpRequestMessage(HttpMethod.Get, 
            $"https://api.crustdata.com/search?q={Uri.EscapeDataString(query)}&limit={maxResults}");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var crustDataResponse = JsonSerializer.Deserialize<CrustDataSearchResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var searchResponse = new SearchResponse
        {
            Query = query,
            TotalResults = crustDataResponse?.Results?.Count ?? 0,
            Results = crustDataResponse?.Results?.Select(r => new SearchResult
            {
                Title = r.Title ?? string.Empty,
                Url = r.Url ?? string.Empty,
                Snippet = r.Snippet ?? string.Empty,
                Source = "CrustData"
            }).ToList() ?? new List<SearchResult>()
        };

        if (includeAiSummary)
        {
            searchResponse.AiSummary = await GenerateAiSummaryAsync(searchResponse.Results);
        }

        return searchResponse;
    }

    /// <summary>
    /// Simulates an AI summary generation.
    /// In a production environment, this would call an LLM API like OpenAI, Anthropic, or Ollama.
    /// </summary>
    /// <param name="results">Search results to summarize.</param>
    /// <returns>AI-generated summary.</returns>
    private Task<string> GenerateAiSummaryAsync(List<SearchResult> results)
    {
        // This is a placeholder for AI summary generation
        // In production, integrate with OpenAI, Anthropic, or local LLM (Ollama)
        
        if (results == null || results.Count == 0)
        {
            return Task.FromResult("No results available to summarize.");
        }

        // Simulate AI processing delay
        var summary = $"[AI Agent Summary] Based on {results.Count} search results for the query. ";
        summary += "The top results provide information about the requested topic. ";
        summary += "For more detailed insights, please review the individual search results above. ";
        summary += "(This is a simulated summary - integrate with OpenAI/Anthropic for real AI-powered summaries)";

        return Task.FromResult(summary);
    }

    /// <summary>
    /// Returns mock search results for demonstration purposes.
    /// Used when no API key is configured or when the API fails.
    /// </summary>
    private Task<SearchResponse> GetMockSearchResultsAsync(
        string query, int maxResults, bool includeAiSummary)
    {
        _logger.LogInformation("Returning mock search results for query: {Query}", query);

        var mockResults = new List<SearchResult>
        {
            new SearchResult
            {
                Title = $"Result 1: {query} - Official Documentation",
                Url = "https://example.com/doc",
                Snippet = $"This is a comprehensive guide about {query}. It covers all the essential topics and best practices.",
                Source = "Mock Source",
                PublishedDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd")
            },
            new SearchResult
            {
                Title = $"Result 2: {query} - Tutorial",
                Url = "https://example.com/tutorial",
                Snippet = $"Learn {query} with this step-by-step tutorial designed for beginners.",
                Source = "Mock Source",
                PublishedDate = DateTime.UtcNow.AddDays(-14).ToString("yyyy-MM-dd")
            },
            new SearchResult
            {
                Title = $"Result 3: {query} - Community Forum",
                Url = "https://example.com/forum",
                Snippet = $"Join the community discussion about {query} and share your experiences.",
                Source = "Mock Source",
                PublishedDate = DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd")
            },
            new SearchResult
            {
                Title = $"Result 4: {query} - Latest News",
                Url = "https://example.com/news",
                Snippet = $"Stay updated with the latest news and developments in {query}.",
                Source = "Mock Source",
                PublishedDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
            },
            new SearchResult
            {
                Title = $"Result 5: {query} - GitHub Repository",
                Url = "https://github.com/example",
                Snippet = $"Open source implementation of {query} available on GitHub.",
                Source = "Mock Source",
                PublishedDate = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd")
            }
        };

        var response = new SearchResponse
        {
            Query = query,
            TotalResults = mockResults.Count,
            Results = mockResults.Take(maxResults).ToList()
        };

        if (includeAiSummary)
        {
            response.AiSummary = $"[AI Agent Demo Mode] Found {response.TotalResults} relevant results for '{query}'. " +
                "To get real-time search results, configure your API key in appsettings.json. " +
                "This is a placeholder summary - integrate with Tavily, Perplexity, or CrustData for actual search functionality.";
        }

        return Task.FromResult(response);
    }
}

// JSON response models for different API providers

/// <summary>
/// Tavily API response model.
/// </summary>
internal class TavilySearchResponse
{
    public string? Answer { get; set; }
    public List<TavilyResult>? Results { get; set; }
}

internal class TavilyResult
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Content { get; set; }
}

/// <summary>
/// Perplexity API response model.
/// </summary>
internal class PerplexitySearchResponse
{
    public List<PerplexityResult>? Results { get; set; }
}

internal class PerplexityResult
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Snippet { get; set; }
}

/// <summary>
/// CrustData API response model.
/// </summary>
internal class CrustDataSearchResponse
{
    public List<CrustDataResult>? Results { get; set; }
}

internal class CrustDataResult
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? Snippet { get; set; }
}
