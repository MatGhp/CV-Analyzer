using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CVAnalyzer.AgentService;

public class AgentStartup
{
    public void ConfigureServices(IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddProblemDetails();

        services.Configure<AgentServiceOptions>(builder.Configuration.GetSection(AgentServiceOptions.SectionName));

        services.AddSingleton<OpenAIClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AgentServiceOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new InvalidOperationException("Agent:Endpoint configuration value is required.");
            }

            var credential = new DefaultAzureCredential();
            return new OpenAIClient(new Uri(options.Endpoint), credential);
        });

        services.AddSingleton<ResumeAnalysisAgent>();
    }

    public void Configure(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseExceptionHandler();
        app.UseHttpsRedirection();
    }
}