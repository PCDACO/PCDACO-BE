using Microsoft.AspNetCore.Http;

namespace Infrastructure.Idempotency;

internal sealed class IdempotentResult(int statusCode, object? value) : IResult
{
    private readonly int _statusCode = statusCode;
    private readonly object? _value = value;

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = _statusCode;

        return httpContext.Response.WriteAsJsonAsync(_value);
    }
}
