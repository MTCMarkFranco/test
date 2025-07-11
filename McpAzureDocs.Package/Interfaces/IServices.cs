using McpAzureDocs.Package.Models;
using Microsoft.AspNetCore.Http;

namespace McpAzureDocs.Package.Interfaces;

/// <summary>
/// Interface for Azure documentation client
/// </summary>
public interface IAzureDocsClient
{
    /// <summary>
    /// Search Azure documentation based on query
    /// </summary>
    Task<List<AzureDocResult>> SearchDocumentationAsync(string query, string? service = null, string? category = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate AKS design against Microsoft patterns
    /// </summary>
    Task<List<AzureDocResult>> ValidateAksDesignAsync(string designDescription, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get specific Azure architecture patterns
    /// </summary>
    Task<List<AzureDocResult>> GetArchitecturePatternsAsync(string patternType, string? service = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for MCP server client functionality
/// </summary>
public interface IMcpServerClient
{
    /// <summary>
    /// Handle MCP Server-Sent Events connection
    /// </summary>
    Task HandleSseConnectionAsync(HttpContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Handle MCP request and return response (for ASP.NET Core integration)
    /// </summary>
    Task<object> HandleMcpRequestAsync(HttpContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get available MCP tools
    /// </summary>
    List<McpTool> GetAvailableTools();
    
    /// <summary>
    /// Handle MCP request directly
    /// </summary>
    Task<McpResponse> ProcessMcpRequestAsync(McpRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for streaming documentation responses
/// </summary>
public interface IStreamingService
{
    /// <summary>
    /// Stream documentation search results
    /// </summary>
    Task StreamDocumentationSearchAsync(HttpContext context, string query, string? service = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stream AKS validation results
    /// </summary>
    Task StreamAksValidationAsync(HttpContext context, string designDescription, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stream architecture patterns
    /// </summary>
    Task StreamArchitecturePatternsAsync(HttpContext context, string patternType, string? service = null, CancellationToken cancellationToken = default);
}
