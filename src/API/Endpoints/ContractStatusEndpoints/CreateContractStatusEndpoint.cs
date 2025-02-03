using API.Utils;

using Ardalis.Result;

using Carter;

using MediatR;

using UseCases.UC_ContractStatus.Commands;

using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractStatusEndpoints;

public class CreateContractStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/contract-statuses", Handle)
            .WithSummary("Create a new contract status")
            .WithTags("Contract Statuses")
            .RequireAuthorization();
    }
    private async Task<IResult> Handle(ISender sender, CreateContractStatusRequest request)
    {
        Result<CreateContractStatus.Response> result = await sender.Send(new CreateContractStatus.Command(request.Name));
        return result.MapResult();
    }
    public record CreateContractStatusRequest(string Name);
}