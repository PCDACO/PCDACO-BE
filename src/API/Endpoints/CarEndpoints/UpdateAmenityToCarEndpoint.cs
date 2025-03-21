using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class UpdateAmenityToCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/cars/{id:guid}/amenities", Handle)
            .WithSummary("Update amenities for a specific car")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Update the list of amenities associated with a specific car.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can update their car's amenities

                    Operation Details:
                    - Replaces all existing amenities with the new list
                    - Performs complete validation of all amenity IDs
                    - Executes as a transaction

                    Process:
                    1. Validates car ownership
                    2. Verifies all amenity IDs exist
                    3. Removes all existing amenities
                    4. Adds the new amenities

                    Note: This is a full replacement operation, not a partial update.
                    All existing amenities will be replaced with the new list.
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Cập nhật thành công")
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid amenity IDs",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "1 số tiện ích không tồn tại"
                                        )
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User is not the car owner",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Không có quyền thực hiện hành động này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy xe")
                                    }
                                }
                            }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id, UpdateAmenityToCarRequest request)
    {
        Result result = await sender.Send(
            new UpdateAmenityToCar.Command(CarId: id, AmenityId: request.AmenityId)
        );
        return result.MapResult();
    }

    private record UpdateAmenityToCarRequest(Guid[] AmenityId);
}
