using System.Net;
using System.Text.Json;
using FluentValidation;

namespace CVAnalyzer.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var response = context.Response;
        response.ContentType = "application/json";

        object errorResponse;

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new
                {
                    message = "Validation failed",
                    statusCode = HttpStatusCode.BadRequest,
                    errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
                };
                break;

            case ArgumentException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = new
                {
                    message = exception.Message,
                    statusCode = HttpStatusCode.BadRequest
                };
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = new
                {
                    message = exception.Message,
                    statusCode = HttpStatusCode.InternalServerError
                };
                break;
        }

        await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}
