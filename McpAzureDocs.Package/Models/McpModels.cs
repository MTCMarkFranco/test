using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace McpAzureDocs.Package.Models;

/// <summary>
/// MCP (Model Context Protocol) request and response models
/// Based on the MCP specification for proper client-server communication
/// </summary>

// Base message interface for all MCP messages
public interface IMcpMessage
{
    string Jsonrpc { get; }
    string Method { get; }
}

// MCP Request models
public record McpRequest : IMcpMessage
{
    public string Jsonrpc { get; init; } = "2.0";
    public string Method { get; init; } = string.Empty;
    public object? Id { get; init; }
    public object? Params { get; init; }
}

// MCP Response models
public record McpResponse
{
    public string Jsonrpc { get; init; } = "2.0";
    public object? Id { get; init; }
    public object? Result { get; init; }
    public McpError? Error { get; init; }
}

public record McpError
{
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
}

// Tool definition for MCP
public record McpTool
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public object InputSchema { get; init; } = new { };
}

// Tool call request
public record McpToolCall
{
    public string Name { get; init; } = string.Empty;
    public object? Arguments { get; init; }
}

// Tool call result
public record McpToolResult
{
    public List<McpContent> Content { get; init; } = new();
    public bool IsError { get; init; }
}

public record McpContent
{
    public string Type { get; init; } = "text";
    public string Text { get; init; } = string.Empty;
}

// Azure documentation specific models
public record AzureDocQuery
{
    public string Query { get; init; } = string.Empty;
    public string? Service { get; init; }
    public string? Category { get; init; }
}

public record AzureDocResult
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public float RelevanceScore { get; init; }
}

// Configuration models
public class McpAzureDocsOptions
{
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 30;
    public int MaxSearchResults { get; set; } = 10;
    public bool EnableDetailedLogging { get; set; } = false;
    public string UserAgent { get; set; } = "McpAzureDocs.Package/1.0.0";
}
