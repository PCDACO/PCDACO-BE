using Carter;
using MediatR;
using UseCases.UC_Contract.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.UserEndpoints;

public class GetBookingContractEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/{id:guid}/contract", Handle)
            .WithSummary("Get booking contract")
            .WithTags("Bookings")
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetBookingContract.Query(id));

        return Results.File(result.Value.PdfFile, "application/pdf", result.Value.FileName);
    }
}
