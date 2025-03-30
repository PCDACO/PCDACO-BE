using API.Utils;
using Carter;
using MediatR;
using Microsoft.OpenApi.Models;
using UseCases.UC_Contract.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractEndpoints;

public class GetBookingPreviewContractEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bookings/preview-contract", Handle)
            .WithSummary("Get booking contract preview")
            .WithTags("Contracts")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Generate a preview of the booking contract before creating/approving a booking.

                    This endpoint allows users to:
                    - View the contract terms and conditions before signing
                    - See all rental details including pricing
                    - Review car and party information
                    - Make an informed decision before proceeding

                    Note: This is a draft contract and not stored in the database
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns HTML content of the contract preview",
                            Content =
                            {
                                ["text/html"] = new() { Schema = new() { Type = "string" } }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["404"] = new() { Description = "Not Found - Car or user not found" }
                    }
                }
            );
    }

    private static async Task<IResult> Handle(
        ISender sender,
        Guid carId,
        DateTimeOffset startTime,
        DateTimeOffset endTime
    )
    {
        var result = await sender.Send(
            new GetBookingPreviewContract.Query(carId, startTime, endTime)
        );

        if (!result.IsSuccess)
            return Results.NotFound(result.Errors);

        return Results.Content(result.Value.HtmlContent, "text/html");
    }
}
