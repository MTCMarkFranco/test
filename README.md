# Azure Documentation MCP Server

A .NET 9.0 Web API that implements a Model Context Protocol (MCP) Server-Sent Events endpoint for serving Microsoft Azure architecture documentation, best practices, and design validation.

## Overview

This server provides AI agents and LLMs with access to curated Microsoft Azure documentation, architecture patterns, and best practices through the Model Context Protocol (MCP). It specifically focuses on helping validate Azure Kubernetes Service (AKS) designs against Microsoft recommended patterns and identifying anti-patterns.

## Features

- **MCP Compliant**: Full support for Model Context Protocol with JSON-RPC 2.0
- **Server-Sent Events**: Real-time streaming of documentation and analysis results
- **Azure Focus**: Curated Azure documentation and best practices
- **AKS Validation**: Specialized tools for validating AKS designs
- **Architecture Patterns**: Access to Microsoft recommended architecture patterns
- **RESTful API**: Standard HTTP endpoints for integration

## Available MCP Tools

### 1. search-azure-docs
Search Microsoft Azure documentation for architecture patterns, best practices, and guidance.

**Parameters:**
- `query` (required): Search query for Azure documentation
- `service` (optional): Specific Azure service (e.g., 'aks', 'app-service', 'functions')
- `category` (optional): Category filter (e.g., 'best-practices', 'security', 'networking')

### 2. validate-aks-design
Validate AKS (Azure Kubernetes Service) design against Microsoft recommended patterns and identify anti-patterns.

**Parameters:**
- `design_description` (required): Description of the AKS cluster design and architecture

### 3. get-azure-patterns
Get specific Azure architecture patterns and reference architectures.

**Parameters:**
- `pattern_type` (required): Type of pattern (e.g., 'microservices', 'baseline', 'enterprise')
- `service` (optional): Azure service name

## API Endpoints

### MCP Endpoints
- `GET /mcp` - Server-Sent Events endpoint for MCP communication
- `POST /mcp` - HTTP POST endpoint for MCP requests

### Streaming Endpoints
- `GET /mcp/search?query={query}&service={service}` - Stream documentation search results
- `GET /mcp/validate-aks?design={design}` - Stream AKS design validation results

### Utility Endpoints
- `GET /` - Server information and available endpoints
- `GET /mcp/tools` - List available MCP tools
- `GET /health` - Health check endpoint

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Visual Studio Code or Visual Studio 2022

### Installation
1. Clone or download the project
2. Restore dependencies:
   ```bash
   dotnet restore
   ```

### Running the Server
1. Start the development server:
   ```bash
   dotnet run
   ```
2. The server will start on `https://localhost:5001` (or the configured port)
3. Access the root endpoint to see available endpoints and documentation

### Testing with MCP Clients

#### Using Context7 MCP Client
Add to your MCP client configuration:
```json
{
  "mcpServers": {
    "azure-docs": {
      "url": "https://localhost:5001/mcp"
    }
  }
}
```

#### Manual Testing
You can test the endpoints directly:

1. **Health Check:**
   ```bash
   curl https://localhost:5001/health
   ```

2. **Available Tools:**
   ```bash
   curl https://localhost:5001/mcp/tools
   ```

3. **Search Documentation:**
   ```bash
   curl "https://localhost:5001/mcp/search?query=AKS%20best%20practices&service=aks"
   ```

4. **MCP Request:**
   ```bash
   curl -X POST https://localhost:5001/mcp \
     -H "Content-Type: application/json" \
     -d '{
       "jsonrpc": "2.0",
       "method": "tools/call",
       "id": 1,
       "params": {
         "name": "search-azure-docs",
         "arguments": {
           "query": "AKS security best practices"
         }
       }
     }'
   ```

## Architecture

### Project Structure
```
├── Models/
│   └── McpModels.cs          # MCP protocol models and Azure doc models
├── Services/
│   ├── AzureDocumentationService.cs  # Azure docs search and curation
│   ├── McpServerService.cs           # MCP protocol implementation
│   └── ServerSentEventsService.cs    # SSE streaming implementation
├── Program.cs                # Application setup and endpoint configuration
└── .github/
    └── copilot-instructions.md       # Copilot development guidance
```

### Key Components

1. **AzureDocumentationService**: Manages the curated Azure knowledge base and provides search functionality
2. **McpServerService**: Implements the MCP protocol for handling requests and managing tools
3. **ServerSentEventsService**: Provides SSE streaming for real-time communication

## Azure Knowledge Base

The server includes a curated knowledge base covering:
- **AKS (Azure Kubernetes Service)**: Baseline architecture, best practices, security, networking
- **App Service**: Deployment best practices, scaling guidance
- **Azure Functions**: Performance optimization, error handling
- **Container Apps**: Modern application patterns, Dapr integration

## Use Cases

### 1. AKS Design Validation
```
User: "I want to validate my AKS design against Microsoft patterns and avoid anti-patterns"
Tool: validate-aks-design
Input: Description of AKS cluster design
Output: Analysis of patterns, anti-patterns, and recommendations
```

### 2. Architecture Documentation Search
```
User: "Find Azure microservices patterns for container applications"
Tool: search-azure-docs  
Input: Query about microservices and containers
Output: Relevant Azure documentation and patterns
```

### 3. Best Practices Guidance
```
User: "What are the security best practices for AKS?"
Tool: search-azure-docs
Input: Security-focused query for AKS
Output: Curated security guidance and references
```

## Development

### Adding New Azure Services
1. Extend the knowledge base in `AzureDocumentationService.InitializeKnowledgeBase()`
2. Add service-specific guidance methods
3. Update search and validation logic as needed

### Adding New MCP Tools
1. Define the tool schema in `McpServerService.GetAvailableTools()`
2. Implement the tool execution logic in `ExecuteToolAsync()`
3. Add corresponding service methods if needed

## Configuration

The server uses standard .NET configuration:
- `appsettings.json` for general settings
- `appsettings.Development.json` for development overrides
- Environment variables for production configuration

## Contributing

1. Follow the existing code style and patterns
2. Add comprehensive logging for new features
3. Update the knowledge base with verified Microsoft documentation
4. Test MCP protocol compliance with standard MCP clients

## References

- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [Azure Architecture Center](https://learn.microsoft.com/en-us/azure/architecture/)
- [Azure Well-Architected Framework](https://learn.microsoft.com/en-us/azure/well-architected/)
- [AKS Baseline Architecture](https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/containers/aks/baseline-aks)

## License

This project is for educational and development purposes. Azure documentation content is subject to Microsoft's terms of use.
