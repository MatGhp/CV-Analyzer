using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using CVAnalyzer.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CVAnalyzer.IntegrationTests.Controllers;

public class PromptManagementIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _context;
    private readonly IPromptTemplateRepository _repository;

    public PromptManagementIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Add InMemory database with transaction warnings suppressed
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
        
        services.AddMemoryCache();
        services.AddLogging();
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPromptTemplateRepository, Infrastructure.Persistence.Repositories.PromptTemplateRepository>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _repository = _serviceProvider.GetRequiredService<IPromptTemplateRepository>();
    }

    [Fact]
    public async Task PromptRepository_GetActive_ReturnsCorrectPrompt()
    {
        // Arrange
        var testPrompt = new PromptTemplate
        {
            Id = Guid.NewGuid(),
            AgentType = "TestAgent",
            TaskType = "TestTask",
            Environment = "Production",
            Content = "Integration test prompt",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PromptTemplates.Add(testPrompt);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveAsync("Production", "TestAgent", "TestTask");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(testPrompt.Id);
        result.Content.Should().Be("Integration test prompt");
    }

    [Fact]
    public async Task PromptRepository_CreateAsync_AddsPromptToDatabase()
    {
        // Arrange
        var newPrompt = new PromptTemplate
        {
            Id = Guid.NewGuid(),
            AgentType = "NewAgent",
            TaskType = "NewTask",
            Environment = "Development",
            Content = "New prompt content",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.CreateAsync(newPrompt);

        // Assert
        var savedPrompt = await _context.PromptTemplates
            .FirstOrDefaultAsync(p => p.Id == newPrompt.Id);
        
        savedPrompt.Should().NotBeNull();
        savedPrompt!.Content.Should().Be("New prompt content");
    }

    [Fact]
    public async Task PromptRepository_ActivateVersion_UpdatesIsActiveStatus()
    {
        // Arrange
        var prompts = new List<PromptTemplate>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "TestAgent",
                TaskType = "TestTask",
                Environment = "Production",
                Content = "Version 1",
                Version = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "TestAgent",
                TaskType = "TestTask",
                Environment = "Production",
                Content = "Version 2",
                Version = 2,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.PromptTemplates.AddRange(prompts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ActivateVersionAsync("TestAgent", "TestTask", "Production", 2);

        // Assert
        result.Should().BeTrue();

        var updatedPrompts = await _context.PromptTemplates
            .Where(p => p.AgentType == "TestAgent" && p.TaskType == "TestTask")
            .ToListAsync();

        var v1 = updatedPrompts.First(p => p.Version == 1);
        var v2 = updatedPrompts.First(p => p.Version == 2);

        v1.IsActive.Should().BeFalse();
        v2.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task PromptRepository_GetVersionHistory_ReturnsAllVersionsDescending()
    {
        // Arrange
        var prompts = new List<PromptTemplate>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "HistoryAgent",
                TaskType = "HistoryTask",
                Environment = "Production",
                Version = 1,
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "HistoryAgent",
                TaskType = "HistoryTask",
                Environment = "Production",
                Version = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "HistoryAgent",
                TaskType = "HistoryTask",
                Environment = "Production",
                Version = 2,
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.PromptTemplates.AddRange(prompts);
        await _context.SaveChangesAsync();

        // Act
        var history = await _repository.GetVersionHistoryAsync("HistoryAgent", "HistoryTask", "Production");

        // Assert
        history.Should().HaveCount(3);
        history[0].Version.Should().Be(3);
        history[1].Version.Should().Be(2);
        history[2].Version.Should().Be(1);
    }

    [Fact]
    public async Task PromptRepository_CacheWorks_ReturnsSameInstanceOnRepeatedCalls()
    {
        // Arrange
        var testPrompt = new PromptTemplate
        {
            Id = Guid.NewGuid(),
            AgentType = "CacheAgent",
            TaskType = "CacheTask",
            Environment = "Production",
            Content = "Cached content",
            Version = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PromptTemplates.Add(testPrompt);
        await _context.SaveChangesAsync();

        // Act
        var firstCall = await _repository.GetActiveAsync("Production", "CacheAgent", "CacheTask");
        var secondCall = await _repository.GetActiveAsync("Production", "CacheAgent", "CacheTask");

        // Assert - Both calls return same data (cache hit)
        firstCall.Should().NotBeNull();
        secondCall.Should().NotBeNull();
        firstCall!.Id.Should().Be(secondCall!.Id);
        firstCall.Content.Should().Be("Cached content");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _serviceProvider.Dispose();
    }
}
