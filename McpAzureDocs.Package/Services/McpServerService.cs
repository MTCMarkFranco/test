using McpAzureDocs.Package.Models;
using McpAzureDocs.Package.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace McpAzureDocs.Package.Services;

/// <summary>
/// MCP (Model Context Protocol) server implementation for Azure documentation
/// Handles MCP requests and provides Azure architecture guidance
/// </summary>
public class McpServerService : IMcpServerClient
{
    private readonly IAzureDocsClient _azureDocsClient;
    private readonly ILogger<McpServerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public McpServerService(IAzureDocsClient azureDocsClient, ILogger<McpServerService> logger)
    {
        _azureDocsClient = azureDocsClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
    
    /// <summary>
    /// Handle MCP Server-Sent Events connection
    /// </summary>
    public async Task HandleSseConnectionAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        context.Response.Headers.Add("Content-Type", "text/event-stream");
        context.Response.Headers.Add("Cache-Control", "no-cache");
        context.Response.Headers.Add("Connection", "keep-alive");
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        
        try
        {
            // Send initial connection message
            await SendSseMessageAsync(context, "connected", new { 
                message = "MCP Azure Docs server connected",
                server = "azure-docs-mcp-server",
                version = "1.0.0",
                timestamp = DateTime.UtcNow
            }, cancellationToken);
            
            // Send available tools
            var tools = GetAvailableTools();
            await SendSseMessageAsync(context, "tools", new { tools }, cancellationToken);
            
            // Keep connection alive
            while (!cancellationToken.IsCancellationRequested)
            {
                await SendSseMessageAsync(context, "heartbeat", new { 
                    timestamp = DateTime.UtcNow 
                }, cancellationToken);
                
                await Task.Delay(30000, cancellationToken); // Send heartbeat every 30 seconds
            }
        }
        catch (OperationCanceledException)
        {
            // Connection closed by client
            _logger.LogInformation("SSE connection closed by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SSE connection");
            await SendSseMessageAsync(context, "error", new { 
                message = "Server error occurred" 
            }, CancellationToken.None);
        }
    }
    
    /// <summary>
    /// Handle MCP request and return response
    /// </summary>
    public async Task<object> HandleMcpRequestAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);
            
            var request = JsonSerializer.Deserialize<McpRequest>(requestBody, _jsonOptions);
            if (request == null)
            {
                return CreateErrorResponse(null, -32700, "Parse error");
            }
            
            var response = await ProcessMcpRequestAsync(request, cancellationToken);
            return response;
        }
        catch (JsonException)
        {
            return CreateErrorResponse(null, -32700, "Parse error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MCP request");
            return CreateErrorResponse(null, -32603, "Internal error");
        }
    }
    
    /// <summary>
    /// Handle MCP request directly
    /// </summary>
    public async Task<McpResponse> ProcessMcpRequestAsync(McpRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Handling MCP request: {Method}", request.Method);
            
            return request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolCallAsync(request, cancellationToken),
                _ => CreateErrorResponse(request.Id, -32601, $"Method not found: {request.Method}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MCP request: {Method}", request.Method);
            return CreateErrorResponse(request.Id, -32603, "Internal error");
        }
    }
    
    /// <summary>
    /// Get available MCP tools
    /// </summary>
    public List<McpTool> GetAvailableTools()
    {
        return new List<McpTool>
        {
            new()
            {
                Name = "search-azure-docs",
                Description = "Search Microsoft Azure documentation for architecture patterns, best practices, and guidance",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Search query for Azure documentation" },
                        service = new { type = "string", description = "Optional Azure service name (e.g., 'aks', 'app-service', 'functions')" },
                        category = new { type = "string", description = "Optional category filter (e.g., 'best-practices', 'security', 'networking')" }
                    },
                    required = new[] { "query" }
                }
            },
            new()
            {
                Name = "validate-aks-design",
                Description = "Validate Azure Kubernetes Service design against Microsoft recommended patterns and identify anti-patterns",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        design_description = new { type = "string", description = "Description of the AKS cluster design and architecture" }
                    },
                    required = new[] { "design_description" }
                }
            },
            new()
            {
                Name = "get-azure-patterns",
                Description = "Get specific Azure architecture patterns and reference architectures",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        pattern_type = new { type = "string", description = "Type of pattern (e.g., 'microservices', 'baseline', 'enterprise')" },
                        service = new { type = "string", description = "Optional Azure service name" }
                    },
                    required = new[] { "pattern_type" }
                }
            }
        };
    }
    
    private McpResponse HandleInitialize(McpRequest request)
    {
        return new McpResponse
        {
            Id = request.Id,
            Result = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { },
                    logging = new { }
                },
                serverInfo = new
                {
                    name = "azure-docs-mcp-server",
                    version = "1.0.0",
                    description = "Model Context Protocol server for Microsoft Azure architecture documentation and best practices"
                }
            }
        };
    }
    
    private McpResponse HandleToolsList(McpRequest request)
    {
        var tools = GetAvailableTools();
        return new McpResponse
        {
            Id = request.Id,
            Result = new { tools }
        };
    }
    
    private async Task<McpResponse> HandleToolCallAsync(McpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var paramsElement = (JsonElement)request.Params!;
            var toolCall = JsonSerializer.Deserialize<McpToolCall>(paramsElement.GetRawText(), _jsonOptions);
            
            if (toolCall == null)
            {
                return CreateErrorResponse(request.Id, -32602, "Invalid tool call parameters");
            }
            
            var result = await ExecuteToolAsync(toolCall, cancellationToken);
            
            return new McpResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool call");
            return CreateErrorResponse(request.Id, -32603, "Tool execution failed");
        }
    }
    
    private async Task<McpToolResult> ExecuteToolAsync(McpToolCall toolCall, CancellationToken cancellationToken)
    {
        try
        {
            var argumentsJson = JsonSerializer.Serialize(toolCall.Arguments);
            var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson, _jsonOptions) 
                           ?? new Dictionary<string, object>();
            
            return toolCall.Name switch
            {
                "search-azure-docs" => await ExecuteSearchDocsAsync(arguments, cancellationToken),
                "validate-aks-design" => await ExecuteValidateAksAsync(arguments, cancellationToken),
                "get-azure-patterns" => await ExecuteGetPatternsAsync(arguments, cancellationToken),
                _ => new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new() { Type = "text", Text = $"Unknown tool: {toolCall.Name}" }
                    },
                    IsError = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolCall.Name);
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = $"Error executing tool {toolCall.Name}: {ex.Message}" }
                },
                IsError = true
            };
        }
    }
    
    private async Task<McpToolResult> ExecuteSearchDocsAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var query = GetStringArgument(arguments, "query") ?? "";
        var service = GetStringArgument(arguments, "service");
        var category = GetStringArgument(arguments, "category");
        
        if (string.IsNullOrEmpty(query))
        {
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = "Query parameter is required" }
                },
                IsError = true
            };
        }
        
        var results = await _azureDocsClient.SearchDocumentationAsync(query, service, category, cancellationToken);
        
        var content = new List<McpContent>();
        foreach (var result in results)
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = $"**{result.Title}** (Score: {result.RelevanceScore:F2})\n" +
                       $"Category: {result.Category}\n" +
                       $"Content: {result.Content}\n" +
                       $"URL: {result.Url}\n\n"
            });
        }
        
        if (content.Count == 0)
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = "No documentation found for the specified query."
            });
        }
        
        return new McpToolResult { Content = content, IsError = false };
    }
    
    private async Task<McpToolResult> ExecuteValidateAksAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var designDescription = GetStringArgument(arguments, "design_description") ?? "";
        
        if (string.IsNullOrEmpty(designDescription))
        {
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = "design_description parameter is required" }
                },
                IsError = true
            };
        }
        
        var results = await _azureDocsClient.ValidateAksDesignAsync(designDescription, cancellationToken);
        
        var content = new List<McpContent>();
        foreach (var result in results)
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = $"{result.Title}\n" +
                       $"Category: {result.Category}\n" +
                       $"Guidance: {result.Content}\n" +
                       $"Reference: {result.Url}\n\n"
            });
        }
        
        if (content.Count == 0)
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = "No validation issues found. Your AKS design appears to follow best practices."
            });
        }
        
        return new McpToolResult { Content = content, IsError = false };
    }
    
    private async Task<McpToolResult> ExecuteGetPatternsAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var patternType = GetStringArgument(arguments, "pattern_type") ?? "";
        var service = GetStringArgument(arguments, "service");
        
        if (string.IsNullOrEmpty(patternType))
        {
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = "pattern_type parameter is required" }
                },
                IsError = true
            };
        }
        
        var results = await _azureDocsClient.GetArchitecturePatternsAsync(patternType, service, cancellationToken);
        
        var content = new List<McpContent>();
        foreach (var result in results)
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = $"**{result.Title}**\n" +
                       $"Category: {result.Category}\n" +
                       $"Description: {result.Content}\n" +
                       $"Documentation: {result.Url}\n\n"
            });
        }
        
        if (content.Count == 0)
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = $"No architecture patterns found for type: {patternType}"
            });
        }
        
        return new McpToolResult { Content = content, IsError = false };
    }
    
    private string? GetStringArgument(Dictionary<string, object> arguments, string key)
    {
        if (arguments.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }
    
    private McpResponse CreateErrorResponse(object? id, int code, string message)
    {
        return new McpResponse
        {
            Id = id,
            Error = new McpError
            {
                Code = code,
                Message = message
            }
        };
    }
    
    private async Task SendSseMessageAsync(HttpContext context, string eventType, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var sseMessage = $"event: {eventType}\ndata: {json}\n\n";
        
        await context.Response.WriteAsync(sseMessage, cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
}
