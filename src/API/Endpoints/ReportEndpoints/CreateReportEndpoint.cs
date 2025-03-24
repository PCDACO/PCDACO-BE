using API.Utils;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Report.Commands;

namespace API.Endpoints.ReportEndpoints;

public class CreateReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reports", Handle)
            .WithSummary("Create a new report")
            .WithTags("Reports")
            .DisableAntiforgery()
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        [FromForm] CreateReport.Command request,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(request, cancellationToken);
        return result.MapResult();
    }
}
