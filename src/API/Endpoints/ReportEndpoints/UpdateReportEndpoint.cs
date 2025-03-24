using API.Utils;
using Carter;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using UseCases.UC_Report.Commands;

namespace API.Endpoints.ReportEndpoints;

public class UpdateReportEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/reports/{id:guid}", Handle)
            .WithSummary("Update a report")
            .WithTags("Reports")
            .DisableAntiforgery()
            .RequireAuthorization();
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] BookingReportStatus status,
        [FromForm] string? resolutionComments,
        IFormFile[]? newImages,
        CancellationToken cancellationToken
    )
    {
        var result = await sender.Send(
            new UpdateReport.Command(id, title, description, status, newImages),
            cancellationToken
        );
        return result.MapResult();
    }
}
