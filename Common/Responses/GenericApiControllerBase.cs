using Microsoft.AspNetCore.Mvc;

namespace ProjectFlow.Api.Common.Responses;

public abstract class GenericApiControllerBase : ControllerBase
{
    protected GenericApiResponse<object?> Success(
        object? data,
        string message,
        int statusCode = StatusCodes.Status200OK,
        object? meta = null)
    {
        Response.StatusCode = statusCode;
        return GenericApiResponse<object?>.Ok(data, message, meta);
    }

    protected GenericApiResponse<object?> Failure(
        int statusCode,
        string message,
        string code)
    {
        Response.StatusCode = statusCode;
        return GenericApiResponse<object?>.Fail(
            message,
            code,
            HttpContext.TraceIdentifier
        );
    }
}
