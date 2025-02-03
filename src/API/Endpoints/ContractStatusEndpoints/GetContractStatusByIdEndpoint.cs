using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_ContractStatus.Queries;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractStatusEndpoints;

public class GetContractStatusByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/contract-statuses/{id:guid}", Handle)
            .WithSummary("Get contract status by id")
            .WithTags("Contract Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id
        )
    {
        Result<GetContractStatusById.Response> result = await sender.Send(new GetContractStatusById.Query(id));
        return result.MapResult();
    }
}