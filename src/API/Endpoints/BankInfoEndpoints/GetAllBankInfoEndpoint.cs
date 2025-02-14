using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_BankInfo.Queries;

namespace API.Endpoints.BankInfoEndpoints;

public class GetAllBankInfoEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bank-info", Handle)
            .WithSummary("Get all bank info")
            .WithTags("BankInfo")
            .RequireAuthorization();
    }

    private static async Task<IResult> Handle(
        ISender sender,
        [FromQuery(Name = "search")] string? searchTerm,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(new GetAllBankInfo.Query(searchTerm), cancellationToken);

        return result.MapResult();
    }
}
