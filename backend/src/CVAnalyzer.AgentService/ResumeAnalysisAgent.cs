using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using Azure;
using Azure.AI.OpenAI;
using CVAnalyzer.AgentService.Models;
using CVAnalyzer.Domain.Entities;
using CVAnalyzer.Domain.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CVAnalyzer.AgentService;

public sealed class ResumeAnalysisAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly OpenAIClient _client;
    private readonly AgentServiceOptions _options;
    private readonly IPromptTemplateRepository _promptRepository;
    private readonly ILogger<ResumeAnalysisAgent> _logger;
    private readonly string _environment;

    public ResumeAnalysisAgent(
        OpenAIClient client, 
        IOptions<AgentServiceOptions> options, 
        IPromptTemplateRepository promptRepository,
        IHostEnvironment hostEnvironment,
        ILogger<ResumeAnalysisAgent> logger)
    {
        _client = client;
        _options = options.Value;
        _promptRepository = promptRepository;
        _environment = hostEnvironment.EnvironmentName ?? "Production";
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Endpoint) && !string.IsNullOrWhiteSpace(_options.Deployment);

    public async Task<ResumeAnalysisResponse> AnalyzeAsync(ResumeAnalysisRequest request, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Agent service is not configured. Ensure Agent:Endpoint and Agent:Deployment are set.");
        }

        _logger.LogInformation("Starting resume analysis for user {UserId}", request.UserId);
        _logger.LogInformation("Using endpoint: {Endpoint}, deployment: {Deployment}", _options.Endpoint, _options.Deployment);

        var chatOptions = await BuildChatOptionsAsync(request.Content, cancellationToken);

        _logger.LogInformation("Calling Azure OpenAI with {MessageCount} messages", chatOptions.Messages.Count);
        var completion = await _client.GetChatCompletionsAsync(chatOptions, cancellationToken);
        _logger.LogInformation("Received completion with {ChoiceCount} choices", completion.Value.Choices?.Count ?? 0);
        
        var message = completion.Value.Choices?.FirstOrDefault()?.Message;

        if (message is null)
        {
            _logger.LogError("No message in response. Choices count: {Count}", completion.Value.Choices?.Count ?? 0);
            throw new InvalidOperationException("AI response contained no message.");
        }

        // Handle function call response (when using Functions API)
        string jsonPayload;
        if (message.FunctionCall != null)
        {
            _logger.LogInformation("Response is a function call: {FunctionName}", message.FunctionCall.Name);
            jsonPayload = message.FunctionCall.Arguments;
            _logger.LogInformation("Function arguments length: {Length} chars", jsonPayload?.Length ?? 0);
        }
        else if (!string.IsNullOrWhiteSpace(message.Content))
        {
            _logger.LogInformation("Response is text content, length: {Length} chars", message.Content.Length);
            jsonPayload = message.Content;
        }
        else
        {
            _logger.LogError("Agent response was empty. FinishReason: {FinishReason}, Content: '{Content}', ToolCalls: {ToolCallCount}", 
                completion.Value.Choices?.FirstOrDefault()?.FinishReason,
                message.Content ?? "(null)",
                message.ToolCalls?.Count ?? 0);
            
            throw new InvalidOperationException("AI response was empty.");
        }
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            _logger.LogError("Agent response did not include text content.");
            throw new InvalidOperationException("AI response did not include text content.");
        }

        var agentResponse = JsonSerializer.Deserialize<AgentResponse>(jsonPayload, JsonOptions);
        if (agentResponse is null)
        {
            _logger.LogError("Failed to deserialize agent response: {Payload}", jsonPayload);
            throw new InvalidOperationException("Unable to parse AI response.");
        }

        _logger.LogInformation("Resume analysis completed for user {UserId} with score {Score}", request.UserId, agentResponse.Score);

        return new ResumeAnalysisResponse
        {
            Score = Math.Clamp(agentResponse.Score, 0, 100),
            OptimizedContent = agentResponse.OptimizedContent,
            CandidateInfo = agentResponse.CandidateInfo != null ? new CandidateInfoDto
            {
                FullName = agentResponse.CandidateInfo.FullName,
                Email = agentResponse.CandidateInfo.Email,
                Phone = agentResponse.CandidateInfo.Phone,
                Location = agentResponse.CandidateInfo.Location,
                Skills = agentResponse.CandidateInfo.Skills,
                YearsOfExperience = agentResponse.CandidateInfo.YearsOfExperience,
                CurrentJobTitle = agentResponse.CandidateInfo.CurrentJobTitle,
                Education = agentResponse.CandidateInfo.Education
            } : null,
            Suggestions = agentResponse.Suggestions?.Select(s => new ResumeSuggestion
            {
                Category = s.Category,
                Description = s.Description,
                Priority = Math.Clamp(s.Priority, 1, 5)
            }).ToArray() ?? Array.Empty<ResumeSuggestion>(),
            Metadata = agentResponse.Metadata ?? new Dictionary<string, JsonElement>()
        };
    }

    private async Task<ChatCompletionsOptions> BuildChatOptionsAsync(string resumeContent, CancellationToken cancellationToken)
    {
        // Fetch environment-specific prompt from database with fallback on failure
        PromptTemplate? promptTemplate = null;
        try
        {
            promptTemplate = await _promptRepository.GetActiveAsync(
                _environment,
                "ResumeAnalyzer",
                "Evaluation",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to fetch prompt from database for {Environment}/ResumeAnalyzer/Evaluation. Falling back to default prompt.",
                _environment);
        }

        if (promptTemplate == null)
        {
            _logger.LogWarning(
                "No active prompt found for {Environment}/ResumeAnalyzer/Evaluation. Using fallback prompt.",
                _environment);
            
            // Fallback to hardcoded prompt if database is unavailable
            return BuildFallbackChatOptions(resumeContent);
        }

        _logger.LogInformation(
            "Using prompt version {Version} for {Environment}/ResumeAnalyzer/Evaluation",
            promptTemplate.Version, _environment);

        var options = new ChatCompletionsOptions
        {
            DeploymentName = _options.Deployment,
            Temperature = (float)_options.Temperature,
            NucleusSamplingFactor = (float)_options.TopP,
            MaxTokens = 1200,
            Functions = { new FunctionDefinition 
            { 
                Name = "resume_analysis",
                Description = "Analyze a resume and provide structured feedback",
                Parameters = BinaryData.FromString(JsonSchema)
            }}
        };

        options.Messages.Add(new ChatRequestSystemMessage(promptTemplate.Content));
        options.Messages.Add(new ChatRequestUserMessage(resumeContent));

        return options;
    }

    private ChatCompletionsOptions BuildFallbackChatOptions(string resumeContent)
    {
        _logger.LogWarning("Using fallback prompt. This should only happen during initial setup or database issues.");
        
        var options = new ChatCompletionsOptions
        {
            DeploymentName = _options.Deployment,
            Temperature = (float)_options.Temperature,
            NucleusSamplingFactor = (float)_options.TopP,
            MaxTokens = 1200,
            Functions = { new FunctionDefinition 
            { 
                Name = "resume_analysis",
                Description = "Analyze a resume and provide structured feedback",
                Parameters = BinaryData.FromString(JsonSchema)
            }}
        };

        options.Messages.Add(new ChatRequestSystemMessage(FallbackSystemPrompt));
        options.Messages.Add(new ChatRequestUserMessage(resumeContent));

        return options;
    }

    private const string FallbackSystemPrompt = """
You are an expert resume analyst. Evaluate the candidate's resume and respond with JSON that includes:
1. A score from 0-100 based on ATS compatibility, clarity, and achievement quantification
2. A revised resume summary with improved wording
3. Extracted candidate information (name, contact, skills, experience)
4. Actionable suggestions focusing on ATS optimization, quantifying achievements, and aligning skills to target roles

Keep responses concise and professional. Extract all available candidate information from the resume.
""";

    private const string JsonSchema = """
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["score", "optimizedContent", "suggestions", "candidateInfo"],
  "properties": {
    "score": {
      "type": "number",
      "minimum": 0,
      "maximum": 100
    },
    "optimizedContent": {
      "type": "string"
    },
    "candidateInfo": {
      "type": "object",
      "required": ["fullName", "email", "skills"],
      "properties": {
        "fullName": { "type": "string" },
        "email": { "type": "string" },
        "phone": { "type": "string" },
        "location": { "type": "string" },
        "skills": { "type": "string" },
        "yearsOfExperience": { "type": "integer" },
        "currentJobTitle": { "type": "string" },
        "education": { "type": "string" }
      }
    },
    "suggestions": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["category", "description", "priority"],
        "properties": {
          "category": { "type": "string" },
          "description": { "type": "string" },
          "priority": { "type": "integer", "minimum": 1, "maximum": 5 }
        }
      }
    },
    "metadata": {
      "type": "object",
      "additionalProperties": { "type": "string" }
    }
  }
}
""";

    private sealed class AgentResponse
    {
        [JsonPropertyName("score")]
        public double Score { get; init; }

        [JsonPropertyName("optimizedContent")]
        public string OptimizedContent { get; init; } = string.Empty;

        [JsonPropertyName("candidateInfo")]
        public AgentCandidateInfo? CandidateInfo { get; init; }

        [JsonPropertyName("suggestions")]
        public IReadOnlyList<AgentSuggestion>? Suggestions { get; init; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, JsonElement>? Metadata { get; init; }
    }

    private sealed class AgentCandidateInfo
    {
        [JsonPropertyName("fullName")]
        public string FullName { get; init; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;

        [JsonPropertyName("phone")]
        public string? Phone { get; init; }

        [JsonPropertyName("location")]
        public string? Location { get; init; }

        [JsonPropertyName("skills")]
        public string Skills { get; init; } = string.Empty;

        [JsonPropertyName("yearsOfExperience")]
        public int? YearsOfExperience { get; init; }

        [JsonPropertyName("currentJobTitle")]
        public string? CurrentJobTitle { get; init; }

        [JsonPropertyName("education")]
        public string? Education { get; init; }
    }

    private sealed class AgentSuggestion
    {
        [JsonPropertyName("category")]
        public string Category { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;

        [JsonPropertyName("priority")]
    public int Priority { get; init; }
    }
}
