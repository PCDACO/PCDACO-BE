using System.Net;

using API.Utils;

using Domain.Exceptions;

using Microsoft.AspNetCore.Diagnostics;

using UseCases.DTOs;

namespace API.Middlewares;

public class ValidationAppExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";
        if (exception is not ValidationAppException ex)
            return false;
        httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
        httpContext.Response.ContentType = "application/json";

        var response = new ResponseResult<string>
        {
            IsSuccess = false,
            Value = null!,
            Message = ex.Errors.JoinReadOnlyDictionary(),
        };

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}