using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_Booking.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.TransactionEndpoints;

public class CheckCurrentPaymentStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions/{orderCode:long}/check", Handle)
            .WithSummary("Check if the orderCode is complete or not")
            .WithTags("Transactions");
    }

    internal async Task<IResult> Handle(ISender sender, long orderCode, CancellationToken CancellationToken)
    {
        Result result = await sender.Send(new CheckCurrentPaymentStatus.Query(orderCode), CancellationToken);
        return result.MapResult();
    }
}