using McpAzureDocsApi.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpAzureDocsApi.Services;

/// <summary>
/// Service for fetching and processing Microsoft Azure documentation
/// Provides curated Azure architecture patterns, best practices, and anti-patterns
/// </summary>
public class AzureDocumentationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureDocumentationService> _logger;
    
    // Curated Azure documentation and best practices
    private readonly Dictionary<string, List<AzureDocResult>> _azureKnowledgeBase;
    
    public AzureDocumentationService(HttpClient httpClient, ILogger<AzureDocumentationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _azureKnowledgeBase = InitializeKnowledgeBase();
    }
    
    /// <summary>
    /// Search Azure documentation based on query
    /// </summary>
    public async Task<List<AzureDocResult>> SearchDocumentationAsync(AzureDocQuery query)
    {
        try
        {
            var results = new List<AzureDocResult>();
            
            // Search through curated knowledge base first
            var localResults = SearchLocalKnowledgeBase(query);
            results.AddRange(localResults);
            
            // If we have specific service mentioned, add service-specific guidance
            if (!string.IsNullOrEmpty(query.Service))
            {
                var serviceResults = GetServiceSpecificGuidance(query.Service);
                results.AddRange(serviceResults);
            }
            
            // Sort by relevance score
            results = results.OrderByDescending(r => r.RelevanceScore).Take(10).ToList();
            
            _logger.LogInformation("Found {Count} documentation results for query: {Query}", 
                results.Count, query.Query);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Azure documentation for query: {Query}", query.Query);
            return new List<AzureDocResult>();
        }
    }
    
    /// <summary>
    /// Get validation results for AKS design against Microsoft patterns
    /// </summary>
    public async Task<List<AzureDocResult>> ValidateAksDesignAsync(string designDescription)
    {
        var results = new List<AzureDocResult>();
        
        // Analyze for common AKS patterns and anti-patterns
        var patterns = AnalyzeAksPatterns(designDescription);
        results.AddRange(patterns);
        
        // Add general AKS best practices
        var bestPractices = GetAksBestPractices();
        results.AddRange(bestPractices);
        
        return results.OrderByDescending(r => r.RelevanceScore).ToList();
    }
    
    private List<AzureDocResult> SearchLocalKnowledgeBase(AzureDocQuery query)
    {
        var results = new List<AzureDocResult>();
        var queryLower = query.Query.ToLowerInvariant();
        
        foreach (var category in _azureKnowledgeBase)
        {
            foreach (var doc in category.Value)
            {
                var score = CalculateRelevanceScore(queryLower, doc);
                if (score > 0.3f)
                {
                    results.Add(doc with { RelevanceScore = score });
                }
            }
        }
        
        return results;
    }
    
    private float CalculateRelevanceScore(string query, AzureDocResult doc)
    {
        var score = 0f;
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in queryWords)
        {
            if (doc.Title.ToLowerInvariant().Contains(word)) score += 0.3f;
            if (doc.Content.ToLowerInvariant().Contains(word)) score += 0.2f;
            if (doc.Category.ToLowerInvariant().Contains(word)) score += 0.1f;
        }
        
        return Math.Min(score, 1.0f);
    }
    
    private List<AzureDocResult> GetServiceSpecificGuidance(string service)
    {
        var serviceLower = service.ToLowerInvariant();
        
        return serviceLower switch
        {
            "aks" or "kubernetes" => GetAksBestPractices(),
            "app service" or "webapp" => GetAppServiceBestPractices(),
            "functions" => GetFunctionsBestPractices(),
            "container apps" => GetContainerAppsBestPractices(),
            _ => new List<AzureDocResult>()
        };
    }
    
    private List<AzureDocResult> AnalyzeAksPatterns(string designDescription)
    {
        var results = new List<AzureDocResult>();
        var description = designDescription.ToLowerInvariant();
        
        // Check for anti-patterns
        if (description.Contains("single node") || description.Contains("one node"))
        {
            results.Add(new AzureDocResult
            {
                Title = "AKS Anti-Pattern: Single Node Cluster",
                Content = "Single node AKS clusters are not recommended for production workloads. Use multi-node clusters with at least 3 nodes for high availability. Consider node pools for different workload types.",
                Url = "https://learn.microsoft.com/en-us/azure/aks/use-multiple-node-pools",
                Category = "Anti-Pattern",
                RelevanceScore = 0.9f
            });
        }
        
        if (!description.Contains("rbac") && !description.Contains("role-based"))
        {
            results.Add(new AzureDocResult
            {
                Title = "AKS Security: Enable RBAC",
                Content = "Always enable Role-Based Access Control (RBAC) for AKS clusters. RBAC provides fine-grained access control and is essential for production security.",
                Url = "https://learn.microsoft.com/en-us/azure/aks/concepts-identity",
                Category = "Security Best Practice",
                RelevanceScore = 0.8f
            });
        }
        
        if (!description.Contains("network policy") && !description.Contains("calico") && !description.Contains("azure cni"))
        {
            results.Add(new AzureDocResult
            {
                Title = "AKS Networking: Implement Network Policies",
                Content = "Use network policies to control traffic between pods. Choose Azure CNI for advanced networking features and better integration with Azure networking.",
                Url = "https://learn.microsoft.com/en-us/azure/aks/concepts-network",
                Category = "Networking Best Practice",
                RelevanceScore = 0.8f
            });
        }
        
        return results;
    }
    
    private List<AzureDocResult> GetAksBestPractices()
    {
        return _azureKnowledgeBase.GetValueOrDefault("aks", new List<AzureDocResult>());
    }
    
    private List<AzureDocResult> GetAppServiceBestPractices()
    {
        return _azureKnowledgeBase.GetValueOrDefault("appservice", new List<AzureDocResult>());
    }
    
    private List<AzureDocResult> GetFunctionsBestPractices()
    {
        return _azureKnowledgeBase.GetValueOrDefault("functions", new List<AzureDocResult>());
    }
    
    private List<AzureDocResult> GetContainerAppsBestPractices()
    {
        return _azureKnowledgeBase.GetValueOrDefault("containerapps", new List<AzureDocResult>());
    }
    
    private Dictionary<string, List<AzureDocResult>> InitializeKnowledgeBase()
    {
        return new Dictionary<string, List<AzureDocResult>>
        {
            ["aks"] = new List<AzureDocResult>
            {
                new()
                {
                    Title = "AKS Baseline Architecture",
                    Content = "The AKS baseline architecture provides a recommended starting point for most AKS deployments. It includes network security, identity and access management, security monitoring, and day-2 operations guidance. Key components include Azure CNI networking, managed identity, Azure Monitor, and GitOps for deployment automation.",
                    Url = "https://learn.microsoft.com/en-us/azure/architecture/reference-architectures/containers/aks/baseline-aks",
                    Category = "Architecture Pattern",
                    RelevanceScore = 1.0f
                },
                new()
                {
                    Title = "AKS Best Practices for Cluster Operators",
                    Content = "Essential practices include: Use managed identity for authentication, implement network policies, enable Azure Monitor for monitoring, use multiple node pools for workload isolation, implement proper resource quotas and limits, and use Azure Policy for governance.",
                    Url = "https://learn.microsoft.com/en-us/azure/aks/best-practices",
                    Category = "Best Practices",
                    RelevanceScore = 0.9f
                },
                new()
                {
                    Title = "AKS Developer Best Practices",
                    Content = "Developer practices include: Use resource requests and limits, implement health checks, use secrets management, follow container image best practices, implement proper logging, and use Helm for application packaging.",
                    Url = "https://learn.microsoft.com/en-us/azure/aks/developer-best-practices-resource-management",
                    Category = "Developer Best Practices",
                    RelevanceScore = 0.9f
                },
                new()
                {
                    Title = "AKS Security Best Practices",
                    Content = "Security recommendations: Enable Microsoft Defender for Containers, use private clusters when possible, implement pod security standards, rotate certificates regularly, use Azure Key Vault for secrets, and enable audit logging.",
                    Url = "https://learn.microsoft.com/en-us/azure/aks/security-best-practices",
                    Category = "Security",
                    RelevanceScore = 0.9f
                },
                new()
                {
                    Title = "AKS Networking Best Practices",
                    Content = "Use Azure CNI for advanced networking, implement network policies for micro-segmentation, use Application Gateway Ingress Controller for external traffic, configure private endpoints for secure connectivity, and plan IP address space carefully.",
                    Url = "https://learn.microsoft.com/en-us/azure/aks/concepts-network",
                    Category = "Networking",
                    RelevanceScore = 0.8f
                }
            },
            ["appservice"] = new List<AzureDocResult>
            {
                new()
                {
                    Title = "App Service Best Practices",
                    Content = "Use deployment slots for zero-downtime deployments, implement health checks, use Application Gateway for SSL termination, enable diagnostic logging, use managed identity for secure access to resources.",
                    Url = "https://learn.microsoft.com/en-us/azure/app-service/app-service-best-practices",
                    Category = "Best Practices",
                    RelevanceScore = 0.9f
                }
            },
            ["functions"] = new List<AzureDocResult>
            {
                new()
                {
                    Title = "Azure Functions Best Practices",
                    Content = "Design functions to be stateless, avoid long-running functions, use dependency injection, implement proper error handling and retry policies, monitor performance with Application Insights.",
                    Url = "https://learn.microsoft.com/en-us/azure/azure-functions/functions-best-practices",
                    Category = "Best Practices",
                    RelevanceScore = 0.9f
                }
            },
            ["containerapps"] = new List<AzureDocResult>
            {
                new()
                {
                    Title = "Container Apps Best Practices",
                    Content = "Use managed identity for authentication, implement health probes, configure scaling rules appropriately, use Dapr for microservices communication, implement proper secret management.",
                    Url = "https://learn.microsoft.com/en-us/azure/container-apps/",
                    Category = "Best Practices",
                    RelevanceScore = 0.9f
                }
            }
        };
    }
}
