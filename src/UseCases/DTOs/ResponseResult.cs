using Ardalis.Result;

namespace UseCases.DTOs;

public class ResponseResult<T>
{
    public T Value { get; set; } = default!;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public static class ResponseResultMapper
{
    public static ResponseResult<T> Map<T>(Result<T> result)
    {
        return new ResponseResult<T>
        {
            Value = result.Value,
            IsSuccess = result.IsSuccess,
            Message = result.IsSuccess ? result.SuccessMessage : string.Join(", ", result.Errors)
        };
    }
}