using System.Net;
using Microsoft.AspNetCore.Diagnostics;

using UseCases.DTOs;

namespace API.Middlewares;

public class ExceptionHandlerMiddleware : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new ResponseResult<string>
        {
            IsSuccess = false,
            Value = null!,
            Message = exception.Message,
        };

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}