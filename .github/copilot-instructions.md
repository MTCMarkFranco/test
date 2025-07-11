<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

# Azure Documentation MCP Server - Copilot Instructions

This is a .NET 9.0 Web API project that implements an MCP (Model Context Protocol) Server-Sent Events endpoint for serving Microsoft Azure architecture documentation and best practices.

## Project Structure
- **Models/**: MCP protocol models and Azure documentation models
- **Services/**: Core business logic including MCP server, Azure documentation service, and SSE service
- **Program.cs**: Main application setup with dependency injection and endpoint configuration

## Key Technologies
- .NET 9.0 Web API
- Model Context Protocol (MCP) for AI agent communication
- Server-Sent Events (SSE) for real-time streaming
- Azure Learn documentation integration
- HTTP clients for external API calls

## MCP Tools Available
1. **search-azure-docs**: Search Microsoft Azure documentation for architecture patterns and best practices
2. **validate-aks-design**: Validate AKS design against Microsoft recommended patterns and identify anti-patterns
3. **get-azure-patterns**: Get specific Azure architecture patterns and reference architectures

## Architecture Guidelines
- Follow Azure Well-Architected Framework principles
- Use managed identity for authentication (when applicable)
- Implement proper error handling and logging
- Follow RESTful API design patterns
- Use dependency injection for service management

## Development Guidelines
- Use proper async/await patterns for all I/O operations
- Implement comprehensive logging for debugging and monitoring
- Follow secure coding practices, especially for external API calls
- Use nullable reference types to prevent null reference exceptions
- Implement proper cancellation token handling for long-running operations

## MCP Protocol Compliance
- Follow JSON-RPC 2.0 specification for request/response format
- Implement proper error codes and messages
- Support streaming responses via Server-Sent Events
- Provide comprehensive tool schemas for AI agent integration

## Testing Endpoints
- Primary MCP endpoint: `/mcp` (GET for SSE, POST for requests)
- Health check: `/health`
- Available tools: `/mcp/tools`
- Server info: `/` (root endpoint)

You can find more info and examples at https://modelcontextprotocol.io/llms-full.txt
