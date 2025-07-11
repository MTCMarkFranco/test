using McpAzureDocs.Package.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace McpAzureDocs.Package.Services;

/// <summary>
/// Service for handling Server-Sent Events streaming of Azure documentation responses
/// </summary>
public class StreamingService : IStreamingService
{
    private readonly IAzureDocsClient _azureDocsClient;
    private readonly ILogger<StreamingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public StreamingService(IAzureDocsClient azureDocsClient, ILogger<StreamingService> logger)
    {
        _azureDocsClient = azureDocsClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
    
    /// <summary>
    /// Stream documentation search results
    /// </summary>
    public async Task StreamDocumentationSearchAsync(
        HttpContext context, 
        string query, 
        string? service = null, 
        CancellationToken cancellationToken = default)
    {
        await SetupSseResponse(context);
        
        try
        {
            _logger.LogInformation("Streaming documentation search for query: {Query}", query);
            
            // Send initial message
            await SendSseEventAsync(context, "search_start", new { 
                query, 
                service,
                timestamp = DateTime.UtcNow 
            }, cancellationToken);
            
            // Perform search
            var results = await _azureDocsClient.SearchDocumentationAsync(query, service, null, cancellationToken);
            
            // Stream results one by one
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                await SendSseEventAsync(context, "result", new {
                    index = i + 1,
                    total = results.Count,
                    title = result.Title,
                    content = result.Content,
                    category = result.Category,
                    url = result.Url,
                    relevanceScore = result.RelevanceScore
                }, cancellationToken);
                
                // Small delay between results for better UX
                await Task.Delay(100, cancellationToken);
            }
            
            // Send completion message
            await SendSseEventAsync(context, "search_complete", new { 
                totalResults = results.Count,
                timestamp = DateTime.UtcNow 
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Documentation search streaming cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming documentation search");
            await SendSseEventAsync(context, "error", new { 
                message = "Error occurred during search" 
            }, CancellationToken.None);
        }
    }
    
    /// <summary>
    /// Stream AKS validation results
    /// </summary>
    public async Task StreamAksValidationAsync(
        HttpContext context, 
        string designDescription, 
        CancellationToken cancellationToken = default)
    {
        await SetupSseResponse(context);
        
        try
        {
            _logger.LogInformation("Streaming AKS validation");
            
            // Send initial message
            await SendSseEventAsync(context, "validation_start", new { 
                timestamp = DateTime.UtcNow 
            }, cancellationToken);
            
            // Perform validation
            var results = await _azureDocsClient.ValidateAksDesignAsync(designDescription, cancellationToken);
            
            // Stream results one by one
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                await SendSseEventAsync(context, "validation_result", new {
                    index = i + 1,
                    total = results.Count,
                    title = result.Title,
                    content = result.Content,
                    category = result.Category,
                    url = result.Url,
                    relevanceScore = result.RelevanceScore
                }, cancellationToken);
                
                // Small delay between results
                await Task.Delay(150, cancellationToken);
            }
            
            // Send completion message
            await SendSseEventAsync(context, "validation_complete", new { 
                totalIssues = results.Count,
                timestamp = DateTime.UtcNow 
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AKS validation streaming cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming AKS validation");
            await SendSseEventAsync(context, "error", new { 
                message = "Error occurred during validation" 
            }, CancellationToken.None);
        }
    }
    
    /// <summary>
    /// Stream architecture patterns
    /// </summary>
    public async Task StreamArchitecturePatternsAsync(
        HttpContext context, 
        string patternType, 
        string? service = null, 
        CancellationToken cancellationToken = default)
    {
        await SetupSseResponse(context);
        
        try
        {
            _logger.LogInformation("Streaming architecture patterns for type: {PatternType}", patternType);
            
            // Send initial message
            await SendSseEventAsync(context, "patterns_start", new { 
                patternType,
                service,
                timestamp = DateTime.UtcNow 
            }, cancellationToken);
            
            // Get patterns
            var results = await _azureDocsClient.GetArchitecturePatternsAsync(patternType, service, cancellationToken);
            
            // Stream results one by one
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                await SendSseEventAsync(context, "pattern", new {
                    index = i + 1,
                    total = results.Count,
                    title = result.Title,
                    content = result.Content,
                    category = result.Category,
                    url = result.Url,
                    relevanceScore = result.RelevanceScore
                }, cancellationToken);
                
                // Small delay between results
                await Task.Delay(120, cancellationToken);
            }
            
            // Send completion message
            await SendSseEventAsync(context, "patterns_complete", new { 
                totalPatterns = results.Count,
                timestamp = DateTime.UtcNow 
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Architecture patterns streaming cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming architecture patterns");
            await SendSseEventAsync(context, "error", new { 
                message = "Error occurred while getting patterns" 
            }, CancellationToken.None);
        }
    }
    
    private static async Task SetupSseResponse(HttpContext context)
    {
        context.Response.Headers.Add("Content-Type", "text/event-stream");
        context.Response.Headers.Add("Cache-Control", "no-cache");
        context.Response.Headers.Add("Connection", "keep-alive");
        context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Add("Access-Control-Allow-Headers", "Cache-Control");
        
        await context.Response.Body.FlushAsync();
    }
    
    private async Task SendSseEventAsync(HttpContext context, string eventType, object data, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var sseMessage = $"event: {eventType}\ndata: {json}\n\n";
            
            await context.Response.WriteAsync(sseMessage, cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SSE event: {EventType}", eventType);
            throw;
        }
    }
}
