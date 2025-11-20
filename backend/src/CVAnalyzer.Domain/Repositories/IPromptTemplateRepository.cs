using CVAnalyzer.Domain.Entities;

namespace CVAnalyzer.Domain.Repositories;

/// <summary>
/// Repository for managing AI prompt templates with caching support.
/// </summary>
public interface IPromptTemplateRepository
{
    /// <summary>
    /// Retrieves the active prompt template for a specific agent and task in the given environment.
    /// Results are cached for performance.
    /// </summary>
    /// <param name="environment">Environment name (Development, Test, Production)</param>
    /// <param name="agentType">Agent type identifier (e.g., ResumeAnalyzer, ContentExtractor)</param>
    /// <param name="taskType">Task type identifier (e.g., Extraction, Scoring)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active prompt template or null if not found</returns>
    Task<PromptTemplate?> GetActiveAsync(
        string environment,
        string agentType,
        string taskType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific version of a prompt template.
    /// </summary>
    /// <param name="agentType">Agent type identifier</param>
    /// <param name="taskType">Task type identifier</param>
    /// <param name="environment">Environment name</param>
    /// <param name="version">Version number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Prompt template or null if not found</returns>
    Task<PromptTemplate?> GetVersionAsync(
        string agentType,
        string taskType,
        string environment,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all versions of a prompt template ordered by version descending.
    /// </summary>
    /// <param name="agentType">Agent type identifier</param>
    /// <param name="taskType">Task type identifier</param>
    /// <param name="environment">Environment name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of prompt template versions</returns>
    Task<List<PromptTemplate>> GetVersionHistoryAsync(
        string agentType,
        string taskType,
        string environment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new prompt template version.
    /// </summary>
    /// <param name="template">Prompt template to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of the created template</returns>
    Task<Guid> CreateAsync(
        PromptTemplate template,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a specific version and deactivates all other versions for the same agent/task/environment.
    /// Clears the cache after activation.
    /// </summary>
    /// <param name="agentType">Agent type identifier</param>
    /// <param name="taskType">Task type identifier</param>
    /// <param name="environment">Environment name</param>
    /// <param name="version">Version to activate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if activation succeeded, false if version not found</returns>
    Task<bool> ActivateVersionAsync(
        string agentType,
        string taskType,
        string environment,
        int version,
        CancellationToken cancellationToken = default);
}
