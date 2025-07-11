using McpAzureDocs.Package.Models;
using McpAzureDocs.Package.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpAzureDocs.Package.Services;

/// <summary>
/// Service for fetching and processing Microsoft Azure documentation
/// Provides curated Azure architecture patterns, best practices, and anti-patterns
/// </summary>
public class AzureDocumentationService : IAzureDocsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureDocumentationService> _logger;
    private readonly McpAzureDocsOptions _options;
    
    // Curated Azure documentation and best practices
    private readonly Dictionary<string, List<AzureDocResult>> _azureKnowledgeBase;
    
    public AzureDocumentationService(
        HttpClient httpClient, 
        ILogger<AzureDocumentationService> logger,
        IOptions<McpAzureDocsOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _azureKnowledgeBase = InitializeKnowledgeBase();
        
        // Configure HTTP client
        _httpClient.DefaultRequestHeaders.Add("User-Agent", _options.UserAgent);
    }
    
    /// <summary>
    /// Search Azure documentation based on query
    /// </summary>
    public Task<List<AzureDocResult>> SearchDocumentationAsync(
        string query, 
        string? service = null, 
        string? category = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var docQuery = new AzureDocQuery
            {
                Query = query,
                Service = service,
                Category = category
            };
            
            var results = new List<AzureDocResult>();
            
            // Search through curated knowledge base first
            var localResults = SearchLocalKnowledgeBase(docQuery);
            results.AddRange(localResults);
            
            // If we have specific service mentioned, add service-specific guidance
            if (!string.IsNullOrEmpty(service))
            {
                var serviceResults = GetServiceSpecificGuidance(service);
                results.AddRange(serviceResults);
            }
            
            // Sort by relevance score and limit results
            results = results
                .OrderByDescending(r => r.RelevanceScore)
                .Take(_options.MaxSearchResults)
                .ToList();
            
            _logger.LogInformation("Found {Count} documentation results for query: {Query}", 
                results.Count, query);
            
            return Task.FromResult(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Azure documentation for query: {Query}", query);
            return Task.FromResult(new List<AzureDocResult>());
        }
    }
    
    /// <summary>
    /// Validate AKS design against Microsoft patterns
    /// </summary>
    public Task<List<AzureDocResult>> ValidateAksDesignAsync(
        string designDescription, 
        CancellationToken cancellationToken = default)
    {
        var results = new List<AzureDocResult>();
        
        try
        {
            // Analyze for common AKS patterns and anti-patterns
            var patterns = AnalyzeAksPatterns(designDescription);
            results.AddRange(patterns);
            
            // Add general AKS best practices
            var bestPractices = GetAksBestPractices();
            results.AddRange(bestPractices);
            
            _logger.LogInformation("Generated {Count} AKS validation results", results.Count);
            
            return Task.FromResult(results.OrderByDescending(r => r.RelevanceScore).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating AKS design");
            return Task.FromResult(results);
        }
    }
    
    /// <summary>
    /// Get specific Azure architecture patterns
    /// </summary>
    public Task<List<AzureDocResult>> GetArchitecturePatternsAsync(
        string patternType, 
        string? service = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var results = new List<AzureDocResult>();
            
            // Get patterns from knowledge base
            if (_azureKnowledgeBase.TryGetValue("patterns", out var patterns))
            {
                var filteredPatterns = patterns
                    .Where(p => p.Title.Contains(patternType, StringComparison.OrdinalIgnoreCase) ||
                               p.Content.Contains(patternType, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                if (!string.IsNullOrEmpty(service))
                {
                    filteredPatterns = filteredPatterns
                        .Where(p => p.Title.Contains(service, StringComparison.OrdinalIgnoreCase) ||
                                   p.Content.Contains(service, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                
                results.AddRange(filteredPatterns);
            }
            
            // Add well-architected framework guidance
            var frameworkGuidance = GetWellArchitectedGuidance(patternType);
            results.AddRange(frameworkGuidance);
            
            _logger.LogInformation("Found {Count} architecture patterns for type: {PatternType}", 
                results.Count, patternType);
            
            return Task.FromResult(results.OrderByDescending(r => r.RelevanceScore).Take(_options.MaxSearchResults).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting architecture patterns for type: {PatternType}", patternType);
            return Task.FromResult(new List<AzureDocResult>());
        }
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
        var score = 0.0f;
        var titleLower = doc.Title.ToLowerInvariant();
        var contentLower = doc.Content.ToLowerInvariant();
        
        // Exact title match gets highest score
        if (titleLower.Contains(query))
            score += 1.0f;
        
        // Content matches get medium score
        if (contentLower.Contains(query))
            score += 0.7f;
        
        // Word matches get lower score
        var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in queryWords)
        {
            if (titleLower.Contains(word))
                score += 0.5f;
            if (contentLower.Contains(word))
                score += 0.3f;
        }
        
        return Math.Min(score, 2.0f); // Cap at 2.0
    }
    
    private List<AzureDocResult> GetServiceSpecificGuidance(string service)
    {
        var serviceLower = service.ToLowerInvariant();
        var results = new List<AzureDocResult>();
        
        // Add service-specific patterns based on common services
        switch (serviceLower)
        {
            case "aks":
            case "kubernetes":
                results.AddRange(GetAksBestPractices());
                break;
            case "appservice":
            case "app service":
                results.AddRange(GetAppServiceBestPractices());
                break;
            case "functions":
            case "azure functions":
                results.AddRange(GetFunctionsBestPractices());
                break;
            case "cosmosdb":
            case "cosmos db":
                results.AddRange(GetCosmosDbBestPractices());
                break;
        }
        
        return results;
    }
    
    private List<AzureDocResult> AnalyzeAksPatterns(string designDescription)
    {
        var results = new List<AzureDocResult>();
        var description = designDescription.ToLowerInvariant();
        
        // Check for common anti-patterns and provide guidance
        if (description.Contains("single node") || description.Contains("one node"))
        {
            results.Add(new AzureDocResult
            {
                Title = "⚠️ AKS Single Node Anti-Pattern",
                Content = "Running AKS with a single node is not recommended for production. Consider using multiple nodes across availability zones for high availability.",
                Category = "Anti-Pattern",
                RelevanceScore = 0.9f,
                Url = "https://docs.microsoft.com/en-us/azure/aks/availability-zones"
            });
        }
        
        if (description.Contains("no monitoring") || !description.Contains("monitor"))
        {
            results.Add(new AzureDocResult
            {
                Title = "📊 AKS Monitoring Best Practice",
                Content = "Enable Azure Monitor for containers to get comprehensive monitoring and logging for your AKS cluster.",
                Category = "Best Practice",
                RelevanceScore = 0.8f,
                Url = "https://docs.microsoft.com/en-us/azure/azure-monitor/containers/container-insights-overview"
            });
        }
        
        if (description.Contains("public ip") || description.Contains("public endpoint"))
        {
            results.Add(new AzureDocResult
            {
                Title = "🔒 AKS Network Security",
                Content = "Consider using private clusters and internal load balancers to improve security posture.",
                Category = "Security",
                RelevanceScore = 0.85f,
                Url = "https://docs.microsoft.com/en-us/azure/aks/private-clusters"
            });
        }
        
        return results;
    }
    
    private List<AzureDocResult> GetAksBestPractices()
    {
        return new List<AzureDocResult>
        {
            new()
            {
                Title = "AKS Cluster Architecture Best Practices",
                Content = "Design your AKS cluster with multiple node pools, enable cluster autoscaler, use availability zones, and implement proper resource quotas.",
                Category = "Best Practices",
                RelevanceScore = 0.95f,
                Url = "https://docs.microsoft.com/en-us/azure/aks/best-practices"
            },
            new()
            {
                Title = "AKS Security Best Practices",
                Content = "Implement pod security standards, use Azure AD integration, enable network policies, and regularly update your cluster.",
                Category = "Security",
                RelevanceScore = 0.9f,
                Url = "https://docs.microsoft.com/en-us/azure/aks/security-best-practices"
            },
            new()
            {
                Title = "AKS Networking Best Practices",
                Content = "Choose the right network plugin (Azure CNI vs kubenet), implement network policies, and consider private clusters for sensitive workloads.",
                Category = "Networking",
                RelevanceScore = 0.85f,
                Url = "https://docs.microsoft.com/en-us/azure/aks/networking-overview"
            }
        };
    }
    
    private List<AzureDocResult> GetAppServiceBestPractices()
    {
        return new List<AzureDocResult>
        {
            new()
            {
                Title = "App Service Deployment Best Practices",
                Content = "Use deployment slots for zero-downtime deployments, enable auto-scaling, and implement proper health checks.",
                Category = "Best Practices",
                RelevanceScore = 0.9f,
                Url = "https://docs.microsoft.com/en-us/azure/app-service/deploy-best-practices"
            },
            new()
            {
                Title = "App Service Security Best Practices",
                Content = "Enable managed identity, use Key Vault for secrets, implement proper authentication, and enable HTTPS only.",
                Category = "Security",
                RelevanceScore = 0.85f,
                Url = "https://docs.microsoft.com/en-us/azure/app-service/security-recommendations"
            }
        };
    }
    
    private List<AzureDocResult> GetFunctionsBestPractices()
    {
        return new List<AzureDocResult>
        {
            new()
            {
                Title = "Azure Functions Performance Best Practices",
                Content = "Optimize cold start times, use appropriate hosting plans, implement connection pooling, and avoid blocking calls.",
                Category = "Performance",
                RelevanceScore = 0.9f,
                Url = "https://docs.microsoft.com/en-us/azure/azure-functions/functions-best-practices"
            },
            new()
            {
                Title = "Azure Functions Security Best Practices",
                Content = "Use function-level authentication, implement proper error handling, and secure function keys properly.",
                Category = "Security",
                RelevanceScore = 0.85f,
                Url = "https://docs.microsoft.com/en-us/azure/azure-functions/security-concepts"
            }
        };
    }
    
    private List<AzureDocResult> GetCosmosDbBestPractices()
    {
        return new List<AzureDocResult>
        {
            new()
            {
                Title = "Cosmos DB Partitioning Best Practices",
                Content = "Choose the right partition key, design for scale, avoid hot partitions, and implement proper data modeling.",
                Category = "Data Modeling",
                RelevanceScore = 0.95f,
                Url = "https://docs.microsoft.com/en-us/azure/cosmos-db/partitioning-overview"
            },
            new()
            {
                Title = "Cosmos DB Performance Best Practices",
                Content = "Optimize queries, use connection pooling, implement proper indexing strategies, and monitor RU consumption.",
                Category = "Performance",
                RelevanceScore = 0.9f,
                Url = "https://docs.microsoft.com/en-us/azure/cosmos-db/performance-tips"
            }
        };
    }
    
    private List<AzureDocResult> GetWellArchitectedGuidance(string patternType)
    {
        var patternLower = patternType.ToLowerInvariant();
        var results = new List<AzureDocResult>();
        
        switch (patternLower)
        {
            case "microservices":
                results.Add(new AzureDocResult
                {
                    Title = "Microservices Architecture on Azure",
                    Content = "Design microservices with API gateways, service meshes, distributed tracing, and proper data isolation.",
                    Category = "Architecture Pattern",
                    RelevanceScore = 0.95f,
                    Url = "https://docs.microsoft.com/en-us/azure/architecture/microservices/"
                });
                break;
            case "baseline":
                results.Add(new AzureDocResult
                {
                    Title = "Azure Baseline Architecture",
                    Content = "Implement a secure, scalable baseline architecture with proper networking, security, and monitoring.",
                    Category = "Architecture Pattern",
                    RelevanceScore = 0.9f,
                    Url = "https://docs.microsoft.com/en-us/azure/architecture/guide/"
                });
                break;
            case "enterprise":
                results.Add(new AzureDocResult
                {
                    Title = "Enterprise-Scale Architecture",
                    Content = "Design enterprise-scale landing zones with proper governance, security, and compliance frameworks.",
                    Category = "Architecture Pattern",
                    RelevanceScore = 0.95f,
                    Url = "https://docs.microsoft.com/en-us/azure/cloud-adoption-framework/ready/enterprise-scale/"
                });
                break;
        }
        
        return results;
    }
    
    private Dictionary<string, List<AzureDocResult>> InitializeKnowledgeBase()
    {
        return new Dictionary<string, List<AzureDocResult>>
        {
            ["best-practices"] = new List<AzureDocResult>
            {
                new()
                {
                    Title = "Azure Well-Architected Framework",
                    Content = "The Azure Well-Architected Framework consists of five pillars: Reliability, Security, Cost Optimization, Operational Excellence, and Performance Efficiency.",
                    Category = "Framework",
                    RelevanceScore = 1.0f,
                    Url = "https://docs.microsoft.com/en-us/azure/architecture/framework/"
                },
                new()
                {
                    Title = "Azure Security Best Practices",
                    Content = "Implement defense in depth, use managed identity, enable monitoring and logging, and follow the principle of least privilege.",
                    Category = "Security",
                    RelevanceScore = 0.95f,
                    Url = "https://docs.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns"
                }
            },
            ["patterns"] = new List<AzureDocResult>
            {
                new()
                {
                    Title = "Cloud Design Patterns",
                    Content = "Common cloud design patterns including Circuit Breaker, Retry, Bulkhead, and Strangler Fig patterns for building resilient applications.",
                    Category = "Design Patterns",
                    RelevanceScore = 0.9f,
                    Url = "https://docs.microsoft.com/en-us/azure/architecture/patterns/"
                },
                new()
                {
                    Title = "Microservices Pattern",
                    Content = "Design microservices with proper service boundaries, API contracts, data isolation, and communication patterns.",
                    Category = "Microservices",
                    RelevanceScore = 0.85f,
                    Url = "https://docs.microsoft.com/en-us/azure/architecture/microservices/"
                }
            },
            ["security"] = new List<AzureDocResult>
            {
                new()
                {
                    Title = "Zero Trust Security Model",
                    Content = "Implement zero trust principles with identity verification, device compliance, and application protection.",
                    Category = "Security Model",
                    RelevanceScore = 0.9f,
                    Url = "https://docs.microsoft.com/en-us/security/zero-trust/"
                },
                new()
                {
                    Title = "Azure AD Security Best Practices",
                    Content = "Use conditional access, enable MFA, implement privileged identity management, and monitor sign-in activities.",
                    Category = "Identity Security",
                    RelevanceScore = 0.85f,
                    Url = "https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/concept-fundamentals-security-defaults"
                }
            }
        };
    }
}
