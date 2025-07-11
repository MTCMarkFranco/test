using System.Text.Json.Serialization;

namespace McpAzureDocsApi.Models;

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
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; init; } = "2.0";
    
    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;
    
    [JsonPropertyName("id")]
    public object? Id { get; init; }
    
    [JsonPropertyName("params")]
    public object? Params { get; init; }
}

// MCP Response models
public record McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string Jsonrpc { get; init; } = "2.0";
    
    [JsonPropertyName("id")]
    public object? Id { get; init; }
    
    [JsonPropertyName("result")]
    public object? Result { get; init; }
    
    [JsonPropertyName("error")]
    public McpError? Error { get; init; }
}

public record McpError
{
    [JsonPropertyName("code")]
    public int Code { get; init; }
    
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
    
    [JsonPropertyName("data")]
    public object? Data { get; init; }
}

// Tool definition for MCP
public record McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
    
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; init; } = new { };
}

// Tool call request
public record McpToolCall
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public object? Arguments { get; init; }
}

// Tool call result
public record McpToolResult
{
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; init; } = new();
    
    [JsonPropertyName("isError")]
    public bool IsError { get; init; }
}

public record McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "text";
    
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

// Azure documentation specific models
public record AzureDocQuery
{
    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;
    
    [JsonPropertyName("service")]
    public string? Service { get; init; }
    
    [JsonPropertyName("category")]
    public string? Category { get; init; }
}

public record AzureDocResult
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;
    
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
    
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;
    
    [JsonPropertyName("relevanceScore")]
    public float RelevanceScore { get; init; }
}
