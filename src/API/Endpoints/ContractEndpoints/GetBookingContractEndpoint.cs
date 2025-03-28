using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
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
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve the booking contract in HTML format.

                    Contract includes:
                    - Contract details and date
                    - Car owner information (name, license number, address)
                    - Driver information (name, license number, address)
                    - Car details (manufacturer, license plate, seats, color)
                    - Rental terms and conditions
                    - Rental period and pricing
                    - Pickup location
                    - Standard clauses and custom terms

                    Notes:
                    - Returns HTML content for display
                    - All sensitive information (license numbers, license plates) is decrypted in the HTML
                    - The contract includes standard clauses about rental terms, responsibilities, and dispute resolution
                    - Custom terms from the car owner are included if provided
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns HTML content",
                            Content =
                            {
                                ["text/html"] = new()
                                {
                                    Schema = new() { Type = "string" },
                                    Example = new OpenApiString("<html>...</html>")
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["404"] = new()
                        {
                            Description = "Not Found - Contract doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy hợp đồng")
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetBookingContract.Query(id));

        if (!result.IsSuccess)
            return Results.NotFound(result.Errors);

        return Results.Content(result.Value.HtmlContent, "text/html");
    }
}
