# McpAzureDocs.Client - Development Guide

## Overview

McpAzureDocs.Client is a .NET library that provides Model Context Protocol (MCP) server capabilities for Microsoft Azure documentation and architecture guidance. This package allows developers to embed Azure documentation MCP server functionality directly into their applications, similar to Context7.

## Architecture

### Core Components

1. **IAzureDocsClient** - Main interface for Azure documentation services
2. **IMcpServerClient** - Interface for MCP server functionality  
3. **IStreamingService** - Interface for Server-Sent Events streaming
4. **Context7 Compatibility** - Builder pattern for easy integration

### Service Implementations

- `AzureDocumentationService` - Core Azure documentation search and validation
- `McpServerService` - MCP protocol implementation with SSE support
- `StreamingService` - Real-time streaming of documentation results

## Project Structure

```
McpAzureDocs.Client/
├── Models/
│   └── McpModels.cs           # MCP protocol and Azure doc models
├── Interfaces/
│   └── IServices.cs           # Service interfaces
├── Services/
│   ├── AzureDocumentationService.cs  # Core documentation service
│   ├── McpServerService.cs           # MCP protocol implementation
│   └── StreamingService.cs           # SSE streaming service
├── Extensions/
│   └── ServiceCollectionExtensions.cs # DI extensions
├── Context7/
│   └── AzureDocsMcpServerBuilder.cs   # Context7-style builder
└── McpAzureDocsClient.cs      # Main entry point
```

## Usage Patterns

### 1. Simple Extension Method (Recommended)

```csharp
var services = new ServiceCollection();
services.AddMcpAzureDocs(options =>
{
    options.MaxSearchResults = 10;
    options.EnableCaching = true;
});

var serviceProvider = services.BuildServiceProvider();
var azureDocsClient = serviceProvider.GetRequiredService<IAzureDocsClient>();
```

### 2. Context7-Style Builder Pattern

```csharp
services.AddMcpServer("azure-docs", builder =>
{
    builder.AddMcpAzureDocs()
           .WithMaxResults(10)
           .WithCaching(true)
           .WithDetailedLogging(true);
});
```

### 3. ASP.NET Core Integration

```csharp
// In Program.cs
builder.Services.AddMcpAzureDocs();

// Custom endpoints
app.MapGet("/mcp", async (HttpContext context, IMcpServerClient mcpClient) =>
{
    await mcpClient.HandleSseConnectionAsync(context);
});
```

## Available Tools

The MCP server provides three main tools:

### 1. search-azure-docs
Search Microsoft Azure documentation for architecture patterns and best practices.

**Parameters:**
- `query` (string, required) - Search query
- `service` (string, optional) - Azure service name  
- `category` (string, optional) - Category filter

### 2. validate-aks-design
Validate AKS designs against Microsoft recommended patterns.

**Parameters:**
- `design_description` (string, required) - AKS design description

### 3. get-azure-patterns
Get specific Azure architecture patterns and reference architectures.

**Parameters:**
- `pattern_type` (string, required) - Pattern type (e.g., 'microservices', 'baseline')
- `service` (string, optional) - Azure service name

## Configuration Options

```csharp
public class McpAzureDocsOptions
{
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 30;
    public int MaxSearchResults { get; set; } = 10;
    public bool EnableDetailedLogging { get; set; } = false;
    public string UserAgent { get; set; } = "McpAzureDocs.Client/1.0.0";
}
```

## MCP Protocol Compliance

The library implements the Model Context Protocol specification:

- **JSON-RPC 2.0** message format
- **Server-Sent Events (SSE)** for streaming responses
- **Tool discovery** and execution
- **Error handling** with proper response codes
- **Heartbeat** mechanism for connection management

## Knowledge Base

The service includes a curated knowledge base with:

- Azure Well-Architected Framework guidance
- Service-specific best practices (AKS, App Service, Functions, Cosmos DB)
- Common anti-patterns and their solutions
- Architecture patterns and design guidance
- Security recommendations

## Testing and Examples

See the `McpAzureDocs.Client.Sample` project for comprehensive examples of all usage patterns.

## Building and Packaging

```bash
# Build the library
dotnet build McpAzureDocs.Client/McpAzureDocs.Client.csproj

# Create NuGet package
dotnet pack McpAzureDocs.Client/McpAzureDocs.Client.csproj

# Run sample application
dotnet run --project McpAzureDocs.Client.Sample/McpAzureDocs.Client.Sample.csproj
```

## Dependencies

- .NET 9.0
- Microsoft.AspNetCore.Http.Abstractions 2.3.0
- Microsoft.Extensions.* packages (9.0.0)
- System.Text.Json 9.0.0

## Extensibility

The library is designed for extensibility:

- Implement `IAzureDocsClient` for custom documentation sources
- Extend `AzureDocumentationService` to add new knowledge domains
- Add custom MCP tools by extending `McpServerService`
- Implement custom streaming patterns with `IStreamingService`

## Performance Considerations

- Knowledge base is loaded in memory for fast searches
- HTTP client is configured for connection pooling
- Streaming responses reduce memory footprint for large result sets
- Configurable caching reduces redundant operations
- Async/await patterns throughout for scalability
