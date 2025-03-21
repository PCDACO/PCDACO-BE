using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class DeleteCarEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/cars/{id:guid}", Handle)
            .WithSummary("Delete a car listing")
            .WithTags("Cars")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Soft delete a car listing and its related data from the system.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can delete their own car
                    - Not available to Admin users

                    Deletion Process:
                    - Soft deletes the car and all related data:
                      * Car images
                      * Car amenities
                      * Car statistics
                      * Car reports
                      * Encryption keys
                    - Updates deletion timestamps and flags
                    - Maintains data integrity while hiding from active use

                    Note: This is a soft delete operation. Data is marked as deleted but remains in the database.
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
                                        ["message"] = new OpenApiString("Xóa thành công")
                                    }
                                }
                            }
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized to delete this car",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            "Bạn không có quyền thực hiện thao tác này"
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
                    },

                    Parameters =
                    {
                        new()
                        {
                            Name = "id",
                            In = ParameterLocation.Path,
                            Required = true,
                            Description = "The unique identifier of the car to delete",
                            Schema = new() { Type = "string", Format = "uuid" }
                        }
                    }
                }
            );
    }

    private async Task<IResult> Handle(ISender sender, Guid id)
    {
        Result result = await sender.Send(new DeleteCar.Command(id));
        return result.MapResult();
    }
}
