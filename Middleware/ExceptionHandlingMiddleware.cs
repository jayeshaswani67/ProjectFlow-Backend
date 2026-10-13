using System.Net;
using System.Text.Json;
using ProjectFlow.Api.Common.Responses;
using ProjectFlow.Api.Exceptions;

namespace ProjectFlow.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(
                context,
                exception
            );
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var traceId = context.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception. TraceId: {TraceId}",
            traceId
        );

        var statusCode = exception switch
        {
            ApiException apiException =>
                apiException.StatusCode,

            ArgumentException =>
                StatusCodes.Status400BadRequest,

            UnauthorizedAccessException =>
                StatusCodes.Status401Unauthorized,

            KeyNotFoundException =>
                StatusCodes.Status404NotFound,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        var code = exception switch
        {
            ApiException apiException =>
                apiException.Code,

            ArgumentException =>
                "BAD_REQUEST",

            UnauthorizedAccessException =>
                "UNAUTHORIZED",

            KeyNotFoundException =>
                "NOT_FOUND",

            _ =>
                "INTERNAL_SERVER_ERROR"
        };

        var message = exception switch
        {
            ApiException apiException =>
                apiException.Message,

            ArgumentException =>
                exception.Message,

            UnauthorizedAccessException =>
                "You are not authorized to perform this action.",

            KeyNotFoundException =>
                exception.Message,

            _ =>
                "An unexpected error occurred."
        };

        var response =
            GenericApiResponse<object>.Fail(
                message,
                code,
                traceId
            );

        context.Response.StatusCode = statusCode;

        context.Response.ContentType =
            "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response)
        );
    }
}