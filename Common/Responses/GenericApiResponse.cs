namespace ProjectFlow.Api.Common.Responses;

public class GenericApiResponse<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = "";

    public T? Data { get; set; }

    public object? Meta { get; set; }

    public string? Code { get; set; }

    public List<GenericApiError> Errors { get; set; } = [];

    public string? TraceId { get; set; }


    // ============================================================
    // SUCCESS
    // ============================================================

    public static GenericApiResponse<T> Ok(
        T data,
        string message = "Request successful.",
        object? meta = null)
    {
        return new GenericApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Meta = meta,
            Code = null,
            Errors = [],
            TraceId = null
        };
    }


    // ============================================================
    // CREATED
    // ============================================================

    public static GenericApiResponse<T> Created(
        T data,
        string message = "Resource created successfully.",
        object? meta = null)
    {
        return new GenericApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Meta = meta,
            Code = null,
            Errors = [],
            TraceId = null
        };
    }


    // ============================================================
    // ERROR
    // ============================================================

    public static GenericApiResponse<T> Fail(
        string message,
        string code,
        string? traceId = null,
        object? meta = null)
    {
        return new GenericApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Meta = meta,
            Code = code,
            Errors = [],
            TraceId = traceId
        };
    }


    // ============================================================
    // VALIDATION ERROR
    // ============================================================

    public static GenericApiResponse<T> ValidationError(
        IEnumerable<GenericApiError> errors,
        string message = "Validation failed.",
        string? traceId = null)
    {
        return new GenericApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Meta = null,
            Code = "VALIDATION_ERROR",
            Errors = errors.ToList(),
            TraceId = traceId
        };
    }

    internal static object? ValidationError(List<Responses.GenericApiResponse> errors, string v, string traceIdentifier)
    {
        throw new NotImplementedException();
    }
}

internal class GenericApiResponse
{
}


// ================================================================
// GENERIC API ERROR
// ================================================================

public class GenericApiError
{
    public string? Field { get; set; }

    public string Code { get; set; } = "";

    public string Message { get; set; } = "";
}