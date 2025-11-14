using CVAnalyzer.AgentService;
using CVAnalyzer.AgentService.Models;
using CVAnalyzer.Application.Common.Exceptions;
using CVAnalyzer.Application.Common.Interfaces;
using CVAnalyzer.Domain.Entities;
using CVAnalyzer.Domain.Enums;
using CVAnalyzer.Infrastructure.Mappers;
using CVAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Infrastructure.Services;

public class ResumeAnalysisOrchestrator
{
    private readonly ILogger<ResumeAnalysisOrchestrator> _logger;

    public ResumeAnalysisOrchestrator(ILogger<ResumeAnalysisOrchestrator> logger)
    {
        _logger = logger;
    }

    public async Task ProcessResumeAsync(
        Guid resumeId,
        string userId,
        ApplicationDbContext context,
        IBlobStorageService blobStorage,
        IDocumentIntelligenceService documentIntelligence,
        ResumeAnalysisAgent agent,
        CancellationToken cancellationToken)
    {
        var resume = await context.Resumes
            .Include(r => r.CandidateInfo)
            .Include(r => r.Suggestions)
            .FirstOrDefaultAsync(r => r.Id == resumeId, cancellationToken);

        if (resume == null)
        {
            throw new NotFoundException(nameof(Resume), resumeId);
        }

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Update status to processing
            resume.Status = ResumeStatus.Processing;
            await context.SaveChangesAsync(cancellationToken);

            // Stage 1: Extract text with Document Intelligence
            _logger.LogInformation("Stage 1: Extracting text for resume {ResumeId}", resume.Id);
            
            var blobUrlWithSas = await blobStorage.GenerateSasTokenAsync(resume.BlobUrl, cancellationToken);
            var extractedText = await documentIntelligence.ExtractTextFromDocumentAsync(blobUrlWithSas, cancellationToken);
            
            resume.Content = extractedText;

            // Stage 2: Analyze with GPT-4o
            _logger.LogInformation("Stage 2: Analyzing resume {ResumeId} with GPT-4o", resume.Id);
            
            var analysisRequest = new ResumeAnalysisRequest
            {
                Content = extractedText,
                UserId = userId
            };
            
            var analysisResult = await agent.AnalyzeAsync(analysisRequest, cancellationToken);

            // Update candidate info
            if (analysisResult.CandidateInfo != null)
            {
                if (resume.CandidateInfo == null)
                {
                    resume.CandidateInfo = CandidateInfoMapper.MapFromDto(analysisResult.CandidateInfo, resume.Id);
                }
                else
                {
                    CandidateInfoMapper.UpdateFromDto(resume.CandidateInfo, analysisResult.CandidateInfo);
                }
            }

            // Update suggestions
            resume.Suggestions.Clear();
            foreach (var suggestion in analysisResult.Suggestions)
            {
                resume.Suggestions.Add(new Suggestion
                {
                    Category = suggestion.Category,
                    Description = suggestion.Description,
                    Priority = suggestion.Priority,
                    ResumeId = resume.Id
                });
            }

            // Update resume
            resume.Score = (int?)Math.Round(analysisResult.Score);
            resume.OptimizedContent = analysisResult.OptimizedContent;
            resume.Status = ResumeStatus.Analyzed;
            resume.AnalyzedAt = DateTimeOffset.UtcNow.UtcDateTime;

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Resume {ResumeId} analysis completed with score {Score}", resume.Id, resume.Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze resume {ResumeId}. Rolling back transaction.", resume.Id);
            
            await transaction.RollbackAsync(cancellationToken);
            
            // Update status to pending for retry
            resume.Status = ResumeStatus.Pending;
            await context.SaveChangesAsync(cancellationToken);
            
            throw;
        }
    }
}
