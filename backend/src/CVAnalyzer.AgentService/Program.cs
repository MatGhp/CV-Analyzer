using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using Azure.Identity;
using CVAnalyzer.AgentService.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CVAnalyzer.AgentService;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        var startup = new AgentStartup();
        startup.ConfigureServices(builder.Services, builder);

        var app = builder.Build();
        startup.Configure(app);

        app.MapGet("/", () => Results.Ok(new
        {
            name = "CV Analyzer Agent Service",
            version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            description = "Analyzes resumes using Azure OpenAI via Microsoft Agent Framework abstractions."
        }));

        app.MapGet("/health", (ResumeAnalysisAgent agent) =>
        {
            var status = new
            {
                status = "healthy",
                aiConnected = agent.IsConfigured
            };

            return Results.Ok(status);
        });

        app.MapPost("/analyze", async (
                ResumeAnalysisRequest request,
                ResumeAnalysisAgent agent,
                CancellationToken cancellationToken) =>
            {
                if (!MiniValidator.TryValidate(request, out var errors))
                {
                    return Results.ValidationProblem(errors);
                }

                var result = await agent.AnalyzeAsync(request, cancellationToken);
                return Results.Ok(result);
            })
            .WithName("AnalyzeResume")
            .Produces<ResumeAnalysisResponse>()
            .ProducesValidationProblem();

        await app.RunAsync();
    }

    private static class MiniValidator
    {
        public static bool TryValidate<T>(T model, out Dictionary<string, string[]> errors)
        {
            errors = new Dictionary<string, string[]>();
            if (model is null)
            {
                errors.Add("model", ["Request body is required."]);
                return false;
            }

            var validationContext = new ValidationContext(model!);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(model!, validationContext, validationResults, true))
            {
                foreach (var validationResult in validationResults)
                {
                    var memberName = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
                    if (!errors.TryGetValue(memberName, out var messages))
                    {
                        errors[memberName] = [validationResult.ErrorMessage ?? "Validation failed."];
                    }
                    else
                    {
                        errors[memberName] = messages.Append(validationResult.ErrorMessage ?? "Validation failed.").ToArray();
                    }
                }

                return false;
            }

            return true;
        }
    }
}
