namespace CVAnalyzer.Domain.Entities;

/// <summary>
/// Represents an AI prompt template for agent execution.
/// Supports environment-specific prompts and version control.
/// </summary>
public class PromptTemplate
{
    public Guid Id { get; set; }

    /// <summary>
    /// Agent type identifier (e.g., "ResumeAnalyzer", "ContentExtractor", "ATSScorer").
    /// Maps to Agent Framework executor class names.
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Task type identifier (e.g., "Extraction", "Scoring", "Suggestion").
    /// Describes what the agent does.
    /// </summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>
    /// Environment identifier (Development, Test, Production).
    /// Enables environment-specific prompt variations.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for administrative purposes.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The actual system prompt content sent to the AI model.
    /// Can be multi-line text with instructions.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON string with variable substitutions (e.g., {"target_role": "Software Engineer"}).
    /// Can be null if no variables are needed.
    /// </summary>
    public string? Variables { get; set; }

    /// <summary>
    /// Version number for tracking prompt evolution.
    /// Allows rollback and A/B testing.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Indicates if this version is currently active.
    /// Only one version per AgentType/TaskType/Environment should be active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User or system that created this prompt version.
    /// </summary>
    public string CreatedBy { get; set; } = "System";

    /// <summary>
    /// Timestamp when this prompt version was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this prompt version was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
