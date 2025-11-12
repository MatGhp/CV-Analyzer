# Golden Architecture Guide for CV Analyzer

## Overview

This document maps Microsoft's recommended "Golden Path" architecture for enterprise-grade agentic environments to the CV Analyzer application. It provides a reference guide for selecting Azure components, implementing agent orchestration, and maintaining production-ready standards as we add new features.

**Last Updated**: November 12, 2025  
**Architecture Source**: Microsoft Azure AI Foundry Golden Architecture

---

## Table of Contents

1. [Architecture Vision](#architecture-vision)
2. [Component Mapping](#component-mapping)
3. [BYO Resources Integration](#byo-resources-integration)
4. [Agent Orchestration Pattern](#agent-orchestration-pattern)
5. [Grounding & Context Retrieval](#grounding--context-retrieval)
6. [Observability & Telemetry](#observability--telemetry)
7. [Security Best Practices](#security-best-practices)
8. [Deployment & Containerization](#deployment--containerization)
9. [Feature Roadmap](#feature-roadmap)
10. [Implementation Checklist](#implementation-checklist)

---

## Architecture Vision

### Core Principles

**Azure AI Foundry as Central Hub**  
All AI operations flow through Azure AI Foundry, providing:
- Standardized agent and model orchestration
- Built-in observability and monitoring
- Consistent security and compliance

**Three Components Working in Harmony**
1. **Built-in AI Tools**: File search, code interpreter
2. **Intelligent Agents**: Decision-makers and task executors
3. **Multiple AI Models**: Working together via orchestration

**Bring Your Own (BYO) Resources**  
Flexibility to use existing Azure infrastructure:
- Cosmos DB for thread/conversation storage
- Key Vault for secure credential management
- Azure Storage for file persistence
- Azure AI Search for semantic indexing

**Container-Ready & API-First**  
Deploy on Azure Container Apps with support for:
- External APIs (OpenAPI specs)
- MCP servers
- Agent-to-Agent (A2A) communication

---

## Component Mapping

### Current Architecture → Golden Architecture

| Golden Component | CV Analyzer Implementation | Location in Repo |
|------------------|---------------------------|------------------|
| **Azure AI Foundry** | Central orchestration for agents & models | `backend/src/CVAnalyzer.AgentService/` |
| **Microsoft Agent Framework** | Multi-agent orchestrator | `backend/src/CVAnalyzer.AgentService/ResumeAnalysisAgent.cs` |
| **Specialized Agents** | Resume parsing, scoring, suggestion generation | To be implemented in `CVAnalyzer.AgentService/Agents/` |
| **Built-in AI Tools** | File search, code interpreter | To be added via Azure AI Foundry SDK |
| **Models** | GPT-4o for analysis | Currently via `ai-service/`; migrate to Agent Framework |
| **Cosmos DB** | Thread & conversation persistence | To be added in `CVAnalyzer.Infrastructure/Persistence/` |
| **Key Vault** | Secrets management | Partially implemented in `DependencyInjection.cs` |
| **Azure Storage** | Resume blob storage | `CVAnalyzer.Infrastructure/Services/BlobStorageService.cs` |
| **Azure AI Search** | Semantic search & grounding | To be implemented in `CVAnalyzer.Infrastructure/Services/AI/` |
| **Grounding (Bing)** | Real-time web context | Future integration |
| **Logic Apps/Functions** | Workflow automation | Future integration |
| **Bot Service** | Omnichannel delivery | To be implemented in `CVAnalyzer.API/Controllers/` |
| **Container Apps** | Deployment target | Terraform modules: `terraform/modules/container-apps/` |
| **Application Insights** | Observability | To be configured in `Program.cs` |

---

## BYO Resources Integration

### 1. Cosmos DB (Thread Storage)

**Purpose**: Store conversation threads, agent states, and session history for scale and global distribution.

**Implementation Steps**:
```csharp
// Location: backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs

services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["CosmosDb:Endpoint"];
    var key = config["CosmosDb:Key"]; // From Key Vault
    return new CosmosClient(endpoint, key);
});

services.AddScoped<IConversationStore, CosmosConversationStore>();
```

**Configuration** (`appsettings.json`):
```json
{
  "CosmosDb": {
    "Endpoint": "https://<account>.documents.azure.com:443/",
    "DatabaseName": "CVAnalyzer",
    "ContainerName": "Conversations"
  }
}
```

**Files to Create/Modify**:
- `backend/src/CVAnalyzer.Infrastructure/Persistence/CosmosConversationStore.cs`
- `backend/src/CVAnalyzer.Domain/Entities/Conversation.cs`
- `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs`

---

### 2. Key Vault (Secrets Management)

**Purpose**: Centralized, secure storage for connection strings, API keys, and credentials.

**Current Implementation**: Partial support exists in `DependencyInjection.cs`.

**Enhancement Steps**:
```csharp
// Location: backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs

var useKeyVault = configuration.GetValue<bool>("UseKeyVault");
if (useKeyVault)
{
    var keyVaultUri = new Uri(configuration["KeyVault:Uri"]!);
    var credential = new DefaultAzureCredential();
    var secretClient = new SecretClient(keyVaultUri, credential);
    
    // Retrieve secrets
    var dbConnectionString = await secretClient.GetSecretAsync("DatabaseConnectionString");
    var storageConnectionString = await secretClient.GetSecretAsync("StorageConnectionString");
    var aiApiKey = await secretClient.GetSecretAsync("AIServiceApiKey");
    
    // Use in services
    configuration["ConnectionStrings:DefaultConnection"] = dbConnectionString.Value.Value;
}
```

**Required Secrets in Key Vault**:
- `DatabaseConnectionString` - SQL Server connection
- `StorageConnectionString` - Azure Blob Storage
- `AIServiceApiKey` - Azure AI Foundry endpoint key
- `CosmosDbKey` - Cosmos DB access key
- `SearchServiceKey` - Azure AI Search admin key

**Files to Modify**:
- `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs`
- `backend/src/CVAnalyzer.API/appsettings.Production.json`

---

### 3. Azure Storage (File Persistence)

**Purpose**: Store uploaded resumes and generated reports.

**Current Implementation**: Already exists via `BlobStorageService.cs`.

**Enhancement**: Add lifecycle policies and CDN for optimized access.

**Files**:
- ✅ `backend/src/CVAnalyzer.Infrastructure/Services/BlobStorageService.cs` (existing)
- Add: `terraform/modules/storage-lifecycle/` (lifecycle policies)

---

### 4. Azure AI Search (Semantic Indexing)

**Purpose**: Enable semantic search over resumes, job descriptions, and knowledge base for agent grounding.

**Implementation Steps**:

**Create Service**:
```csharp
// Location: backend/src/CVAnalyzer.Infrastructure/Services/AI/AzureSearchService.cs

public interface IAzureSearchService
{
    Task IndexResumeAsync(Resume resume, CancellationToken ct);
    Task<IEnumerable<SearchDocument>> QueryAsync(string query, int topK, CancellationToken ct);
    Task DeleteIndexAsync(string indexName, CancellationToken ct);
}

public class AzureSearchService : IAzureSearchService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    
    public AzureSearchService(IConfiguration config)
    {
        var endpoint = new Uri(config["AzureSearch:Endpoint"]!);
        var credential = new AzureKeyCredential(config["AzureSearch:Key"]!);
        
        _indexClient = new SearchIndexClient(endpoint, credential);
        _searchClient = _indexClient.GetSearchClient("resumes");
    }
    
    public async Task IndexResumeAsync(Resume resume, CancellationToken ct)
    {
        var document = new SearchDocument
        {
            ["id"] = resume.Id.ToString(),
            ["content"] = resume.Content,
            ["fileName"] = resume.FileName,
            ["uploadDate"] = resume.UploadDate,
            ["score"] = resume.Score
        };
        
        await _searchClient.UploadDocumentsAsync(new[] { document }, cancellationToken: ct);
    }
    
    public async Task<IEnumerable<SearchDocument>> QueryAsync(string query, int topK, CancellationToken ct)
    {
        var options = new SearchOptions
        {
            Size = topK,
            QueryType = SearchQueryType.Semantic,
            SemanticConfigurationName = "resume-semantic-config"
        };
        
        var response = await _searchClient.SearchAsync<SearchDocument>(query, options, ct);
        return response.Value.GetResults().Select(r => r.Document);
    }
}
```

**Register in DI**:
```csharp
// Location: backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs
services.AddSingleton<IAzureSearchService, AzureSearchService>();
```

**Configuration**:
```json
{
  "AzureSearch": {
    "Endpoint": "https://<service-name>.search.windows.net",
    "IndexName": "resumes"
  }
}
```

**Files to Create**:
- `backend/src/CVAnalyzer.Infrastructure/Services/AI/AzureSearchService.cs`
- `backend/src/CVAnalyzer.Infrastructure/Services/AI/SearchDocument.cs`
- `backend/tests/CVAnalyzer.UnitTests/Services/AzureSearchServiceTests.cs`

---

## Agent Orchestration Pattern

### Multi-Agent Architecture

**Vision**: Break resume analysis into specialized agents coordinated by an orchestrator.

```
┌─────────────────────────────────────────────────────────────┐
│                    Agent Orchestrator                       │
│                  (CVAnalyzer.AgentService)                  │
└─────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│ Resume Parser │   │ Score Agent   │   │ Suggestion    │
│    Agent      │   │               │   │    Agent      │
└───────────────┘   └───────────────┘   └───────────────┘
        │                   │                   │
        └───────────────────┼───────────────────┘
                            │
                            ▼
                    ┌───────────────┐
                    │   Grounder    │
                    │ (Azure Search)│
                    └───────────────┘
```

### Agent Interface Pattern

**Create Base Interface**:
```csharp
// Location: backend/src/CVAnalyzer.AgentService/IAgent.cs

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    Task<AgentResult> RunAsync(AgentRequest request, CancellationToken ct);
}

public record AgentRequest(
    string Content,
    string UserId,
    Dictionary<string, object>? Context = null,
    List<GroundingDocument>? GroundingDocs = null
);

public record AgentResult(
    bool Success,
    string? Output,
    Dictionary<string, object>? Metadata,
    List<string>? Errors = null
);

public record GroundingDocument(
    string Id,
    string Content,
    double RelevanceScore,
    Dictionary<string, object>? Metadata = null
);
```

### Orchestrator Implementation

```csharp
// Location: backend/src/CVAnalyzer.AgentService/AgentOrchestrator.cs

public interface IAgentOrchestrator
{
    Task<OrchestrationResult> RunWorkflowAsync(WorkflowRequest request, CancellationToken ct);
}

public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IEnumerable<IAgent> _agents;
    private readonly IAgentGrounder _grounder;
    private readonly ILogger<AgentOrchestrator> _logger;
    
    public AgentOrchestrator(
        IEnumerable<IAgent> agents,
        IAgentGrounder grounder,
        ILogger<AgentOrchestrator> logger)
    {
        _agents = agents;
        _grounder = grounder;
        _logger = logger;
    }
    
    public async Task<OrchestrationResult> RunWorkflowAsync(
        WorkflowRequest request, 
        CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("AgentWorkflow");
        activity?.SetTag("workflow.id", request.Id);
        
        // Step 1: Ground with relevant context
        var groundingDocs = await _grounder.GroundAsync(
            request.Query, 
            request.Context, 
            ct);
        
        // Step 2: Execute agents in sequence
        var results = new List<AgentResult>();
        var context = request.Context ?? new Dictionary<string, object>();
        
        foreach (var agent in _agents.OrderBy(a => a.Name))
        {
            _logger.LogInformation("Executing agent: {AgentName}", agent.Name);
            
            var agentRequest = new AgentRequest(
                request.Content,
                request.UserId,
                context,
                groundingDocs);
            
            var result = await agent.RunAsync(agentRequest, ct);
            results.Add(result);
            
            // Pass metadata to next agent
            if (result.Metadata != null)
            {
                foreach (var (key, value) in result.Metadata)
                {
                    context[key] = value;
                }
            }
            
            if (!result.Success)
            {
                _logger.LogWarning("Agent {AgentName} failed", agent.Name);
                break;
            }
        }
        
        return new OrchestrationResult(
            request.Id,
            results.All(r => r.Success),
            results,
            context);
    }
}
```

### Example Specialized Agent

```csharp
// Location: backend/src/CVAnalyzer.AgentService/Agents/ResumeParserAgent.cs

public class ResumeParserAgent : IAgent
{
    public string Name => "ResumeParser";
    public string Description => "Extracts structured data from resume text";
    
    private readonly ILogger<ResumeParserAgent> _logger;
    
    public ResumeParserAgent(ILogger<ResumeParserAgent> logger)
    {
        _logger = logger;
    }
    
    public async Task<AgentResult> RunAsync(AgentRequest request, CancellationToken ct)
    {
        try
        {
            // Parse resume content (extract sections, skills, experience)
            var parsed = ParseResumeContent(request.Content);
            
            var metadata = new Dictionary<string, object>
            {
                ["parsed_sections"] = parsed.Sections,
                ["extracted_skills"] = parsed.Skills,
                ["years_experience"] = parsed.YearsExperience
            };
            
            return new AgentResult(true, parsed.Summary, metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resume parsing failed");
            return new AgentResult(false, null, null, new[] { ex.Message }.ToList());
        }
    }
    
    private ParsedResume ParseResumeContent(string content)
    {
        // Implementation details
        throw new NotImplementedException();
    }
}
```

**Files to Create**:
- `backend/src/CVAnalyzer.AgentService/IAgent.cs`
- `backend/src/CVAnalyzer.AgentService/IAgentOrchestrator.cs`
- `backend/src/CVAnalyzer.AgentService/AgentOrchestrator.cs`
- `backend/src/CVAnalyzer.AgentService/Agents/ResumeParserAgent.cs`
- `backend/src/CVAnalyzer.AgentService/Agents/ScoreAgent.cs`
- `backend/src/CVAnalyzer.AgentService/Agents/SuggestionAgent.cs`

---

## Grounding & Context Retrieval

### Agent Grounder Component

**Purpose**: Provide relevant context to agents before model invocation (RAG pattern).

```csharp
// Location: backend/src/CVAnalyzer.AgentService/IAgentGrounder.cs

public interface IAgentGrounder
{
    Task<List<GroundingDocument>> GroundAsync(
        string query,
        Dictionary<string, object>? context,
        CancellationToken ct);
}

public class AgentGrounder : IAgentGrounder
{
    private readonly IAzureSearchService _searchService;
    private readonly ILogger<AgentGrounder> _logger;
    
    public AgentGrounder(
        IAzureSearchService searchService,
        ILogger<AgentGrounder> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }
    
    public async Task<List<GroundingDocument>> GroundAsync(
        string query,
        Dictionary<string, object>? context,
        CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("AgentGrounding");
        
        // Query Azure AI Search for relevant documents
        var searchResults = await _searchService.QueryAsync(query, topK: 5, ct);
        
        var groundingDocs = searchResults.Select(doc => new GroundingDocument(
            Id: doc["id"]?.ToString() ?? string.Empty,
            Content: doc["content"]?.ToString() ?? string.Empty,
            RelevanceScore: doc.Score ?? 0.0,
            Metadata: doc.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        )).ToList();
        
        _logger.LogInformation(
            "Grounding returned {Count} documents for query: {Query}",
            groundingDocs.Count,
            query);
        
        return groundingDocs;
    }
}
```

**Register in DI**:
```csharp
// Location: backend/src/CVAnalyzer.AgentService/AgentStartup.cs
services.AddScoped<IAgentGrounder, AgentGrounder>();
```

**Files to Create**:
- `backend/src/CVAnalyzer.AgentService/IAgentGrounder.cs`
- `backend/src/CVAnalyzer.AgentService/AgentGrounder.cs`

---

## Observability & Telemetry

### OpenTelemetry Integration

**Purpose**: Distributed tracing, metrics, and logs for agent operations.

**Setup in Program.cs**:
```csharp
// Location: backend/src/CVAnalyzer.API/Program.cs

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("CVAnalyzer.API")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["service.version"] = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("CVAnalyzer.AgentService")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("CVAnalyzer.AgentService")
        .AddOtlpExporter());
```

**Application Insights Integration**:
```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
});
```

### Agent Telemetry Helper

**Create Telemetry Wrapper**:
```csharp
// Location: backend/src/CVAnalyzer.AgentService/AgentTelemetry.cs

public static class Telemetry
{
    public static readonly ActivitySource ActivitySource = new("CVAnalyzer.AgentService");
    public static readonly Meter Meter = new("CVAnalyzer.AgentService");
    
    // Metrics
    public static readonly Counter<long> AgentRunCounter = Meter.CreateCounter<long>(
        "agent.runs.total",
        description: "Total number of agent runs");
    
    public static readonly Histogram<double> AgentDurationHistogram = Meter.CreateHistogram<double>(
        "agent.run.duration",
        unit: "ms",
        description: "Agent run duration in milliseconds");
    
    public static readonly Counter<long> AgentErrorCounter = Meter.CreateCounter<long>(
        "agent.errors.total",
        description: "Total number of agent errors");
}
```

**Usage in Agents**:
```csharp
public async Task<AgentResult> RunAsync(AgentRequest request, CancellationToken ct)
{
    using var activity = Telemetry.ActivitySource.StartActivity("AgentRun");
    activity?.SetTag("agent.name", Name);
    activity?.SetTag("user.id", request.UserId);
    
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var result = await ExecuteAsync(request, ct);
        
        Telemetry.AgentRunCounter.Add(1, new TagList
        {
            { "agent.name", Name },
            { "success", result.Success }
        });
        
        return result;
    }
    catch (Exception ex)
    {
        Telemetry.AgentErrorCounter.Add(1, new TagList
        {
            { "agent.name", Name },
            { "error.type", ex.GetType().Name }
        });
        
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
    finally
    {
        stopwatch.Stop();
        Telemetry.AgentDurationHistogram.Record(
            stopwatch.Elapsed.TotalMilliseconds,
            new TagList { { "agent.name", Name } });
    }
}
```

**Key Metrics to Track**:
- Agent run count (success/failure)
- Agent execution duration (p50, p95, p99)
- Grounding query latency
- Model token usage and cost
- Error rates by agent type
- Concurrent agent executions

**Files to Create/Modify**:
- `backend/src/CVAnalyzer.API/Program.cs` (add OpenTelemetry)
- `backend/src/CVAnalyzer.AgentService/AgentStartup.cs` (add OpenTelemetry)
- `backend/src/CVAnalyzer.AgentService/AgentTelemetry.cs`
- Update all agents to use telemetry wrapper

---

## Security Best Practices

### Immediate Actions (Critical)

#### 1. Remove Terraform State from Repository

**Problem**: `terraform/terraform.tfstate*` files contain plaintext secrets.

**Solution**:
```powershell
# Step 1: Verify what's tracked
git ls-files terraform/*.tfstate*

# Step 2: Remove from tracking (does not remove from history)
git rm --cached terraform/terraform.tfstate*

# Step 3: Add to .gitignore
Add-Content -Path .gitignore -Value "`n# Terraform state files`nterraform/*.tfstate*`nterraform/.terraform/"

# Step 4: Commit the removal
git commit -m "security: remove terraform state files from tracking"

# Step 5: Clean history (COORDINATE WITH TEAM FIRST)
# Option A: git-filter-repo (recommended)
pip install git-filter-repo
git filter-repo --path terraform/terraform.tfstate --invert-paths
git filter-repo --path terraform/terraform.tfstate.backup --invert-paths

# Option B: BFG Repo Cleaner
# Download from https://rtyley.github.io/bfg-repo-cleaner/
java -jar bfg.jar --delete-files 'terraform.tfstate*'
```

**After History Rewrite**:
1. Force push to remote: `git push origin main --force`
2. Notify all team members to re-clone
3. Rotate all secrets that were in tfstate files:
   - SQL admin password
   - Service principal credentials
   - Storage account keys

#### 2. Configure Remote State Backend

**Create Terraform Backend Configuration**:
```hcl
# Location: terraform/backend.tf

terraform {
  backend "azurerm" {
    resource_group_name  = "rg-cvanalyzer-terraform-state"
    storage_account_name = "stcvanalyzertfstate"
    container_name       = "tfstate"
    key                  = "cvanalyzer.tfstate"
  }
}
```

**Setup Script**:
```powershell
# Create storage account for Terraform state
az group create --name rg-cvanalyzer-terraform-state --location eastus

az storage account create `
  --name stcvanalyzertfstate `
  --resource-group rg-cvanalyzer-terraform-state `
  --location eastus `
  --sku Standard_LRS `
  --encryption-services blob

az storage container create `
  --name tfstate `
  --account-name stcvanalyzertfstate
```

#### 3. Align CI and Local Secret Scanning

**Create Gitleaks Configuration**:
```toml
# Location: .gitleaks.toml

title = "CV Analyzer Security Scan"

[allowlist]
description = "Allowlist for false positives"
regexes = [
  '''<PASSWORD_PLACEHOLDER>''',
  '''example\.com''',
  '''localhost''',
  '''127\.0\.0\.1''',
]

paths = [
  '''\.md$''',  # Markdown documentation
  '''\.example$''',  # Example files
]

[[rules]]
id = "generic-api-key"
description = "Detect generic API keys"
regex = '''(?i)(api[_-]?key|apikey|api[_-]?token)\s*[:=]\s*['"][0-9a-zA-Z]{32,}['"]'''
tags = ["api", "key"]

[[rules]]
id = "connection-string"
description = "Detect connection strings"
regex = '''(?i)(server|data source|password|pwd|uid|user id)\s*=\s*[^;]+;'''
tags = ["database", "connection"]

[[rules]]
id = "azure-subscription-id"
description = "Detect Azure subscription IDs"
regex = '''[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}'''
tags = ["azure", "subscription"]
```

**Update GitHub Actions Workflow**:
```yaml
# Location: .github/workflows/security-scan.yml

name: Security Scan

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  gitleaks:
    name: Gitleaks Secret Scan
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      
      - name: Run Gitleaks
        uses: gitleaks/gitleaks-action@v2
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          GITLEAKS_CONFIG: .gitleaks.toml
```

### Key Vault Best Practices

**Development Environment**:
```json
// appsettings.Development.json
{
  "UseKeyVault": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CVAnalyzerDb;Trusted_Connection=True;"
  }
}
```

**Production Environment**:
```json
// appsettings.Production.json
{
  "UseKeyVault": true,
  "KeyVault": {
    "Uri": "https://kv-cvanalyzer-prod.vault.azure.net/"
  }
  // No connection strings or secrets here
}
```

**Access Control**:
- Use Managed Identity for app services
- Grant least-privilege access (Secret Get, not List/Set)
- Enable audit logging for secret access
- Rotate secrets every 90 days

---

## Deployment & Containerization

### Azure Container Apps Configuration

**Dockerfile Optimization**:
```dockerfile
# Location: backend/Dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Non-root user
RUN groupadd -r cvanalyzer && useradd -r -g cvanalyzer cvanalyzer
USER cvanalyzer

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/CVAnalyzer.API/CVAnalyzer.API.csproj", "CVAnalyzer.API/"]
COPY ["src/CVAnalyzer.Application/CVAnalyzer.Application.csproj", "CVAnalyzer.Application/"]
COPY ["src/CVAnalyzer.Domain/CVAnalyzer.Domain.csproj", "CVAnalyzer.Domain/"]
COPY ["src/CVAnalyzer.Infrastructure/CVAnalyzer.Infrastructure.csproj", "CVAnalyzer.Infrastructure/"]
COPY ["src/CVAnalyzer.AgentService/CVAnalyzer.AgentService.csproj", "CVAnalyzer.AgentService/"]
RUN dotnet restore "CVAnalyzer.API/CVAnalyzer.API.csproj"

COPY src/ .
WORKDIR "/src/CVAnalyzer.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "CVAnalyzer.API.dll"]
```

### Terraform Module for Container Apps

```hcl
# Location: terraform/modules/container-apps/main.tf

resource "azurerm_container_app_environment" "main" {
  name                       = "cae-cvanalyzer-${var.environment}"
  location                   = var.location
  resource_group_name        = var.resource_group_name
  log_analytics_workspace_id = var.log_analytics_workspace_id
}

resource "azurerm_container_app" "api" {
  name                         = "ca-cvanalyzer-api-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = var.resource_group_name
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  template {
    container {
      name   = "api"
      image  = "${var.acr_name}.azurecr.io/cvanalyzer-api:${var.image_tag}"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name        = "ASPNETCORE_ENVIRONMENT"
        value       = var.environment
      }

      env {
        name        = "UseKeyVault"
        value       = "true"
      }

      env {
        name        = "KeyVault__Uri"
        value       = var.key_vault_uri
      }

      # Key Vault reference for secrets
      env {
        name  = "ConnectionStrings__DefaultConnection"
        secret_name = "database-connection-string"
      }
    }

    min_replicas = var.min_replicas
    max_replicas = var.max_replicas
  }

  secret {
    name  = "database-connection-string"
    key_vault_secret_id = "${var.key_vault_uri}/secrets/DatabaseConnectionString"
    identity            = "system"
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    
    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  dapr {
    enabled       = false
    app_id        = "cvanalyzer-api"
    app_protocol  = "http"
    app_port      = 8080
  }
}

# Grant Key Vault access to managed identity
resource "azurerm_key_vault_access_policy" "container_app" {
  key_vault_id = var.key_vault_id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_container_app.api.identity[0].principal_id

  secret_permissions = [
    "Get",
  ]
}
```

### CI/CD Pipeline Structure

```yaml
# Location: .github/workflows/deploy.yml

name: Build and Deploy

on:
  push:
    branches: [main]
  workflow_dispatch:

env:
  ACR_NAME: acrcvanalyzer
  IMAGE_NAME: cvanalyzer-api

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 9.0.x
      
      - name: Restore dependencies
        run: dotnet restore backend/CVAnalyzer.sln
      
      - name: Build
        run: dotnet build backend/CVAnalyzer.sln --configuration Release --no-restore
      
      - name: Run unit tests
        run: dotnet test backend/tests/CVAnalyzer.UnitTests --no-build --verbosity normal
      
      - name: Run integration tests
        run: dotnet test backend/tests/CVAnalyzer.IntegrationTests --no-build --verbosity normal
      
      - name: Log in to Azure Container Registry
        uses: azure/docker-login@v1
        with:
          login-server: ${{ env.ACR_NAME }}.azurecr.io
          username: ${{ secrets.ACR_USERNAME }}
          password: ${{ secrets.ACR_PASSWORD }}
      
      - name: Build and push Docker image
        run: |
          docker build -t ${{ env.ACR_NAME }}.azurecr.io/${{ env.IMAGE_NAME }}:${{ github.sha }} -f backend/Dockerfile backend/
          docker push ${{ env.ACR_NAME }}.azurecr.io/${{ env.IMAGE_NAME }}:${{ github.sha }}
  
  deploy-dev:
    needs: build
    runs-on: ubuntu-latest
    environment: development
    steps:
      - uses: actions/checkout@v4
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy to Container Apps (Dev)
        run: |
          az containerapp update \
            --name ca-cvanalyzer-api-dev \
            --resource-group rg-cvanalyzer-dev \
            --image ${{ env.ACR_NAME }}.azurecr.io/${{ env.IMAGE_NAME }}:${{ github.sha }}
```

---

## Feature Roadmap

### Sprint 0: Foundation (Week 1)

**Priority**: Critical - Security & DX

- [ ] **Task 1.1**: Remove Terraform state from Git history
  - Action: Use git-filter-repo to clean history
  - Owner: DevOps Lead
  - Files: `terraform/*.tfstate*`
  - Acceptance: No tfstate files in Git history, secrets rotated

- [ ] **Task 1.2**: Configure remote Terraform backend
  - Action: Create Azure Storage backend, update `backend.tf`
  - Owner: DevOps Lead
  - Files: `terraform/backend.tf`
  - Acceptance: Terraform init works with remote backend

- [ ] **Task 1.3**: Align CI and pre-commit secret scanning
  - Action: Add `.gitleaks.toml`, update GitHub Actions
  - Owner: DevOps Lead
  - Files: `.gitleaks.toml`, `.github/workflows/security-scan.yml`
  - Acceptance: Local and CI scans produce consistent results

- [ ] **Task 1.4**: Enhance Key Vault integration
  - Action: Extend `DependencyInjection.cs` with comprehensive Key Vault retrieval
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs`
  - Acceptance: All secrets loaded from Key Vault in production

**Deliverables**: Secure repository, no secrets committed, production secrets in Key Vault

---

### Sprint 1: Observability (Weeks 2-3)

**Priority**: High - Foundation for monitoring

- [ ] **Task 2.1**: Add OpenTelemetry to API
  - Action: Configure tracing, metrics, and exporters in `Program.cs`
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.API/Program.cs`
  - Acceptance: Traces visible in Application Insights

- [ ] **Task 2.2**: Add OpenTelemetry to AgentService
  - Action: Configure telemetry in `AgentStartup.cs`
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/AgentStartup.cs`
  - Acceptance: Agent traces propagate to Application Insights

- [ ] **Task 2.3**: Create AgentTelemetry helper
  - Action: Implement centralized telemetry wrapper
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/AgentTelemetry.cs`
  - Acceptance: Metrics for agent runs, duration, errors

- [ ] **Task 2.4**: Instrument existing agents
  - Action: Add telemetry to `ResumeAnalysisAgent`
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/ResumeAnalysisAgent.cs`
  - Acceptance: Full trace for resume analysis workflow

**Deliverables**: End-to-end tracing, key metrics dashboards, Application Insights configured

---

### Sprint 2: Grounding & Search (Weeks 4-6)

**Priority**: High - Enables RAG pattern

- [ ] **Task 3.1**: Implement Azure AI Search service wrapper
  - Action: Create `AzureSearchService.cs` with index/query methods
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.Infrastructure/Services/AI/AzureSearchService.cs`
  - Acceptance: Can index and query resume documents

- [ ] **Task 3.2**: Create resume indexing pipeline
  - Action: Add indexing step after resume upload
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.Application/Features/Resumes/Commands/UploadResumeCommandHandler.cs`
  - Acceptance: New resumes automatically indexed

- [ ] **Task 3.3**: Implement AgentGrounder component
  - Action: Create grounder that queries Azure Search
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/AgentGrounder.cs`
  - Acceptance: Returns relevant context documents for query

- [ ] **Task 3.4**: Integrate grounding with agents
  - Action: Modify `ResumeAnalysisAgent` to use grounding
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/ResumeAnalysisAgent.cs`
  - Acceptance: Agent decisions informed by retrieved context

- [ ] **Task 3.5**: Add unit and integration tests
  - Action: Test search indexing and grounding
  - Owner: Backend Dev
  - Files: `backend/tests/CVAnalyzer.UnitTests/Services/AzureSearchServiceTests.cs`
  - Acceptance: 80%+ code coverage for search components

**Deliverables**: Working RAG pipeline, semantic search over resumes, grounding integration

---

### Sprint 3: Agent Orchestration (Weeks 7-10)

**Priority**: Medium-High - Multi-agent architecture

- [ ] **Task 4.1**: Define agent interfaces
  - Action: Create `IAgent`, `IAgentOrchestrator` interfaces
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/IAgent.cs`, `IAgentOrchestrator.cs`
  - Acceptance: Clear contract for agent implementations

- [ ] **Task 4.2**: Implement AgentOrchestrator
  - Action: Create orchestrator with sequential agent execution
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/AgentOrchestrator.cs`
  - Acceptance: Can execute multiple agents in workflow

- [ ] **Task 4.3**: Create ResumeParserAgent
  - Action: Extract structured data (skills, experience, education)
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/Agents/ResumeParserAgent.cs`
  - Acceptance: Returns parsed sections and metadata

- [ ] **Task 4.4**: Create ScoreAgent
  - Action: Calculate ATS score based on parsed data
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/Agents/ScoreAgent.cs`
  - Acceptance: Produces 0-100 score with reasoning

- [ ] **Task 4.5**: Create SuggestionAgent
  - Action: Generate improvement suggestions
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.AgentService/Agents/SuggestionAgent.cs`
  - Acceptance: Returns categorized, prioritized suggestions

- [ ] **Task 4.6**: Wire orchestrator into API
  - Action: Update ResumesController to use orchestrator
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.API/Controllers/ResumesController.cs`
  - Acceptance: API endpoint uses multi-agent workflow

- [ ] **Task 4.7**: Add comprehensive tests
  - Action: Test each agent and orchestration flow
  - Owner: Backend Dev
  - Files: `backend/tests/CVAnalyzer.IntegrationTests/Agents/`
  - Acceptance: End-to-end tests pass

**Deliverables**: Multi-agent orchestration, specialized agents (parse, score, suggest), integration tests

---

### Sprint 4: Containerization & Deployment (Weeks 11-13)

**Priority**: Medium - Production readiness

- [ ] **Task 5.1**: Optimize Dockerfiles
  - Action: Multi-stage builds, non-root user, health checks
  - Owner: DevOps Lead
  - Files: `backend/Dockerfile`, `ai-service/Dockerfile`
  - Acceptance: Images < 200MB, build time < 5 min

- [ ] **Task 5.2**: Create Container Apps Terraform module
  - Action: Define ACA resources with managed identity
  - Owner: DevOps Lead
  - Files: `terraform/modules/container-apps/main.tf`
  - Acceptance: Terraform apply creates ACA environment

- [ ] **Task 5.3**: Setup Azure Container Registry
  - Action: Create ACR, configure access policies
  - Owner: DevOps Lead
  - Files: `terraform/modules/acr/main.tf`
  - Acceptance: Can push/pull images via CI/CD

- [ ] **Task 5.4**: Build CI/CD pipeline
  - Action: GitHub Actions for build, test, push, deploy
  - Owner: DevOps Lead
  - Files: `.github/workflows/deploy.yml`
  - Acceptance: Automated deployment to dev/test/prod

- [ ] **Task 5.5**: Configure autoscaling rules
  - Action: CPU/memory-based scaling, min/max replicas
  - Owner: DevOps Lead
  - Files: `terraform/modules/container-apps/main.tf`
  - Acceptance: Scales under load

**Deliverables**: Containerized apps, automated CI/CD, deployed to Azure Container Apps

---

### Sprint 5: Omnichannel & Integrations (Weeks 14-16)

**Priority**: Medium - Extended reach

- [ ] **Task 6.1**: Add Bot Framework SDK
  - Action: Install packages, configure bot adapter
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.API/CVAnalyzer.API.csproj`
  - Acceptance: Bot SDK integrated

- [ ] **Task 6.2**: Create BotsController
  - Action: Endpoint for bot messages
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.API/Controllers/BotsController.cs`
  - Acceptance: Receives messages from Bot Service

- [ ] **Task 6.3**: Implement bot adapter service
  - Action: Convert bot messages to agent requests
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.Infrastructure/Services/BotAdapterService.cs`
  - Acceptance: Bot messages trigger agent workflows

- [ ] **Task 6.4**: Deploy Azure Bot Service
  - Action: Create bot resource, configure channels
  - Owner: DevOps Lead
  - Files: `terraform/modules/bot-service/main.tf`
  - Acceptance: Bot accessible in Teams

- [ ] **Task 6.5**: Add Slack integration
  - Action: Configure Slack channel in Bot Service
  - Owner: DevOps Lead
  - Acceptance: Bot responds in Slack

**Deliverables**: Bot Service integration, Teams and Slack channels

---

### Sprint 6: Advanced Features (Weeks 17-20)

**Priority**: Low-Medium - Nice-to-have

- [ ] **Task 7.1**: Add Cosmos DB for conversation storage
  - Action: Implement `CosmosConversationStore`
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.Infrastructure/Persistence/CosmosConversationStore.cs`
  - Acceptance: Conversations persisted to Cosmos

- [ ] **Task 7.2**: Implement Bing Search grounding
  - Action: Add Bing Search API client
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.Infrastructure/Services/AI/BingSearchService.cs`
  - Acceptance: Agents can retrieve web context

- [ ] **Task 7.3**: Add Logic Apps integration
  - Action: Trigger workflows on resume events
  - Owner: DevOps Lead
  - Files: `terraform/modules/logic-apps/main.tf`
  - Acceptance: Email notifications on resume upload

- [ ] **Task 7.4**: Implement audit logging
  - Action: Track all agent actions with user context
  - Owner: Backend Dev
  - Files: `backend/src/CVAnalyzer.Infrastructure/Services/AuditService.cs`
  - Acceptance: Audit logs queryable in Application Insights

**Deliverables**: Cosmos DB storage, web grounding, workflow automation, audit logs

---

## Implementation Checklist

### Phase 1: Security & Foundation (Immediate)

- [ ] Remove Terraform state files from Git history
- [ ] Rotate all secrets that were in tracked tfstate files
- [ ] Configure remote Terraform backend (Azure Storage)
- [ ] Update `.gitignore` to prevent future tfstate commits
- [ ] Create `.gitleaks.toml` with allowlist for placeholders
- [ ] Update GitHub Actions to use Gitleaks with config
- [ ] Verify pre-commit and CI secret scans are aligned
- [ ] Extend Key Vault integration in `DependencyInjection.cs`
- [ ] Update production `appsettings.json` to remove secrets
- [ ] Document Key Vault usage for development team

### Phase 2: Observability (Weeks 1-2)

- [ ] Add OpenTelemetry packages to API and AgentService
- [ ] Configure tracing in `Program.cs` and `AgentStartup.cs`
- [ ] Create `AgentTelemetry.cs` helper class
- [ ] Instrument `ResumeAnalysisAgent` with traces and metrics
- [ ] Configure Application Insights exporter
- [ ] Create initial Application Insights dashboards
- [ ] Set up alerts for critical errors and high latency
- [ ] Document telemetry usage patterns for team

### Phase 3: Grounding & Search (Weeks 3-6)

- [ ] Provision Azure AI Search service via Terraform
- [ ] Create `IAzureSearchService` interface
- [ ] Implement `AzureSearchService.cs` with index/query
- [ ] Update `UploadResumeCommandHandler` to index resumes
- [ ] Create `IAgentGrounder` interface
- [ ] Implement `AgentGrounder.cs` using Azure Search
- [ ] Integrate grounder into `ResumeAnalysisAgent`
- [ ] Write unit tests for `AzureSearchService`
- [ ] Write integration tests for grounding flow
- [ ] Update documentation with RAG pattern details

### Phase 4: Agent Orchestration (Weeks 7-10)

- [ ] Define `IAgent` interface with standard contract
- [ ] Define `IAgentOrchestrator` interface
- [ ] Implement `AgentOrchestrator.cs` with sequential execution
- [ ] Create `ResumeParserAgent` for data extraction
- [ ] Create `ScoreAgent` for ATS scoring
- [ ] Create `SuggestionAgent` for improvements
- [ ] Register all agents in `AgentStartup.cs`
- [ ] Update `ResumesController` to use orchestrator
- [ ] Write unit tests for each agent
- [ ] Write integration tests for orchestration flow
- [ ] Add telemetry to all agents
- [ ] Update API documentation

### Phase 5: Deployment (Weeks 11-13)

- [ ] Optimize `backend/Dockerfile` (multi-stage, non-root)
- [ ] Optimize `ai-service/Dockerfile`
- [ ] Create Terraform module for Azure Container Registry
- [ ] Create Terraform module for Container Apps Environment
- [ ] Create Terraform module for Container App instances
- [ ] Configure managed identity for Container Apps
- [ ] Grant Key Vault access to managed identities
- [ ] Create GitHub Actions workflow for build
- [ ] Create GitHub Actions workflow for test
- [ ] Create GitHub Actions workflow for Docker build/push
- [ ] Create GitHub Actions workflow for deployment
- [ ] Configure environment-specific deployments (dev/test/prod)
- [ ] Add health checks to containers
- [ ] Configure autoscaling rules
- [ ] Test deployment pipeline end-to-end

### Phase 6: Advanced Features (Weeks 14+)

- [ ] Implement Cosmos DB conversation store
- [ ] Add Bing Search grounding service
- [ ] Create Bot Framework integration
- [ ] Deploy Azure Bot Service via Terraform
- [ ] Configure Teams channel
- [ ] Configure Slack channel
- [ ] Add Logic Apps for workflow automation
- [ ] Implement audit logging service
- [ ] Create governance dashboards
- [ ] Document omnichannel usage

---

## Quick Reference

### Key Configuration Files

| Purpose | File Path |
|---------|-----------|
| API startup | `backend/src/CVAnalyzer.API/Program.cs` |
| Agent startup | `backend/src/CVAnalyzer.AgentService/AgentStartup.cs` |
| Infrastructure DI | `backend/src/CVAnalyzer.Infrastructure/DependencyInjection.cs` |
| Application DI | `backend/src/CVAnalyzer.Application/DependencyInjection.cs` |
| API settings (dev) | `backend/src/CVAnalyzer.API/appsettings.Development.json` |
| API settings (prod) | `backend/src/CVAnalyzer.API/appsettings.Production.json` |
| Docker compose | `docker-compose.yml` |
| Backend Dockerfile | `backend/Dockerfile` |
| AI service Dockerfile | `ai-service/Dockerfile` |
| Terraform main | `terraform/main.tf` |
| Gitleaks config | `.gitleaks.toml` |
| Pre-commit hook | `.git/hooks/pre-commit.ps1` |

### Azure Resources Naming Convention

Follow this pattern: `{resource-type}-cvanalyzer-{environment}`

- Resource Group: `rg-cvanalyzer-{env}`
- App Service: `app-cvanalyzer-{env}`
- Container Registry: `acrcvanalyzer{env}`
- Container App Environment: `cae-cvanalyzer-{env}`
- Container App: `ca-cvanalyzer-api-{env}`
- SQL Server: `sql-cvanalyzer-{env}`
- Key Vault: `kv-cvanalyzer-{env}`
- Storage Account: `stcvanalyzer{env}`
- AI Search: `srch-cvanalyzer-{env}`
- Cosmos DB: `cosmos-cvanalyzer-{env}`
- Application Insights: `appi-cvanalyzer-{env}`

### Useful Commands

**Development**:
```powershell
# Run backend locally
cd backend/src/CVAnalyzer.API
dotnet run

# Run frontend locally
cd frontend
npm start

# Run tests
cd backend
dotnet test

# Docker compose (full stack)
docker-compose up -d
```

**Terraform**:
```powershell
cd terraform
terraform init
terraform plan -var-file="environments/dev.tfvars"
terraform apply -var-file="environments/dev.tfvars"
```

**Azure CLI**:
```powershell
# Login
az login

# Set subscription
az account set --subscription <subscription-id>

# View Container App logs
az containerapp logs show --name ca-cvanalyzer-api-dev --resource-group rg-cvanalyzer-dev

# Scale Container App
az containerapp update --name ca-cvanalyzer-api-dev --resource-group rg-cvanalyzer-dev --min-replicas 2 --max-replicas 10
```

---

## Additional Resources

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/azure/ai-studio/how-to/develop/agents)
- [Azure AI Foundry](https://learn.microsoft.com/azure/ai-studio/)
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Azure AI Search](https://learn.microsoft.com/azure/search/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

**Document Status**: Living document - update as architecture evolves  
**Maintainer**: Development Team  
**Review Cadence**: Monthly or after major feature additions
