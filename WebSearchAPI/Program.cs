using WebSearchAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// ============================================================================
// DEPENDENCY INJECTION SETUP
// ============================================================================

// Register the WebSearchService as a scoped service
// This allows the service to be injected into controllers
builder.Services.AddScoped<IWebSearchService, WebSearchService>();

// Add HttpClientFactory for making HTTP requests to external APIs
// This is used by WebSearchService to call Tavily, Perplexity, or CrustData
builder.Services.AddHttpClient();

// Add controller services
builder.Services.AddControllers();

// Add Swagger/OpenAPI for API testing and documentation
// This provides an interactive UI at /swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Web Search API",
        Version = "v1",
        Description = "A .NET Core Web API that integrates with Web Search APIs (Tavily, Perplexity, CrustData) " +
                      "to provide AI-powered search functionality. Includes optional AI summary generation."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger in development mode
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Web Search API v1");
        c.RoutePrefix = "swagger"; // Access Swagger UI at /swagger
    });
}

// Enable CORS for frontend integration
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

// Map API routes
app.MapControllers();

// ============================================================================
// HEALTH CHECK ENDPOINT
// ============================================================================
// Simple health check at root URL
app.MapGet("/", () => new
{
    service = "WebSearchAPI",
    version = "1.0.0",
    status = "running",
    endpoints = new[]
    {
        "POST /api/search - Search with JSON body",
        "GET /api/search?query=your query - Simple GET search",
        "GET /api/search/health - Health check",
        "GET /swagger - API documentation"
    },
    timestamp = DateTime.UtcNow
});

Console.WriteLine("===========================================");
Console.WriteLine("  WebSearchAPI - .NET Core 8 Web API");
Console.WriteLine("===========================================");
Console.WriteLine("  Endpoints:");
Console.WriteLine("  - POST /api/search");
Console.WriteLine("  - GET  /api/search?query=...");
Console.WriteLine("  - GET  /api/search/health");
Console.WriteLine("  - GET  /swagger");
Console.WriteLine("===========================================");
Console.WriteLine();

app.Run();
