using Ardalis.Result;
using UseCases.DTOs;

namespace API.Utils;

public static class ResultMapper
{
    public static Microsoft.AspNetCore.Http.IResult MapResult<T>(this Result<T> result)
    {
        return result.Status switch
        {
            ResultStatus.Ok => TypedResults.Ok(ResponseResultMapper.Map(result)),
            ResultStatus.Created => TypedResults.Created(result.SuccessMessage, ResponseResultMapper.Map(result)),
            ResultStatus.Error => TypedResults.BadRequest(ResponseResultMapper.Map(result)),
            ResultStatus.Forbidden => TypedResults.Forbid(),
            ResultStatus.Unauthorized => TypedResults.Unauthorized(),
            ResultStatus.Invalid => TypedResults.BadRequest(ResponseResultMapper.Map(result)),
            ResultStatus.NotFound => TypedResults.NotFound(ResponseResultMapper.Map(result)),
            ResultStatus.NoContent => TypedResults.NoContent(),
            ResultStatus.Conflict => TypedResults.Conflict(ResponseResultMapper.Map(result)),
            ResultStatus.CriticalError => TypedResults.Problem(),
            ResultStatus.Unavailable => TypedResults.Problem(),
            _ => TypedResults.Problem("An unexpected error occurred")
        };
    }
}