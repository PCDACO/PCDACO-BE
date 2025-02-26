using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.UC_User.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class UploadUserAvatarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/users/{id:guid}/avatar", Handle)
            .WithSummary("Upload user's avatar")
            .WithTags("Users")
            .DisableAntiforgery()
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        IFormFile avatar,
        CancellationToken cancellationToken
    )
    {
        Result<UploadUserAvatar.Response> result = await sender.Send(
            new UploadUserAvatar.Command(id, avatar.OpenReadStream()),
            cancellationToken
        );
        return result.MapResult();
    }
}
