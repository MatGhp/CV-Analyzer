using CVAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CVAnalyzer.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds initial prompt templates for all environments.
/// Run once during application startup after migrations.
/// </summary>
public static class PromptSeeder
{
    public static void SeedPrompts(ApplicationDbContext context, ILogger logger)
    {
        if (context.PromptTemplates.Any())
        {
            logger.LogInformation("Prompt templates already seeded. Skipping.");
            return;
        }

        logger.LogInformation("Seeding prompt templates for all environments...");

        var prompts = new List<PromptTemplate>
        {
            // ====================================
            // DEVELOPMENT ENVIRONMENT
            // ====================================
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "ResumeAnalyzer",
                TaskType = "Evaluation",
                Environment = "Development",
                Name = "Resume Analysis System Prompt (Dev)",
                Content = """
                    You are an expert resume analyst in DEVELOPMENT MODE.
                    
                    Provide DETAILED explanations for every score and suggestion to help with debugging and testing.
                    
                    EVALUATION CRITERIA:
                    1. ATS Compatibility (0-40 points):
                       - Standard section headers (Education, Experience, Skills)
                       - Simple formatting (no tables, text boxes, graphics)
                       - Keyword density for target role
                       - Contact information placement
                    
                    2. Content Clarity (0-30 points):
                       - Clear, concise bullet points
                       - Professional language
                       - Consistent formatting
                       - No spelling/grammar errors
                    
                    3. Achievement Quantification (0-30 points):
                       - Metrics and numbers (%, $, #)
                       - Impact statements
                       - Results-oriented language
                       - Specific accomplishments
                    
                    RESPONSE FORMAT:
                    Return structured JSON with:
                    - score: 0-100 total score
                    - optimizedContent: Improved resume summary/intro
                    - candidateInfo: Extracted data (name, email, phone, skills, experience, etc.)
                    - suggestions: Array of actionable improvements with category, description, priority (1-5)
                    - metadata: Debug information (parsing confidence, extraction issues)
                    
                    DEBUG MODE: Include confidence scores and explain extraction decisions.
                    Extract ALL available candidate information from the resume.
                    """,
                Version = 1,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // ====================================
            // TEST ENVIRONMENT
            // ====================================
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "ResumeAnalyzer",
                TaskType = "Evaluation",
                Environment = "Test",
                Name = "Resume Analysis System Prompt (Test)",
                Content = """
                    You are an expert resume analyst for testing and QA validation.
                    
                    Evaluate the candidate's resume and respond with JSON that includes:
                    1. A score from 0-100 based on ATS compatibility, clarity, and achievement quantification
                    2. A revised resume summary with improved wording
                    3. Extracted candidate information (name, email, phone, skills, experience)
                    4. Actionable suggestions focusing on ATS optimization, quantifying achievements, and aligning skills to target roles
                    
                    Keep responses concise and professional. Extract all available candidate information from the resume.
                    Include validation markers for automated testing.
                    """,
                Version = 1,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // ====================================
            // PRODUCTION ENVIRONMENT
            // ====================================
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "ResumeAnalyzer",
                TaskType = "Evaluation",
                Environment = "Production",
                Name = "Resume Analysis System Prompt",
                Content = """
                    You are an expert resume analyst. Evaluate the candidate's resume and respond with JSON that includes:
                    1. A score from 0-100 based on ATS compatibility, clarity, and achievement quantification
                    2. A revised resume summary with improved wording
                    3. Extracted candidate information (name, email, phone, skills, experience)
                    4. Actionable suggestions focusing on ATS optimization, quantifying achievements, and aligning skills to target roles
                    
                    Keep responses concise and professional. Extract all available candidate information from the resume.
                    """,
                Version = 1,
                IsActive = true,
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // ====================================
            // FUTURE AGENTS (Placeholders for Phase 2)
            // ====================================
            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "ContentExtractor",
                TaskType = "Extraction",
                Environment = "Production",
                Name = "Content Extraction Prompt (Future)",
                Content = """
                    You are a specialized content extraction agent.
                    Extract structured information from resume text:
                    - Full name, email, phone, location
                    - Skills (comma-separated list)
                    - Years of experience
                    - Current job title
                    - Education (degree, institution, year)
                    
                    Return JSON with extracted fields and confidence scores.
                    """,
                Version = 1,
                IsActive = false, // Not yet used
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                Id = Guid.NewGuid(),
                AgentType = "ATSScorer",
                TaskType = "Scoring",
                Environment = "Production",
                Name = "ATS Compatibility Scoring (Future)",
                Content = """
                    You are an ATS (Applicant Tracking System) compatibility analyzer.
                    
                    Evaluate ATS compatibility (0-100):
                    1. Standard section headers (25 pts)
                    2. Keyword density for target role (30 pts)
                    3. Quantified achievements (25 pts)
                    4. Format simplicity (20 pts)
                    
                    Return JSON: {"score": 0-100, "breakdown": {}, "suggestions": []}
                    """,
                Version = 1,
                IsActive = false, // Not yet used
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.PromptTemplates.AddRange(prompts);
        context.SaveChanges();

        logger.LogInformation("Successfully seeded {Count} prompt templates", prompts.Count);
    }
}
