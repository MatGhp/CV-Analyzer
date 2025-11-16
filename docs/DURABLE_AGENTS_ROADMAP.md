# Durable Agents Roadmap

## Overview
This document outlines the strategic path to upgrade CV Analyzer's AI agent implementation from the current Azure.AI.OpenAI SDK approach to Microsoft Agent Framework's **Durable Agents** pattern, enabling stateful, fault-tolerant, and scalable agent orchestrations.

**Current State**: Azure.AI.OpenAI SDK with custom queue-based background processing  
**Target State**: Microsoft Agent Framework Durable Agents with Azure Functions  
**Migration Complexity**: Medium (3-4 weeks)  
**Business Value**: High (improved reliability, scalability, observability)

---

## Why Durable Agents?

### Current Architecture Limitations

**Our Current Implementation** (`backend/src/CVAnalyzer.AgentService/`):
```csharp
// Single-shot analysis with no conversation state
public async Task<ResumeAnalysisResponse> AnalyzeAsync(
    ResumeAnalysisRequest request, 
    CancellationToken ct)
{
    var chatCompletionOptions = new ChatCompletionOptions
    {
        Temperature = _options.Temperature,
        TopP = _options.TopP
    };
    
    var response = await _chatClient.CompleteChatAsync(...);
    // No state persistence, no conversation history
}
```

**Limitations**:
1. ❌ **No Conversation State**: Each analysis is stateless, can't do iterative refinement
2. ❌ **Manual Queue Management**: Custom `ResumeAnalysisWorker` + Azure Storage Queues
3. ❌ **No Built-in Retry Logic**: Must implement custom retry policies
4. ❌ **Limited Observability**: Custom logging, no visual workflow debugging
5. ❌ **No Multi-Agent Orchestration**: Can't coordinate multiple specialized agents
6. ❌ **Process Crashes Lose Context**: If worker dies, must restart from scratch

### Durable Agents Benefits

**Key Features from Microsoft Agent Framework**:

| Feature | Current Implementation | With Durable Agents |
|---------|----------------------|---------------------|
| **Stateful Threads** | ❌ None (stateless) | ✅ Automatic conversation persistence |
| **Fault Tolerance** | ⚠️ Queue retry only | ✅ Deterministic orchestrations survive crashes |
| **Scaling** | ⚠️ Manual worker scaling | ✅ Azure Functions Flex Consumption (0-1000s instances) |
| **Observability** | ⚠️ Serilog + custom traces | ✅ Built-in Durable Task Scheduler dashboard |
| **Multi-Agent Coordination** | ❌ Not supported | ✅ Sequential, parallel, human-in-the-loop patterns |
| **Long-Running Operations** | ⚠️ Limited by queue visibility timeout | ✅ Days/weeks with automatic checkpointing |
| **HTTP Endpoints** | ⚠️ Manual controller implementation | ✅ Auto-generated agent endpoints |
| **Event-Driven** | ⚠️ Queue triggers only | ✅ All Azure Functions triggers (HTTP, Timer, Blob, etc.) |

---

## Architecture Comparison

### Current Architecture (v1.0)

```
┌─────────────────────────────────────────────────────────────┐
│ Frontend (Angular 20)                                        │
│ - Upload resume → POST /api/resumes/upload                  │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTP
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ CVAnalyzer.API (.NET 10)                                     │
│ ┌─────────────────────────────────────────────────────┐    │
│ │ ResumesController                                    │    │
│ │ - Validate file                                      │    │
│ │ - Create Resume entity (Status: Pending)            │    │
│ │ - Upload blob (IBlobStorageService)                 │    │
│ │ - Enqueue analysis (IResumeQueueService)            │◄───┐│
│ │ - Return resume ID                                   │    ││
│ └─────────────────────────────────────────────────────┘    ││
└────────────────────────┬────────────────────────────────────┘│
                         │                                      │
                         │ Azure Storage Queue                  │
                         │ (resume-analysis)                    │
                         ▼                                      │
┌─────────────────────────────────────────────────────────────┐│
│ ResumeAnalysisWorker (BackgroundService)                    ││
│ ┌─────────────────────────────────────────────────────┐    ││
│ │ Poll queue → Process message                        │    ││
│ │ ├─ Extract text (Document Intelligence)            │    ││
│ │ ├─ Analyze (ResumeAnalysisAgent)                   │    ││
│ │ ├─ Update Resume entity (Score, Suggestions)       │    ││
│ │ └─ Delete queue message                             │    ││
│ └─────────────────────────────────────────────────────┘    ││
│                                                              ││
│ ┌─────────────────────────────────────────────────────┐    ││
│ │ ResumeAnalysisAgent (Azure.AI.OpenAI SDK)           │    ││
│ │ - Single-shot analysis                               │    ││
│ │ - No conversation state                              │    ││
│ │ - Returns ResumeAnalysisResponse                    │    ││
│ └─────────────────────────────────────────────────────┘    ││
└─────────────────────────────────────────────────────────────┘│
                         │                                      │
                         │ If error                             │
                         └──────────────────────────────────────┘
                         Retry (manual exponential backoff)
```

**Pain Points**:
- Manual state management (Resume entity status tracking)
- Custom retry logic in worker
- No visibility into processing pipeline
- Can't do multi-turn conversations (e.g., "improve this suggestion")
- Difficult to add new agent types (requires new workers/queues)

---

### Future Architecture (v2.0 with Durable Agents)

```
┌─────────────────────────────────────────────────────────────┐
│ Frontend (Angular 20)                                        │
│ - Upload resume → POST /api/resumes/upload                  │
│ - Chat with agent → POST /api/agents/ResumeAnalyzer/run    │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTP
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ CVAnalyzer.API (.NET 10) - Orchestrator Layer               │
│ ┌─────────────────────────────────────────────────────┐    │
│ │ ResumesController (lightweight)                      │    │
│ │ - Validate file                                      │    │
│ │ - Upload blob                                        │    │
│ │ - Start durable orchestration                        │    │
│ │ - Return resume ID + thread ID                      │    │
│ └─────────────────────────────────────────────────────┘    │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTP (internal)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ Azure Functions (Durable Agents) - Agent Layer              │
│                                                              │
│ ┌──────────────────────────────────────────────────────┐   │
│ │ Durable Agent: ResumeAnalyzer                        │   │
│ │ - Stateful conversation threads                      │   │
│ │ - Auto-generated HTTP endpoints                      │   │
│ │ - Persistent conversation history                    │   │
│ └──────────────────────────────────────────────────────┘   │
│                                                              │
│ ┌──────────────────────────────────────────────────────┐   │
│ │ Durable Orchestration: ResumeProcessingOrchestrator │   │
│ │                                                       │   │
│ │ 1. ExtractText (Document Intelligence)               │   │
│ │    ├─ Retry on failure (automatic)                   │   │
│ │    └─ Checkpoint after success                       │   │
│ │                                                       │   │
│ │ 2. AnalyzeResume (ResumeAnalyzer agent)              │   │
│ │    ├─ Retry on failure (automatic)                   │   │
│ │    └─ Checkpoint after success                       │   │
│ │                                                       │   │
│ │ 3. GenerateSuggestions (SuggestionAgent)             │   │
│ │    ├─ Parallel calls for different categories        │   │
│ │    └─ Checkpoint after success                       │   │
│ │                                                       │   │
│ │ 4. OptimizeContent (OptimizationAgent)               │   │
│ │    └─ Optional human-in-the-loop approval            │   │
│ │                                                       │   │
│ │ 5. SaveResults (Database update)                     │   │
│ │    └─ Final checkpoint                                │   │
│ └──────────────────────────────────────────────────────┘   │
│                                                              │
│ ┌──────────────────────────────────────────────────────┐   │
│ │ Durable Task Scheduler                                │   │
│ │ - State persistence (Azure Storage)                   │   │
│ │ - Automatic retries with exponential backoff          │   │
│ │ - Visual debugging dashboard                          │   │
│ │ - Distributed execution across instances              │   │
│ └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                         │
                         │ Event Triggers (future)
                         │
         ┌───────────────┼───────────────┐
         │               │               │
    Blob Trigger    Timer Trigger   Queue Trigger
   (new resumes)   (scheduled jobs) (priority queue)
```

**Improvements**:
- ✅ Automatic state management via Durable Task Framework
- ✅ Built-in retry logic with exponential backoff
- ✅ Visual debugging via Durable Task Scheduler dashboard
- ✅ Multi-agent orchestration (specialized agents for different tasks)
- ✅ Event-driven triggers (blob upload, timer, queue, HTTP)
- ✅ Fault-tolerant checkpointing (survive crashes/restarts)
- ✅ Conversational continuity (multi-turn refinement)

---

## Migration Strategy

### Phase 1: Foundation (Week 1)

**Goal**: Set up Azure Functions project with durable agent hosting

**Tasks**:
1. **Create Azure Functions Project**
   ```bash
   cd backend/src
   dotnet new func -n CVAnalyzer.AgentFunctions --worker-runtime dotnet-isolated
   cd CVAnalyzer.AgentFunctions
   ```

2. **Add Required NuGet Packages**
   ```bash
   dotnet add package Azure.AI.OpenAI --prerelease
   dotnet add package Azure.Identity
   dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
   dotnet add package Microsoft.Agents.AI.Hosting.AzureFunctions --prerelease
   dotnet add package Microsoft.Azure.Functions.Worker --version 2.2.0
   ```

3. **Create Basic Durable Agent**
   ```csharp
   // Program.cs
   using Azure.AI.OpenAI;
   using Azure.Identity;
   using Microsoft.Agents.AI;
   using Microsoft.Agents.AI.Hosting.AzureFunctions;
   using Microsoft.Azure.Functions.Worker.Builder;
   using Microsoft.Extensions.Hosting;
   
   var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
   var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? "gpt-4o";
   
   AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
       .GetChatClient(deploymentName)
       .CreateAIAgent(
           instructions: @"You are an expert resume analyzer specializing in ATS optimization.
                          Analyze resumes for clarity, impact, keyword optimization, and formatting.
                          Provide actionable suggestions with priority levels.",
           name: "ResumeAnalyzer");
   
   using IHost app = FunctionsApplication
       .CreateBuilder(args)
       .ConfigureFunctionsWebApplication()
       .ConfigureDurableAgents(options =>
           options.AddAIAgent(agent)
       )
       .Build();
   app.Run();
   ```

4. **Update Infrastructure (Terraform)**
   - Add Azure Functions resource (Flex Consumption plan)
   - Configure managed identity for OpenAI access
   - Set up Application Insights for monitoring

**Deliverables**:
- ✅ Working Azure Functions project with durable agent
- ✅ Deployed to Azure with auto-generated HTTP endpoints
- ✅ Basic conversation testing via curl/Postman

---

### Phase 2: Orchestration (Week 2)

**Goal**: Implement multi-step durable orchestration for resume processing

**Tasks**:
1. **Create Orchestration Function**
   ```csharp
   [Function("ResumeProcessingOrchestrator")]
   public async Task<ResumeAnalysisResult> RunOrchestrator(
       [OrchestrationTrigger] TaskOrchestrationContext context)
   {
       var input = context.GetInput<ResumeProcessingInput>();
       
       try
       {
           // Step 1: Extract text from resume (with retry)
           var extractedText = await context.CallActivityAsync<string>(
               "ExtractResumeText", 
               input.BlobUrl);
           
           // Step 2: Analyze resume with agent (with retry)
           var analysis = await context.CallAgentAsync<ResumeAnalysisResponse>(
               "ResumeAnalyzer",
               new { Content = extractedText, UserId = input.UserId });
           
           // Step 3: Generate suggestions in parallel
           var suggestionTasks = new[]
           {
               context.CallAgentAsync<List<Suggestion>>(
                   "SuggestionAgent", 
                   new { Category = "Technical", Content = extractedText }),
               context.CallAgentAsync<List<Suggestion>>(
                   "SuggestionAgent", 
                   new { Category = "Formatting", Content = extractedText }),
               context.CallAgentAsync<List<Suggestion>>(
                   "SuggestionAgent", 
                   new { Category = "ATS", Content = extractedText })
           };
           var suggestions = (await Task.WhenAll(suggestionTasks)).SelectMany(s => s).ToList();
           
           // Step 4: Save results to database
           await context.CallActivityAsync(
               "SaveResumeAnalysis",
               new { ResumeId = input.ResumeId, Analysis = analysis, Suggestions = suggestions });
           
           return new ResumeAnalysisResult 
           { 
               ResumeId = input.ResumeId, 
               Score = analysis.Score,
               Status = "Completed" 
           };
       }
       catch (Exception ex)
       {
           // Automatic retry with exponential backoff
           // Update status to failed after max retries
           await context.CallActivityAsync(
               "UpdateResumeStatus",
               new { ResumeId = input.ResumeId, Status = "Failed", Error = ex.Message });
           
           throw;
       }
   }
   ```

2. **Implement Activity Functions**
   - `ExtractResumeText`: Document Intelligence integration
   - `SaveResumeAnalysis`: Database persistence
   - `UpdateResumeStatus`: Status updates

3. **Update API Controller**
   ```csharp
   [HttpPost("upload")]
   public async Task<IActionResult> Upload(
       [FromForm] IFormFile file,
       [FromForm] string? userId,
       [FromServices] DurableTaskClient durableClient,
       CancellationToken ct)
   {
       // Validate and upload blob (existing logic)
       var blobUrl = await _blobService.UploadAsync(file.OpenReadStream(), fileName, ct);
       
       // Create Resume entity
       var resume = new Resume { /* ... */ };
       _context.Resumes.Add(resume);
       await _context.SaveChangesAsync(ct);
       
       // Start durable orchestration instead of queue
       var instanceId = await durableClient.ScheduleNewOrchestrationInstanceAsync(
           "ResumeProcessingOrchestrator",
           new ResumeProcessingInput 
           { 
               ResumeId = resume.Id, 
               BlobUrl = blobUrl, 
               UserId = userId ?? "anonymous" 
           });
       
       return CreatedAtAction(
           nameof(GetResume), 
           new { id = resume.Id }, 
           new { id = resume.Id, orchestrationId = instanceId });
   }
   ```

**Deliverables**:
- ✅ Multi-step orchestration with automatic retries
- ✅ Parallel agent execution for suggestions
- ✅ Integration with existing API and database

---

### Phase 3: Conversational Refinement (Week 3)

**Goal**: Enable multi-turn conversations for iterative resume improvement

**Tasks**:
1. **Add Chat Endpoint**
   ```csharp
   [HttpPost("resumes/{resumeId}/chat")]
   public async Task<IActionResult> ChatWithAgent(
       Guid resumeId,
       [FromBody] ChatRequest request,
       [FromServices] IDurableAgentClient agentClient,
       CancellationToken ct)
   {
       // Get resume and thread ID
       var resume = await _context.Resumes
           .Include(r => r.Suggestions)
           .FirstOrDefaultAsync(r => r.Id == resumeId, ct);
       
       if (resume == null) return NotFound();
       
       // Continue or start thread
       var threadId = request.ThreadId ?? await agentClient.CreateThreadAsync("ResumeAnalyzer");
       
       // Send message with full context
       var response = await agentClient.SendMessageAsync(
           "ResumeAnalyzer",
           threadId,
           $"Resume Score: {resume.Score}\n" +
           $"Current Suggestions: {JsonSerializer.Serialize(resume.Suggestions)}\n" +
           $"User Question: {request.Message}");
       
       return Ok(new { threadId, message = response });
   }
   ```

2. **Update Frontend**
   - Add chat interface component
   - Display conversation history
   - Enable iterative refinement ("improve this suggestion", "explain this score")

3. **Implement Specialized Agents**
   ```csharp
   // SuggestionAgent - generates targeted suggestions
   AIAgent suggestionAgent = client.CreateAIAgent(
       instructions: "Generate specific, actionable resume suggestions for the given category.",
       name: "SuggestionAgent");
   
   // OptimizationAgent - improves resume content
   AIAgent optimizationAgent = client.CreateAIAgent(
       instructions: "Optimize resume content for ATS compatibility and impact.",
       name: "OptimizationAgent");
   
   // Configure all agents
   options.AddAIAgent(agent);
   options.AddAIAgent(suggestionAgent);
   options.AddAIAgent(optimizationAgent);
   ```

**Deliverables**:
- ✅ Chat interface for conversational refinement
- ✅ Persistent conversation threads per resume
- ✅ Multiple specialized agents with distinct responsibilities

---

### Phase 4: Observability & Scaling (Week 4)

**Goal**: Production-ready monitoring and auto-scaling

**Tasks**:
1. **Configure Durable Task Scheduler Dashboard**
   - Enable in Azure Functions settings
   - Configure Application Insights integration
   - Set up alerts for failed orchestrations

2. **Implement Monitoring**
   ```csharp
   // Add telemetry to orchestrations
   [Function("ResumeProcessingOrchestrator")]
   public async Task<ResumeAnalysisResult> RunOrchestrator(
       [OrchestrationTrigger] TaskOrchestrationContext context,
       FunctionContext functionContext)
   {
       var logger = functionContext.GetLogger("ResumeProcessingOrchestrator");
       var input = context.GetInput<ResumeProcessingInput>();
       
       logger.LogInformation("Starting resume processing for {ResumeId}", input.ResumeId);
       
       var stopwatch = Stopwatch.StartNew();
       
       try
       {
           // Orchestration steps...
           
           stopwatch.Stop();
           logger.LogInformation(
               "Resume processing completed for {ResumeId} in {ElapsedMs}ms",
               input.ResumeId, 
               stopwatch.ElapsedMilliseconds);
           
           return result;
       }
       catch (Exception ex)
       {
           logger.LogError(ex, "Resume processing failed for {ResumeId}", input.ResumeId);
           throw;
       }
   }
   ```

3. **Configure Azure Functions Flex Consumption**
   ```terraform
   resource "azurerm_linux_function_app" "agent_functions" {
     name                = "func-cvanalyzer-${var.environment}"
     resource_group_name = azurerm_resource_group.main.name
     location            = azurerm_resource_group.main.location
     
     # Flex Consumption plan
     service_plan_id = azurerm_service_plan.flex_plan.id
     
     # Scale to zero when idle
     site_config {
       minimum_instance_count = 0
       maximum_instance_count = 100
       
       application_insights_connection_string = azurerm_application_insights.main.connection_string
     }
     
     app_settings = {
       "AZURE_OPENAI_ENDPOINT"   = azurerm_cognitive_account.openai.endpoint
       "AZURE_OPENAI_DEPLOYMENT" = "gpt-4o"
       "AzureWebJobsStorage"     = azurerm_storage_account.main.primary_connection_string
     }
     
     identity {
       type = "SystemAssigned"
     }
   }
   ```

4. **Load Testing**
   - Simulate concurrent resume uploads (100+ resumes)
   - Verify auto-scaling behavior (0 → 100 instances)
   - Measure end-to-end latency and cost

**Deliverables**:
- ✅ Visual debugging dashboard
- ✅ Comprehensive monitoring and alerts
- ✅ Auto-scaling configuration (0-100 instances)
- ✅ Load testing validation

---

## Cost Analysis

### Current Architecture (v1.0)

**Monthly Costs (Development)**:
- App Service (B1 Basic): $13.14/month
- SQL Database (Basic): $4.90/month
- Azure Storage: $0.50/month (queue + blobs)
- Azure OpenAI (GPT-4o): ~$30/month (500 resumes × $0.06 per analysis)
- **Total**: ~$48/month

**Limitations**:
- Always-on compute (App Service)
- Manual scaling required
- Limited to 1 worker instance (single BackgroundService)

### Durable Agents Architecture (v2.0)

**Monthly Costs (Development)**:
- Azure Functions Flex Consumption:
  - Compute: ~$5/month (scale to zero, pay per execution)
  - Storage (durable state): $2/month
- SQL Database (Basic): $4.90/month
- Azure Storage: $0.50/month (blobs only, no queue)
- Azure OpenAI (GPT-4o): ~$30/month (same workload)
- Application Insights: $2/month
- **Total**: ~$44/month

**Production Costs (Estimated 10,000 resumes/month)**:
- Azure Functions Flex Consumption:
  - Compute: ~$150/month (average 50 instances, 3 seconds per resume)
  - Storage: $10/month
- SQL Database (Standard S2): $150/month
- Azure Storage: $5/month
- Azure OpenAI (GPT-4o): ~$600/month (10,000 × $0.06)
- Application Insights: $20/month
- **Total**: ~$935/month

**Cost Savings vs. App Service Plan**:
- Flex Consumption scales to zero (idle time = $0 compute)
- Pay-per-execution model (no idle costs)
- Better resource utilization (parallel orchestrations)
- Estimated 30-40% cost reduction at scale

---

## Migration Risks & Mitigation

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Breaking changes in Agent Framework** | High | Medium | Pin to stable package versions, test in dev environment |
| **State migration complexity** | Medium | High | Implement dual-write pattern (queue + orchestration) during transition |
| **Learning curve for team** | Medium | High | Comprehensive training, documentation, pair programming sessions |
| **Orchestration debugging challenges** | Medium | Medium | Use Durable Task Scheduler dashboard, extensive logging |
| **Azure Functions cold start latency** | Low | Medium | Use Flex Consumption's "always ready instances" feature |
| **Increased infrastructure complexity** | Medium | High | Thorough Terraform testing, rollback plan, monitoring alerts |

**Rollback Plan**:
1. Keep existing queue-based worker in codebase (feature flag)
2. Dual-write to both queue and orchestration during transition
3. Monitor error rates and performance metrics
4. If issues arise, disable orchestration and fall back to queue
5. Full rollback: revert Terraform changes, redeploy API with queue-only mode

---

## Success Metrics

### Technical Metrics
- ✅ **Fault Tolerance**: 99.9% orchestration completion rate
- ✅ **Scaling**: 0-100 instances in < 30 seconds
- ✅ **Latency**: P95 < 5 seconds for resume analysis
- ✅ **Cost**: 30% reduction vs. always-on App Service
- ✅ **Observability**: 100% of orchestrations visible in dashboard

### Business Metrics
- ✅ **User Experience**: 95% satisfaction with chat refinement
- ✅ **Engagement**: 3x increase in resume iterations per user
- ✅ **Reliability**: Zero data loss during process failures
- ✅ **Developer Velocity**: 50% faster to add new agent types

---

## Implementation Checklist

### Pre-Migration
- [ ] Read [Durable Agents documentation](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/durable-agent/create-durable-agent)
- [ ] Complete [Durable Agents tutorial](https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/create-and-run-durable-agent)
- [ ] Review [Durable Task Scheduler overview](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-task-scheduler/durable-task-scheduler)
- [ ] Prototype basic durable agent in separate project
- [ ] Get team buy-in and allocate 3-4 week sprint

### Phase 1: Foundation
- [ ] Create Azure Functions project (`CVAnalyzer.AgentFunctions`)
- [ ] Add NuGet packages (Agent Framework, Durable Task)
- [ ] Implement basic `ResumeAnalyzer` agent
- [ ] Deploy to Azure Functions (Flex Consumption)
- [ ] Test auto-generated HTTP endpoints
- [ ] Verify conversation state persistence

### Phase 2: Orchestration
- [ ] Implement `ResumeProcessingOrchestrator` function
- [ ] Create activity functions (ExtractText, SaveResults)
- [ ] Update `ResumesController` to start orchestrations
- [ ] Implement automatic retry logic
- [ ] Add orchestration status endpoint
- [ ] Test parallel agent execution

### Phase 3: Conversational Refinement
- [ ] Add chat endpoint (`/api/resumes/{id}/chat`)
- [ ] Implement thread management (create, resume, list)
- [ ] Create specialized agents (Suggestion, Optimization)
- [ ] Build frontend chat component
- [ ] Test multi-turn conversations
- [ ] Verify conversation history persistence

### Phase 4: Production Readiness
- [ ] Configure Durable Task Scheduler dashboard
- [ ] Set up Application Insights integration
- [ ] Implement comprehensive logging
- [ ] Configure auto-scaling rules (0-100 instances)
- [ ] Load testing (100+ concurrent resumes)
- [ ] Create monitoring alerts (failed orchestrations, high latency)
- [ ] Document deployment procedures
- [ ] Train team on debugging orchestrations

### Post-Migration
- [ ] Monitor production metrics for 2 weeks
- [ ] Collect user feedback on chat feature
- [ ] Analyze cost reduction vs. projections
- [ ] Remove legacy queue-based worker code
- [ ] Update all documentation
- [ ] Conduct retrospective and lessons learned session

---

## Alternative Approaches Considered

### 1. Keep Current Queue-Based Architecture
**Pros**: No migration effort, proven and working  
**Cons**: Limited scalability, no conversation state, manual state management  
**Decision**: Rejected due to lack of multi-agent orchestration and conversational features

### 2. Use Azure AI Foundry Agent Service (Fully Managed)
**Pros**: Zero infrastructure management, built-in conversation history  
**Cons**: Less code control, limited customization, vendor lock-in  
**Decision**: Deferred for future consideration; prefer code-first approach for now

### 3. Build Custom State Management with Azure Durable Functions (No Agent Framework)
**Pros**: Full control, mature Durable Functions ecosystem  
**Cons**: Must implement agent patterns manually, no built-in AI agent features  
**Decision**: Rejected; Agent Framework provides higher abstraction for AI-specific scenarios

---

## Related Documentation

- [Microsoft Agent Framework Overview](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- [Create and Run a Durable Agent (Tutorial)](https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/create-and-run-durable-agent)
- [Durable Task Scheduler Dashboard](https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-task-scheduler/durable-task-scheduler)
- [Azure Functions Flex Consumption Plan](https://learn.microsoft.com/en-us/azure/azure-functions/flex-consumption-plan)
- [Current Agent Implementation](../backend/src/CVAnalyzer.AgentService/ResumeAnalysisAgent.cs)
- [Architecture Documentation](./ARCHITECTURE.md)
- [Security Guidelines](./SECURITY.md)

---

## Conclusion

Migrating to **Durable Agents** will transform CV Analyzer from a basic stateless analysis service to a **sophisticated, conversational AI platform** with:

✅ **Fault-tolerant multi-agent orchestrations** that survive crashes  
✅ **Stateful conversation threads** for iterative resume refinement  
✅ **Automatic scaling** from 0 to 1000s of instances  
✅ **Visual debugging** via Durable Task Scheduler  
✅ **30-40% cost reduction** with pay-per-execution pricing  

The 3-4 week migration is justified by significant improvements in **reliability**, **scalability**, **observability**, and **user experience**.

**Recommended Action**: Approve Phase 1 (Foundation) for next sprint to validate feasibility and developer experience before committing to full migration.

---

*Document Version*: 1.0  
*Created*: November 16, 2025  
*Author*: Expert AI Architecture Review  
*Status*: Proposed - Awaiting Approval  
*Estimated Effort*: 3-4 weeks (1 sprint)  
*Business Value*: High (conversational AI, fault tolerance, cost savings)
