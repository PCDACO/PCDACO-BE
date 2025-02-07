using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Auth.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.AuthEndpoints;

public class RefreshTokenEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh-token", Handle)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token")
            .WithTags("Auth")
            .AllowAnonymous();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken
    )
    {
        Result<RefreshUserToken.TokenResponse> result = await sender.Send(
            new RefreshUserToken.Command(request.RefreshToken),
            cancellationToken
        );
        return result.MapResult();
    }
}

public record RefreshTokenRequest(string RefreshToken);
