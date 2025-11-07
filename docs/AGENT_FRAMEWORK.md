## Microsoft Agent Framework for .NET — How we use it

This guide explains how we adopt the Microsoft Agent Framework in our C# services (API and AgentService). It covers installation, configuration, core concepts, example patterns aligned with our Clean Architecture, and migration notes from our current `Azure.AI.OpenAI`-only implementation.

References:
- Official repo: https://github.com/microsoft/agent-framework
- MS Learn docs: https://learn.microsoft.com/agent-framework/


## What is it?

Microsoft Agent Framework is a multi-language framework (C#/.NET and Python) for building agentic AI: single agents, multi-agent workflows, tools/functions, observability, and (optionally) graph-based orchestration. For .NET, it provides a thin, consistent API over different model providers and an opinionated “agent” abstraction you can run directly or wire into workflows.


## Install (our baseline)

We’ll use the OpenAI/Azure OpenAI provider in production. Add these packages to projects that host or call agents (e.g., `CVAnalyzer.AgentService`, optionally API):

```pwsh
# In the project directory (Windows PowerShell)
dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
dotnet add package Azure.Identity
```

Notes:
- Use DefaultAzureCredential in Azure (managed identity) and Azure CLI locally (`az login`).
- You can also use API keys during local spikes, but we avoid committing secrets.


## Quick start (two ways)

### 1) OpenAI API key (local only)

```csharp
// dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
using OpenAI;

// Replace with a local dev key only. Do not commit.
var agent = new OpenAIClient("<apikey>")
    .GetOpenAIResponseClient("gpt-4o-mini")
    .CreateAIAgent(name: "HaikuBot", instructions: "You are an upbeat assistant that writes beautifully.");

Console.WriteLine(await agent.RunAsync("Write a haiku about Microsoft Agent Framework."));
```

### 2) Azure OpenAI with managed identity / Azure CLI (recommended)

```csharp
// dotnet add package Microsoft.Agents.AI.OpenAI --prerelease
// dotnet add package Azure.Identity
using Azure.Identity;
using OpenAI;

// Use `az login` locally; in Azure use managed identity (DefaultAzureCredential)
var agent = new OpenAIClient(
    new BearerTokenPolicy(new AzureCliCredential(), "https://ai.azure.com/.default"),
    new OpenAIClientOptions { Endpoint = new Uri("https://<resource>.openai.azure.com/openai/v1") })
    .GetOpenAIResponseClient("gpt-4o-mini")
    .CreateAIAgent(name: "HaikuBot", instructions: "You are an upbeat assistant that writes beautifully.");

Console.WriteLine(await agent.RunAsync("Write a haiku about Microsoft Agent Framework."));
```

Replace `<resource>` with your Azure OpenAI resource name and `gpt-4o-mini` with your deployment name.


## How this maps to our architecture

- Agent hosting stays in `CVAnalyzer.AgentService`. We expose a method like `AnalyzeAsync` and keep our domain models (`ResumeAnalysisRequest/Response`).
- Dependency Injection: register any required clients/credentials once; create short-lived agents per request or keep a configured agent singleton if thread-safe for your scenario.
- Controllers or MediatR handlers call the AgentService as usual; no controller code needs to know provider details.


## Example: Resume analysis agent with structured JSON output

We instruct the model to return JSON, then parse with `System.Text.Json`. With Agent Framework, the pattern is the same; the framework simplifies getting a response from the provider.

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using OpenAI;

public sealed class ResumeAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ILogger<ResumeAgent> _logger;
    private readonly OpenAIClient _client;
    private readonly string _deployment;

    public ResumeAgent(ILogger<ResumeAgent> logger, Uri endpoint, string deployment)
    {
        _logger = logger;
        _deployment = deployment;
        _client = new OpenAIClient(
            new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
            new OpenAIClientOptions { Endpoint = endpoint });
    }

    public async Task<ResumeAnalysisResponse> AnalyzeAsync(string content, CancellationToken ct = default)
    {
        var instructions = """
You are an expert resume analyst. Respond ONLY with JSON matching this schema:
{
  "score": number (0-100),
  "optimizedContent": string,
  "suggestions": [ { "category": string, "description": string, "priority": 1|2|3|4|5 } ],
  "metadata": { [key: string]: string }
}
""";

        var agent = _client
            .GetOpenAIResponseClient(_deployment)
            .CreateAIAgent(name: "ResumeAnalyzer", instructions: instructions);

        var json = await agent.RunAsync(content, cancellationToken: ct);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Empty AI response.");
        }

        var dto = JsonSerializer.Deserialize<AgentResponse>(json, JsonOptions)
                  ?? throw new InvalidOperationException("Failed to parse AI JSON response.");

        return new ResumeAnalysisResponse
        {
            Score = Math.Clamp(dto.Score, 0, 100),
            OptimizedContent = dto.OptimizedContent ?? string.Empty,
            Suggestions = dto.Suggestions?.Select(s => new ResumeSuggestion
            {
                Category = s.Category ?? string.Empty,
                Description = s.Description ?? string.Empty,
                Priority = Math.Clamp(s.Priority, 1, 5)
            }).ToArray() ?? Array.Empty<ResumeSuggestion>(),
            Metadata = dto.Metadata ?? new Dictionary<string, string>()
        };
    }

    private sealed class AgentResponse
    {
        public double Score { get; init; }
        public string? OptimizedContent { get; init; }
        public IReadOnlyList<Suggestion>? Suggestions { get; init; }
        public Dictionary<string, string>? Metadata { get; init; }
    }

    private sealed class Suggestion
    {
        public string? Category { get; init; }
        public string? Description { get; init; }
        public int Priority { get; init; }
    }
}
```

Key points:
- Validate and clamp values to enforce domain constraints.
- Keep instructions short, explicit, and deterministic when asking for structured JSON.
- Consider logging a truncated response for diagnostics; never log secrets or full PII.


## DI and configuration

Register configuration once. For example in our AgentService startup:

```csharp
using Azure.Identity;
using Microsoft.Extensions.Options;
using OpenAI;

builder.Services.Configure<AgentServiceOptions>(builder.Configuration.GetSection("Agent"));

builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<IOptions<AgentServiceOptions>>().Value;
    return new OpenAIClient(
        new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
        new OpenAIClientOptions { Endpoint = new Uri(opts.Endpoint!) });
});

builder.Services.AddSingleton(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ResumeAgent>>();
    var opts = provider.GetRequiredService<IOptions<AgentServiceOptions>>().Value;
    return new ResumeAgent(logger, new Uri(opts.Endpoint!), opts.Deployment!);
});
```

Configuration (appsettings):

```json
{
  "Agent": {
    "Endpoint": "https://<resource>.openai.azure.com/openai/v1",
    "Deployment": "gpt-4o-mini"
  }
}
```


## Adding “tools” (functions your agent can call)

The framework supports tool-style integrations so the agent can call back into deterministic functions. In .NET, consult the samples in the repo for the latest APIs. Typical pattern:
- Define a function/method that performs a deterministic action (e.g., lookup a job description, fetch candidate profile).
- Register it with the agent (middleware/tooling API) so the model can request its execution.
- Authorize and validate inputs; treat tool calls as untrusted input.

See: repo samples “GettingStarted/Agents” and “AgentProviders”, and MS Learn “User Guide → Tools/Functions”.


## Streaming, tracing, and workflows

- Streaming: the .NET SDK supports streaming results; see the .NET samples for `RunAsync` variants and streaming examples if you need token-level UI updates.
- Observability: built-in OpenTelemetry support is available; see the “AgentOpenTelemetry” sample for distributed tracing.
- Workflows: for multi-step processes (e.g., retrieval → analysis → summarization), consider the framework’s workflow/graph features. Start simple (single agent), then extract steps into a workflow only when necessary.


## Error handling and resilience

- Treat the model output as untrusted; validate and clamp.
- Timeouts and retries: wrap calls with resilient policies (e.g., Polly) where appropriate.
- Guardrails: never echo credentials; redact logs; respect our `docs/SECURITY.md`.
- Backpressure: avoid long token outputs in interactive endpoints; consider background jobs for heavy tasks.


## Testing

- Unit tests: abstract your agent behind an interface and mock it in handlers/controllers.
- Contract tests: record a minimal golden JSON and assert we can parse/validate it.
- Prompt changes: add unit tests that verify schema adherence (e.g., using JSON Schema validation for regressions).


## Migration from current code

Our `ResumeAnalysisAgent` currently uses `Azure.AI.OpenAI` directly. To migrate incrementally:
1) Introduce Agent Framework packages into `CVAnalyzer.AgentService`.
2) Create a new implementation (`ResumeAgent`, as shown above) using Agent Framework.
3) Behind `IAIResumeAnalyzerService` (or similar), switch from the legacy implementation to the new one.
4) Keep JSON schema and response DTOs identical to avoid ripple effects in API and frontend.
5) Roll out behind a feature flag if desired; compare outputs in logs for a short period.


## Best practices (team checklist)

- Keep instructions short and declarative; ask for strict JSON when needed.
- Validate and clamp numbers and enums.
- Avoid secrets in code; use managed identity and Key Vault/env for configuration.
- Log minimally and safely; truncate model output in logs; never log PII-rich full resumes.
- Start with a single agent; introduce workflows/tools later if/when clear value exists.
- Prefer deterministic tools for data access over letting the model “guess.”


## Troubleshooting

- 401/403 with Azure OpenAI: ensure `az login` locally or managed identity has the correct Azure OpenAI access. Check the scope `https://ai.azure.com/.default`.
- Empty/garbled JSON: tighten instructions, reduce temperature, explicitly say “Respond ONLY with JSON”.
- Latency too high: reduce max tokens, simplify instructions, or run in a background job.
- Model mismatch: ensure deployment name matches the chosen model (e.g., `gpt-4o`, `gpt-4o-mini`).


## Pinned links

- Repo README (quickstart + links): https://github.com/microsoft/agent-framework
- .NET samples: https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/GettingStarted
- MS Learn User Guide: https://learn.microsoft.com/agent-framework/user-guide/overview
- Migration from Semantic Kernel: https://learn.microsoft.com/agent-framework/migration-guide/from-semantic-kernel
