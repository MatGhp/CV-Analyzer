using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for PromptTemplate with in-memory caching.
/// Cache TTL: 15 minutes to balance freshness and performance.
/// </summary>
public class PromptTemplateRepository : IPromptTemplateRepository
{
    private readonly IApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PromptTemplateRepository> _logger;
    private const int CacheExpirationMinutes = 15;

    public PromptTemplateRepository(
        IApplicationDbContext context,
        IMemoryCache cache,
        ILogger<PromptTemplateRepository> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PromptTemplate?> GetActiveAsync(
        string environment,
        string agentType,
        string taskType,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(environment, agentType, taskType);

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes);

            _logger.LogDebug(
                "Cache miss for prompt: {Environment}/{AgentType}/{TaskType}. Fetching from database.",
                environment, agentType, taskType);

            var prompt = await _context.PromptTemplates
                .Where(p => p.Environment == environment
                         && p.AgentType == agentType
                         && p.TaskType == taskType
                         && p.IsActive)
                .OrderByDescending(p => p.Version)
                .FirstOrDefaultAsync(cancellationToken);

            if (prompt == null)
            {
                _logger.LogWarning(
                    "No active prompt found for {Environment}/{AgentType}/{TaskType}",
                    environment, agentType, taskType);
            }
            else
            {
                _logger.LogInformation(
                    "Loaded prompt version {Version} for {Environment}/{AgentType}/{TaskType}",
                    prompt.Version, environment, agentType, taskType);
            }

            return prompt;
        });
    }

    public async Task<PromptTemplate?> GetVersionAsync(
        string agentType,
        string taskType,
        string environment,
        int version,
        CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(p => p.AgentType == agentType
                     && p.TaskType == taskType
                     && p.Environment == environment
                     && p.Version == version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PromptTemplate>> GetVersionHistoryAsync(
        string agentType,
        string taskType,
        string environment,
        CancellationToken cancellationToken = default)
    {
        return await _context.PromptTemplates
            .Where(p => p.AgentType == agentType
                     && p.TaskType == taskType
                     && p.Environment == environment)
            .OrderByDescending(p => p.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        PromptTemplate template,
        CancellationToken cancellationToken = default)
    {
        template.Id = Guid.NewGuid();
        template.CreatedAt = DateTime.UtcNow;
        template.UpdatedAt = DateTime.UtcNow;

        _context.PromptTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created prompt template {Id} version {Version} for {Environment}/{AgentType}/{TaskType}",
            template.Id, template.Version, template.Environment, template.AgentType, template.TaskType);

        // Clear cache for this agent/task/environment
        var cacheKey = GetCacheKey(template.Environment, template.AgentType, template.TaskType);
        _cache.Remove(cacheKey);

        return template.Id;
    }

    public async Task<bool> ActivateVersionAsync(
        string agentType,
        string taskType,
        string environment,
        int version,
        CancellationToken cancellationToken = default)
    {
        // Use transaction to prevent race conditions when multiple requests activate different versions
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check if target version exists
            var targetExists = await _context.PromptTemplates
                .AnyAsync(p => p.AgentType == agentType
                            && p.TaskType == taskType
                            && p.Environment == environment
                            && p.Version == version,
                         cancellationToken);

            if (!targetExists)
            {
                _logger.LogWarning(
                    "Version {Version} not found for {Environment}/{AgentType}/{TaskType}",
                    version, environment, agentType, taskType);
                return false;
            }

            // Load all versions for this agent/task/environment
            var allVersions = await _context.PromptTemplates
                .Where(p => p.AgentType == agentType
                         && p.TaskType == taskType
                         && p.Environment == environment)
                .ToListAsync(cancellationToken);

            // Deactivate all versions
            foreach (var prompt in allVersions)
            {
                prompt.IsActive = false;
                prompt.UpdatedAt = DateTime.UtcNow;
            }

            // Activate target version
            var targetPrompt = allVersions.FirstOrDefault(p => p.Version == version);
            if (targetPrompt != null)
            {
                targetPrompt.IsActive = true;
                targetPrompt.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Activated prompt version {Version} for {Environment}/{AgentType}/{TaskType}",
                version, environment, agentType, taskType);

            // Clear cache to force refresh (only after successful commit)
            var cacheKey = GetCacheKey(environment, agentType, taskType);
            _cache.Remove(cacheKey);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to activate version {Version} for {Environment}/{AgentType}/{TaskType}",
                version, environment, agentType, taskType);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string GetCacheKey(string environment, string agentType, string taskType)
        => $"prompt:{environment}:{agentType}:{taskType}";
}
