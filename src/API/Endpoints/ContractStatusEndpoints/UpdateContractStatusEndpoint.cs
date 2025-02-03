using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_ContractStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractStatusEndpoints;

public class UpdateContractStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/contract-statuses/{id:guid}", Handle)
            .WithSummary("Update a contract status")
            .WithTags("Contract Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        UpdateContractStatusRequest request
    )
    {
        Result result = await sender.Send(new UpdateContractStatus.Command(id, request.Name));
        return result.MapResult();
    }
    private record UpdateContractStatusRequest(string Name);
}