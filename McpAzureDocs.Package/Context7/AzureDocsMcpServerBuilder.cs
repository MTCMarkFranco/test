using McpAzureDocs.Package.Interfaces;
using McpAzureDocs.Package.Models;
using McpAzureDocs.Package.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpAzureDocs.Package.Context7;

/// <summary>
/// Context7-compatible MCP server builder for Azure documentation
/// Provides a familiar interface similar to Context7 for integrating MCP servers
/// </summary>
public class AzureDocsMcpServerBuilder
{
    private readonly IServiceCollection _services;
    private readonly string _serverName;
    private McpAzureDocsOptions _options = new();
    private Action<HttpClient>? _httpClientConfiguration;
    
    public AzureDocsMcpServerBuilder(IServiceCollection services, string serverName)
    {
        _services = services;
        _serverName = serverName;
    }
    
    /// <summary>
    /// Configure options for the Azure Docs MCP server
    /// </summary>
    public AzureDocsMcpServerBuilder WithOptions(Action<McpAzureDocsOptions> configure)
    {
        configure(_options);
        return this;
    }
    
    /// <summary>
    /// Configure the HTTP client used for external API calls
    /// </summary>
    public AzureDocsMcpServerBuilder WithHttpClient(Action<HttpClient> configure)
    {
        _httpClientConfiguration = configure;
        return this;
    }
    
    /// <summary>
    /// Enable detailed logging for debugging
    /// </summary>
    public AzureDocsMcpServerBuilder WithDetailedLogging(bool enabled = true)
    {
        _options.EnableDetailedLogging = enabled;
        return this;
    }
    
    /// <summary>
    /// Configure caching options
    /// </summary>
    public AzureDocsMcpServerBuilder WithCaching(bool enabled = true, int expirationMinutes = 30)
    {
        _options.EnableCaching = enabled;
        _options.CacheExpirationMinutes = expirationMinutes;
        return this;
    }
    
    /// <summary>
    /// Set maximum number of search results
    /// </summary>
    public AzureDocsMcpServerBuilder WithMaxResults(int maxResults)
    {
        _options.MaxSearchResults = maxResults;
        return this;
    }
    
    /// <summary>
    /// Build and register the MCP server services
    /// </summary>
    public IServiceCollection Build()
    {
        // Configure options
        _services.Configure<McpAzureDocsOptions>(opt =>
        {
            opt.EnableCaching = _options.EnableCaching;
            opt.CacheExpirationMinutes = _options.CacheExpirationMinutes;
            opt.MaxSearchResults = _options.MaxSearchResults;
            opt.EnableDetailedLogging = _options.EnableDetailedLogging;
            opt.UserAgent = _options.UserAgent;
        });
        
        // Add HTTP client
        if (_httpClientConfiguration != null)
        {
            _services.AddHttpClient<AzureDocumentationService>(_httpClientConfiguration);
        }
        else
        {
            _services.AddHttpClient<AzureDocumentationService>();
        }
        
        // Register services with named instances for Context7 compatibility
        _services.AddScoped<IAzureDocsClient>(provider => 
            provider.GetRequiredService<AzureDocumentationService>());
        
        _services.AddScoped<IMcpServerClient>(provider => 
            provider.GetRequiredService<McpServerService>());
        
        _services.AddScoped<IStreamingService>(provider => 
            provider.GetRequiredService<StreamingService>());
        
        // Register concrete implementations
        _services.AddScoped<AzureDocumentationService>();
        _services.AddScoped<McpServerService>();
        _services.AddScoped<StreamingService>();
        
        // Register named MCP server (Context7 style)
        _services.AddScoped<INamedMcpServer>(provider => 
            new NamedMcpServer(_serverName, provider.GetRequiredService<IMcpServerClient>()));
        
        return _services;
    }
}

/// <summary>
/// Named MCP server for Context7 compatibility
/// </summary>
public interface INamedMcpServer
{
    string Name { get; }
    IMcpServerClient Server { get; }
}

/// <summary>
/// Implementation of named MCP server
/// </summary>
public class NamedMcpServer : INamedMcpServer
{
    public string Name { get; }
    public IMcpServerClient Server { get; }
    
    public NamedMcpServer(string name, IMcpServerClient server)
    {
        Name = name;
        Server = server;
    }
}

/// <summary>
/// Context7-compatible extension methods for service collection
/// </summary>
public static class Context7Extensions
{
    /// <summary>
    /// Add an MCP server using Context7-style builder pattern
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serverName">Name of the MCP server</param>
    /// <param name="configure">Configuration action for the server builder</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMcpServer(
        this IServiceCollection services,
        string serverName,
        Action<AzureDocsMcpServerBuilder> configure)
    {
        var builder = new AzureDocsMcpServerBuilder(services, serverName);
        configure(builder);
        return builder.Build();
    }
    
    /// <summary>
    /// Add Azure Docs MCP server with Context7-compatible builder
    /// </summary>
    /// <param name="builder">The MCP server builder</param>
    /// <returns>The builder for chaining</returns>
    public static AzureDocsMcpServerBuilder AddMcpAzureDocs(this AzureDocsMcpServerBuilder builder)
    {
        return builder;
    }
}

/// <summary>
/// Context7-compatible MCP server registry
/// </summary>
public interface IMcpServerRegistry
{
    IEnumerable<INamedMcpServer> GetServers();
    INamedMcpServer? GetServer(string name);
    void RegisterServer(INamedMcpServer server);
}

/// <summary>
/// Implementation of MCP server registry
/// </summary>
public class McpServerRegistry : IMcpServerRegistry
{
    private readonly Dictionary<string, INamedMcpServer> _servers = new();
    
    public IEnumerable<INamedMcpServer> GetServers()
    {
        return _servers.Values;
    }
    
    public INamedMcpServer? GetServer(string name)
    {
        return _servers.TryGetValue(name, out var server) ? server : null;
    }
    
    public void RegisterServer(INamedMcpServer server)
    {
        _servers[server.Name] = server;
    }
}

/// <summary>
/// Extension methods for registering the MCP server registry
/// </summary>
public static class McpServerRegistryExtensions
{
    /// <summary>
    /// Add MCP server registry to the service collection
    /// </summary>
    public static IServiceCollection AddMcpServerRegistry(this IServiceCollection services)
    {
        services.AddSingleton<IMcpServerRegistry, McpServerRegistry>();
        return services;
    }
}
