using McpAzureDocsApi.Models;
using McpAzureDocsApi.Services;
using System.Text.Json;

namespace McpAzureDocsApi.Services;

/// <summary>
/// MCP (Model Context Protocol) server implementation for Azure documentation
/// Handles MCP requests and provides Azure architecture guidance
/// </summary>
public class McpServerService
{
    private readonly AzureDocumentationService _azureDocsService;
    private readonly ILogger<McpServerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public McpServerService(AzureDocumentationService azureDocsService, ILogger<McpServerService> logger)
    {
        _azureDocsService = azureDocsService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
    
    /// <summary>
    /// Handle MCP request and return appropriate response
    /// </summary>
    public async Task<McpResponse> HandleRequestAsync(McpRequest request)
    {
        try
        {
            _logger.LogInformation("Handling MCP request: {Method}", request.Method);
            
            return request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolCallAsync(request),
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
                        service = new { type = "string", description = "Specific Azure service (e.g., 'aks', 'app-service', 'functions')" },
                        category = new { type = "string", description = "Category filter (e.g., 'best-practices', 'security', 'networking')" }
                    },
                    required = new[] { "query" }
                }
            },
            new()
            {
                Name = "validate-aks-design",
                Description = "Validate AKS (Azure Kubernetes Service) design against Microsoft recommended patterns and identify anti-patterns",
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
                        service = new { type = "string", description = "Azure service name" }
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
                    description = "MCP server for Microsoft Azure architecture documentation and best practices"
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
    
    private async Task<McpResponse> HandleToolCallAsync(McpRequest request)
    {
        if (request.Params is not JsonElement paramsElement)
        {
            return CreateErrorResponse(request.Id, -32602, "Invalid params");
        }
        
        if (!paramsElement.TryGetProperty("name", out var nameElement))
        {
            return CreateErrorResponse(request.Id, -32602, "Missing tool name");
        }
        
        var toolName = nameElement.GetString();
        if (string.IsNullOrEmpty(toolName))
        {
            return CreateErrorResponse(request.Id, -32602, "Invalid tool name");
        }
        
        var arguments = paramsElement.TryGetProperty("arguments", out var argsElement) ? argsElement : default;
        
        var result = await ExecuteToolAsync(toolName, arguments);
        
        return new McpResponse
        {
            Id = request.Id,
            Result = result
        };
    }
    
    private async Task<McpToolResult> ExecuteToolAsync(string toolName, JsonElement arguments)
    {
        try
        {
            return toolName switch
            {
                "search-azure-docs" => await SearchAzureDocsAsync(arguments),
                "validate-aks-design" => await ValidateAksDesignAsync(arguments),
                "get-azure-patterns" => await GetAzurePatternsAsync(arguments),
                _ => new McpToolResult 
                { 
                    IsError = true, 
                    Content = new List<McpContent> 
                    { 
                        new() { Type = "text", Text = $"Unknown tool: {toolName}" } 
                    } 
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolName);
            return new McpToolResult
            {
                IsError = true,
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = $"Error executing tool {toolName}: {ex.Message}" }
                }
            };
        }
    }
    
    private async Task<McpToolResult> SearchAzureDocsAsync(JsonElement arguments)
    {
        var query = ExtractStringProperty(arguments, "query");
        var service = ExtractStringProperty(arguments, "service");
        var category = ExtractStringProperty(arguments, "category");
        
        if (string.IsNullOrEmpty(query))
        {
            return new McpToolResult
            {
                IsError = true,
                Content = new List<McpContent> { new() { Type = "text", Text = "Query parameter is required" } }
            };
        }
        
        var searchQuery = new AzureDocQuery
        {
            Query = query,
            Service = service,
            Category = category
        };
        
        var results = await _azureDocsService.SearchDocumentationAsync(searchQuery);
        
        var content = new List<McpContent>();
        
        if (results.Any())
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = $"Found {results.Count} Azure documentation results for: {query}\n\n"
            });
            
            foreach (var result in results)
            {
                content.Add(new McpContent
                {
                    Type = "text",
                    Text = $"## {result.Title}\n" +
                           $"**Category:** {result.Category}\n" +
                           $"**Relevance:** {result.RelevanceScore:P0}\n" +
                           $"**URL:** {result.Url}\n\n" +
                           $"{result.Content}\n\n---\n\n"
                });
            }
        }
        else
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = $"No Azure documentation found for query: {query}"
            });
        }
        
        return new McpToolResult { Content = content };
    }
    
    private async Task<McpToolResult> ValidateAksDesignAsync(JsonElement arguments)
    {
        var designDescription = ExtractStringProperty(arguments, "design_description");
        
        if (string.IsNullOrEmpty(designDescription))
        {
            return new McpToolResult
            {
                IsError = true,
                Content = new List<McpContent> { new() { Type = "text", Text = "design_description parameter is required" } }
            };
        }
        
        var results = await _azureDocsService.ValidateAksDesignAsync(designDescription);
        
        var content = new List<McpContent>
        {
            new()
            {
                Type = "text",
                Text = $"# AKS Design Validation Results\n\n" +
                       $"**Design Description:** {designDescription}\n\n" +
                       $"**Analysis Results:**\n\n"
            }
        };
        
        var antiPatterns = results.Where(r => r.Category.Contains("Anti-Pattern")).ToList();
        var bestPractices = results.Where(r => r.Category.Contains("Best Practice")).ToList();
        var patterns = results.Where(r => r.Category.Contains("Pattern") && !r.Category.Contains("Anti-Pattern")).ToList();
        
        if (antiPatterns.Any())
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = "## ⚠️ Anti-Patterns Detected\n\n"
            });
            
            foreach (var antiPattern in antiPatterns)
            {
                content.Add(new McpContent
                {
                    Type = "text",
                    Text = $"### {antiPattern.Title}\n{antiPattern.Content}\n\n**Reference:** {antiPattern.Url}\n\n"
                });
            }
        }
        
        if (bestPractices.Any())
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = "## ✅ Recommended Best Practices\n\n"
            });
            
            foreach (var practice in bestPractices)
            {
                content.Add(new McpContent
                {
                    Type = "text",
                    Text = $"### {practice.Title}\n{practice.Content}\n\n**Reference:** {practice.Url}\n\n"
                });
            }
        }
        
        if (patterns.Any())
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = "## 📋 Architecture Patterns\n\n"
            });
            
            foreach (var pattern in patterns)
            {
                content.Add(new McpContent
                {
                    Type = "text",
                    Text = $"### {pattern.Title}\n{pattern.Content}\n\n**Reference:** {pattern.Url}\n\n"
                });
            }
        }
        
        return new McpToolResult { Content = content };
    }
    
    private async Task<McpToolResult> GetAzurePatternsAsync(JsonElement arguments)
    {
        var patternType = ExtractStringProperty(arguments, "pattern_type");
        var service = ExtractStringProperty(arguments, "service");
        
        if (string.IsNullOrEmpty(patternType))
        {
            return new McpToolResult
            {
                IsError = true,
                Content = new List<McpContent> { new() { Type = "text", Text = "pattern_type parameter is required" } }
            };
        }
        
        // Create a search query based on pattern type and service
        var searchQuery = new AzureDocQuery
        {
            Query = $"{patternType} pattern {service}",
            Service = service,
            Category = "Architecture Pattern"
        };
        
        var results = await _azureDocsService.SearchDocumentationAsync(searchQuery);
        
        var content = new List<McpContent>
        {
            new()
            {
                Type = "text",
                Text = $"# Azure {patternType} Patterns\n\n"
            }
        };
        
        if (results.Any())
        {
            foreach (var result in results)
            {
                content.Add(new McpContent
                {
                    Type = "text",
                    Text = $"## {result.Title}\n" +
                           $"**Category:** {result.Category}\n" +
                           $"**URL:** {result.Url}\n\n" +
                           $"{result.Content}\n\n---\n\n"
                });
            }
        }
        else
        {
            content.Add(new McpContent
            {
                Type = "text",
                Text = $"No patterns found for: {patternType} {service}"
            });
        }
        
        return new McpToolResult { Content = content };
    }
    
    private string? ExtractStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
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
}
