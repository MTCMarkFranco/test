# McpAzureDocs.Package

A .NET library that provides Model Context Protocol (MCP) server capabilities for Microsoft Azure documentation and architecture guidance. This package allows you to embed Azure documentation MCP server functionality directly into your applications, similar to Context7.

## Features

- **Azure Documentation Search**: Search Microsoft Azure documentation for architecture patterns and best practices
- **AKS Design Validation**: Validate Azure Kubernetes Service designs against Microsoft recommended patterns
- **Azure Architecture Patterns**: Get specific Azure architecture patterns and reference architectures
- **MCP Protocol Compliance**: Full support for Model Context Protocol Server-Sent Events (SSE) streaming
- **Easy Integration**: Simple dependency injection setup for ASP.NET Core and .NET applications

## Installation

```bash
dotnet add package McpAzureDocs.Package
```

## Quick Start

### ASP.NET Core Integration

```csharp
using McpAzureDocs.Package;

var builder = WebApplication.CreateBuilder(args);

// Add MCP Azure Docs services
builder.Services.AddMcpAzureDocs();

var app = builder.Build();

// Configure MCP endpoints (optional - you can also use the client services directly)
app.UseMcpAzureDocs();

app.Run();
```

### Direct Service Usage

```csharp
using McpAzureDocs.Package;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddMcpAzureDocs();
var serviceProvider = services.BuildServiceProvider();

var azureDocsClient = serviceProvider.GetRequiredService<IAzureDocsClient>();

// Search Azure documentation
var searchResults = await azureDocsClient.SearchDocumentationAsync("microservices architecture patterns");

// Validate AKS design
var validationResults = await azureDocsClient.ValidateAksDesignAsync("My AKS cluster with node pools...");

// Get architecture patterns
var patterns = await azureDocsClient.GetArchitecturePatternsAsync("baseline", "aks");
```

### MCP Server Integration

```csharp
using McpAzureDocs.Package;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpAzureDocs();

var app = builder.Build();

// Add MCP SSE endpoint
app.MapGet("/mcp", async (HttpContext context, IMcpServerClient mcpClient) =>
{
    await mcpClient.HandleSseConnectionAsync(context);
});

// Add MCP request endpoint
app.MapPost("/mcp", async (HttpContext context, IMcpServerClient mcpClient) =>
{
    return await mcpClient.HandleMcpRequestAsync(context);
});

app.Run();
```

## Available Tools

The library provides three main MCP tools:

1. **search-azure-docs**: Search Microsoft Azure documentation
   - Parameters: `query` (string), `service` (optional string), `category` (optional string)

2. **validate-aks-design**: Validate AKS design against recommended patterns
   - Parameters: `design_description` (string)

3. **get-azure-patterns**: Get specific Azure architecture patterns
   - Parameters: `pattern_type` (string), `service` (optional string)

## Advanced Configuration

```csharp
builder.Services.AddMcpAzureDocs(options =>
{
    options.EnableCaching = true;
    options.CacheExpirationMinutes = 30;
    options.MaxSearchResults = 20;
    options.EnableDetailedLogging = true;
});
```

## MCP Protocol Support

This library fully supports the Model Context Protocol specification:
- JSON-RPC 2.0 message format
- Server-Sent Events (SSE) for streaming responses
- Tool discovery and execution
- Error handling and proper response codes

## Context7 Compatibility

This package follows the same patterns as Context7, making it easy to integrate into existing MCP-enabled applications:

```csharp
// Similar to Context7 usage
services.AddMcpServer("azure-docs", builder =>
{
    builder.AddMcpAzureDocs();
});
```

## License

MIT License - see LICENSE file for details.
