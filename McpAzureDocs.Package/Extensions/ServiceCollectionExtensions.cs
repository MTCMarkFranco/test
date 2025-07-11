using McpAzureDocs.Package.Interfaces;
using McpAzureDocs.Package.Models;
using McpAzureDocs.Package.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpAzureDocs.Package.Extensions;

/// <summary>
/// Extension methods for configuring MCP Azure Docs services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add MCP Azure Docs services to the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration for MCP Azure Docs options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMcpAzureDocs(
        this IServiceCollection services, 
        Action<McpAzureDocsOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<McpAzureDocsOptions>(options => { });
        }
        
        // Add HTTP client for external calls
        services.AddHttpClient<AzureDocumentationService>();
        
        // Register core services
        services.AddScoped<IAzureDocsClient, AzureDocumentationService>();
        services.AddScoped<IMcpServerClient, McpServerService>();
        services.AddScoped<IStreamingService, StreamingService>();
        
        // Register concrete implementations for direct access if needed
        services.AddScoped<AzureDocumentationService>();
        services.AddScoped<McpServerService>();
        services.AddScoped<StreamingService>();
        
        return services;
    }
    
    /// <summary>
    /// Add MCP Azure Docs services with a specific HttpClient configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureHttpClient">HttpClient configuration</param>
    /// <param name="configureOptions">Optional configuration for MCP Azure Docs options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMcpAzureDocs(
        this IServiceCollection services,
        Action<HttpClient> configureHttpClient,
        Action<McpAzureDocsOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<McpAzureDocsOptions>(options => { });
        }
        
        // Add HTTP client with custom configuration
        services.AddHttpClient<AzureDocumentationService>(configureHttpClient);
        
        // Register core services
        services.AddScoped<IAzureDocsClient, AzureDocumentationService>();
        services.AddScoped<IMcpServerClient, McpServerService>();
        services.AddScoped<IStreamingService, StreamingService>();
        
        // Register concrete implementations for direct access if needed
        services.AddScoped<AzureDocumentationService>();
        services.AddScoped<McpServerService>();
        services.AddScoped<StreamingService>();
        
        return services;
    }
}

// Note: For ASP.NET Core WebApplication integration, see the separate 
// McpAzureDocs.Package.AspNetCore package or use the services directly
