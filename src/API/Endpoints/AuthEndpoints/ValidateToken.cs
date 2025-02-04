using API.Utils;

using Ardalis.Result;

using Carter;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AuthEndpoints;

public class ValidateToken : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/validate-token", Handle)
            .WithSummary("Validate token")
            .WithTags("Auth")
            .RequireAuthorization();
    }
    private IResult Handle()
    {
        Result result = Result.SuccessWithMessage("Success");
        return result.MapResult();
    }
}