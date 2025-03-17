using API.Utils;

using Ardalis.Result;

using Carter;

using Infrastructure.Idempotency;

using MediatR;

using UseCases.UC_License.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;



namespace API.Endpoints.LicenseEndpoints;



public class AddUserLicenseEndpoint : ICarterModule

{

    public void AddRoutes(IEndpointRouteBuilder app)

    {

        app.MapPost("/api/users/license", Handle)

            .WithName("AddUserLicense")

            .WithSummary("Add user license information for owner and driver")

            .WithTags("Licenses")

            .RequireAuthorization()

            .AddEndpointFilter<IdempotencyFilter>()

            .DisableAntiforgery();

    }



    private async Task<IResult> Handle(

        ISender sender,

        AddUserLicenseRequest request,

        CancellationToken cancellationToken

    )

    {

        Result<AddUserLicense.Response> result = await sender.Send(

            new AddUserLicense.Command(

                LicenseNumber: request.LicenseNumber,

                ExpirationDate: request.ExpirationDate

            ),

            cancellationToken

        );

        return result.MapResult();

    }



    private record AddUserLicenseRequest(string LicenseNumber, DateTimeOffset ExpirationDate);

}