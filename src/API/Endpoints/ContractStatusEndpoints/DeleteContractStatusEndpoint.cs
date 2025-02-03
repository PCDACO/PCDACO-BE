using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_ContractStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractStatusEndpoints;

public class DeleteContractStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/contract-statuses/{id:guid}", Handle)
            .WithSummary("Delete a contract status")
            .WithTags("Contract Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new DeleteContractStatus.Command(id));
        return result.MapResult();
    }
}