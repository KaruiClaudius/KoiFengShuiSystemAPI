using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace KoiFengShuiSystem.Host.Middleware;

/// <summary>
/// Maps unhandled exceptions to RFC 7807 problem details. Client faults surface their
/// message as <c>detail</c>; server faults stay opaque and only expose a trace id.
/// </summary>
public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            var status = ExceptionProblemMapper.ResolveStatus(ex);
            var logLevel = status >= 500 ? LogLevel.Error : LogLevel.Warning;
            _logger.Log(logLevel, ex, "Unhandled exception ({Status}) for {Method} {Path}",
                status, context.Request.Method, context.Request.Path);

            await WriteProblemAsync(context, ex, status);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception, int status)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = ExceptionProblemMapper.ResolveTitle(status),
            Status = status,
            Detail = status < 500 ? exception.Message : null,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }
}

/// <summary>Severity-aware translation from exception types to HTTP semantics.</summary>
public static class ExceptionProblemMapper
{
    public static int ResolveStatus(Exception exception) => exception switch
    {
        ArgumentException => StatusCodes.Status400BadRequest,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        InvalidOperationException => StatusCodes.Status409Conflict,
        UnauthorizedAccessException => StatusCodes.Status403Forbidden,
        ApplicationException => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };

    public static string ResolveTitle(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Invalid request",
        StatusCodes.Status404NotFound => "Resource not found",
        StatusCodes.Status409Conflict => "Conflicting request state",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status500InternalServerError => "An unexpected error occurred",
        _ => "Request failed"
    };
}
