using McpAzureDocsApi.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

// Add HTTP client for external API calls
builder.Services.AddHttpClient<AzureDocumentationService>();

// Register our services
builder.Services.AddScoped<AzureDocumentationService>();
builder.Services.AddScoped<McpServerService>();
builder.Services.AddScoped<ServerSentEventsService>();

// Add CORS for cross-origin requests
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure JSON options
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// MCP SSE endpoint - primary route
app.MapGet("/mcp", async (HttpContext context, ServerSentEventsService sseService) =>
{
    await sseService.HandleSseConnectionAsync(context);
});

// MCP request endpoint for POST requests
app.MapPost("/mcp", async (HttpContext context, ServerSentEventsService sseService) =>
{
    return await sseService.HandleMcpRequestAsync(context);
});

// Streaming endpoints for specific operations
app.MapGet("/mcp/search", async (HttpContext context, ServerSentEventsService sseService, string query, string? service) =>
{
    await sseService.StreamDocumentationSearchAsync(context, query, service);
});

app.MapGet("/mcp/validate-aks", async (HttpContext context, ServerSentEventsService sseService, string design) =>
{
    await sseService.StreamAksValidationAsync(context, design);
});

// Health check endpoint
app.MapGet("/health", () => new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    server = "azure-docs-mcp-server",
    version = "1.0.0"
});

// Available tools endpoint
app.MapGet("/mcp/tools", (McpServerService mcpServer) =>
{
    var tools = mcpServer.GetAvailableTools();
    return Results.Ok(new { tools });
});

// Server info endpoint
app.MapGet("/", () => new {
    name = "Azure Documentation MCP Server",
    version = "1.0.0",
    description = "Model Context Protocol server for Microsoft Azure architecture documentation and best practices",
    endpoints = new {
        mcp_sse = "/mcp (GET for SSE, POST for requests)",
        search = "/mcp/search?query={query}&service={service}",
        validate_aks = "/mcp/validate-aks?design={design}",
        tools = "/mcp/tools",
        health = "/health"
    },
    documentation = "https://modelcontextprotocol.io/",
    azure_docs = "https://learn.microsoft.com/en-us/azure/"
});

app.Run();
