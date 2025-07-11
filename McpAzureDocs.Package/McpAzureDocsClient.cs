// McpAzureDocs.Package - Main entry point
// This file serves as the main entry point for the library and provides convenience imports

using McpAzureDocs.Package.Extensions;
using McpAzureDocs.Package.Context7;

// Re-export key namespaces for easier consumption
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("McpAzureDocs.Package.Tests")]

namespace McpAzureDocs.Package;

/// <summary>
/// Main entry point for the McpAzureDocs.Package library
/// Provides convenient access to all library functionality
/// </summary>
public static class McpAzureDocsClient
{
    /// <summary>
    /// Library version
    /// </summary>
    public const string Version = "1.0.0";
    
    /// <summary>
    /// Library name
    /// </summary>
    public const string Name = "McpAzureDocs.Package";
    
    /// <summary>
    /// Default user agent string
    /// </summary>
    public const string DefaultUserAgent = $"{Name}/{Version}";
    
    /// <summary>
    /// Create a new Azure Docs MCP server builder for Context7-style configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serverName">Name of the MCP server</param>
    /// <returns>Azure Docs MCP server builder</returns>
    public static AzureDocsMcpServerBuilder CreateServer(IServiceCollection services, string serverName = "azure-docs")
    {
        return new AzureDocsMcpServerBuilder(services, serverName);
    }
}
