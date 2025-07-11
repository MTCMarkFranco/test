using McpAzureDocsApi.Models;
using McpAzureDocsApi.Services;
using System.Text;
using System.Text.Json;

namespace McpAzureDocsApi.Services;

/// <summary>
/// Server-Sent Events service for streaming MCP responses
/// Provides real-time streaming of Azure documentation and analysis results
/// </summary>
public class ServerSentEventsService
{
    private readonly McpServerService _mcpServer;
    private readonly ILogger<ServerSentEventsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public ServerSentEventsService(McpServerService mcpServer, ILogger<ServerSentEventsService> logger)
    {
        _mcpServer = mcpServer;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    
    /// <summary>
    /// Handle SSE connection and stream MCP responses
    /// </summary>
    public async Task HandleSseConnectionAsync(HttpContext context)
    {
        var response = context.Response;
        
        // Set SSE headers
        response.Headers["Content-Type"] = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        response.Headers["Access-Control-Allow-Origin"] = "*";
        response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        
        var cancellationToken = context.RequestAborted;
        
        try
        {
            _logger.LogInformation("SSE connection established from {RemoteIP}", 
                context.Connection.RemoteIpAddress);
            
            // Send initial connection event
            await SendSseEventAsync(response, "connection", new { 
                status = "connected", 
                serverInfo = new {
                    name = "azure-docs-mcp-server",
                    version = "1.0.0",
                    capabilities = new[] { "tools", "search", "validation" }
                }
            });
            
            // Keep connection alive and handle incoming messages
            await HandleSseMessagesAsync(context, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSE connection cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE connection");
            await SendSseEventAsync(response, "error", new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Handle incoming MCP requests via POST to SSE endpoint
    /// </summary>
    public async Task<IResult> HandleMcpRequestAsync(HttpContext context)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                return Results.BadRequest("Request body is required");
            }
            
            var mcpRequest = JsonSerializer.Deserialize<McpRequest>(requestBody, _jsonOptions);
            if (mcpRequest == null)
            {
                return Results.BadRequest("Invalid MCP request format");
            }
            
            _logger.LogInformation("Processing MCP request: {Method}", mcpRequest.Method);
            
            var response = await _mcpServer.HandleRequestAsync(mcpRequest);
            
            return Results.Ok(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in MCP request");
            return Results.BadRequest("Invalid JSON format");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MCP request");
            return Results.StatusCode(500);
        }
    }
    
    /// <summary>
    /// Stream Azure documentation search results
    /// </summary>
    public async Task StreamDocumentationSearchAsync(HttpContext context, string query, string? service = null)
    {
        var response = context.Response;
        
        // Set SSE headers
        response.Headers["Content-Type"] = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        
        try
        {
            // Send search start event
            await SendSseEventAsync(response, "search_start", new { query, service });
            
            // Create search request
            var searchRequest = new McpRequest
            {
                Method = "tools/call",
                Id = Guid.NewGuid().ToString(),
                Params = new
                {
                    name = "search-azure-docs",
                    arguments = new { query, service }
                }
            };
            
            // Process the search
            var searchResponse = await _mcpServer.HandleRequestAsync(searchRequest);
            
            // Stream results
            await SendSseEventAsync(response, "search_result", searchResponse.Result ?? new { error = "No results" });
            
            // Send completion event
            await SendSseEventAsync(response, "search_complete", new { query });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming documentation search");
            await SendSseEventAsync(response, "error", new { message = ex.Message });
        }
    }
    
    /// <summary>
    /// Stream AKS design validation results
    /// </summary>
    public async Task StreamAksValidationAsync(HttpContext context, string designDescription)
    {
        var response = context.Response;
        
        // Set SSE headers
        response.Headers["Content-Type"] = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        
        try
        {
            // Send validation start event
            await SendSseEventAsync(response, "validation_start", new { designDescription });
            
            // Create validation request
            var validationRequest = new McpRequest
            {
                Method = "tools/call",
                Id = Guid.NewGuid().ToString(),
                Params = new
                {
                    name = "validate-aks-design",
                    arguments = new { design_description = designDescription }
                }
            };
            
            // Process the validation
            var validationResponse = await _mcpServer.HandleRequestAsync(validationRequest);
            
            // Stream results
            await SendSseEventAsync(response, "validation_result", validationResponse.Result ?? new { error = "No results" });
            
            // Send completion event
            await SendSseEventAsync(response, "validation_complete", new { designDescription });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming AKS validation");
            await SendSseEventAsync(response, "error", new { message = ex.Message });
        }
    }
    
    private async Task HandleSseMessagesAsync(HttpContext context, CancellationToken cancellationToken)
    {
        // Keep connection alive with periodic heartbeat
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(30000, cancellationToken); // 30 second heartbeat
                await SendSseEventAsync(context.Response, "heartbeat", new { timestamp = DateTime.UtcNow });
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
    
    private async Task SendSseEventAsync(HttpResponse response, string eventType, object data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var sseData = $"event: {eventType}\ndata: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(sseData);
            
            await response.Body.WriteAsync(bytes);
            await response.Body.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SSE event: {EventType}", eventType);
        }
    }
}
