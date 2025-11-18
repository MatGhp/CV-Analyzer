using CVAnalyzer.API.Middleware;
using CVAnalyzer.Application;
using CVAnalyzer.Infrastructure;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with conditional Application Insights
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/cvanalyzer-.log", rollingInterval: RollingInterval.Day);

// Only add Application Insights if connection string is valid
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
{
    try
    {
        loggerConfig.WriteTo.ApplicationInsights(appInsightsConnectionString, TelemetryConverter.Traces);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Failed to configure Application Insights: {ex.Message}");
    }
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CV Analyzer API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();

// Health checks with specific checks
builder.Services.AddHealthChecks()
    .AddCheck<CVAnalyzer.Infrastructure.HealthChecks.BlobStorageHealthCheck>("blob_storage")
    .AddCheck<CVAnalyzer.Infrastructure.HealthChecks.DocumentIntelligenceHealthCheck>("document_intelligence");

var app = builder.Build();

// Initialize Azure Storage Queues if configured
using (var scope = app.Services.CreateScope())
{
    try
    {
        var queueServiceClient = scope.ServiceProvider.GetService<Azure.Storage.Queues.QueueServiceClient>();
        if (queueServiceClient != null)
        {
            var queueOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CVAnalyzer.Infrastructure.Options.QueueOptions>>().Value;
            
            await queueServiceClient.GetQueueClient(queueOptions.ResumeAnalysisQueueName).CreateIfNotExistsAsync();
            await queueServiceClient.GetQueueClient(queueOptions.PoisonQueueName).CreateIfNotExistsAsync();
            Log.Information("Azure Storage Queues initialized: {ResumeQueue}, {PoisonQueue}", queueOptions.ResumeAnalysisQueueName, queueOptions.PoisonQueueName);
        }
        else
        {
            Log.Warning("QueueServiceClient not configured. Skipping queue initialization.");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to initialize Azure Storage Queues. This is expected in test environments.");
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CV Analyzer API v1"));
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/api/health");

try
{
    Log.Information("Starting CV Analyzer API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
