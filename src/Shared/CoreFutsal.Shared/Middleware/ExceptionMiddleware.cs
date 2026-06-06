using CoreFutsal.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreFutsal.Shared.Middleware;

public class ExceptionMiddleware
{
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            NotFoundException    => (StatusCodes.Status404NotFound,       "Not Found"),
            ConflictException    => (StatusCodes.Status409Conflict,       "Conflict"),
            ForbiddenException   => (StatusCodes.Status403Forbidden,      "Forbidden"),
            BadRequestException  => (StatusCodes.Status400BadRequest,     "Bad Request"),
            _                    => (StatusCodes.Status500InternalServerError, "Server Error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception");

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = ex.Message
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
