using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class UploadCarImagesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/cars/{carId}/car-images", Handle)
            .WithSummary("Upload car images")
            .WithTags("Cars")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Upload and update images for a specific car.

                    Access Control:
                    - Requires authentication
                    - Only the car owner can upload images

                    Upload Process:
                    - Replaces all existing car images
                    - Uploads images to Cloudinary
                    - Stores image metadata in database

                    Image Handling:
                    - Multiple images can be uploaded simultaneously
                    - Images are categorized as 'car' type
                    - Existing images of type 'car' are removed
                    - Unique naming: Car-{carId}-Image-{index}

                    Note: This is a complete replacement operation.
                    All existing car images will be replaced with the new ones.
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
                                        ["value"] = new OpenApiObject
                                        {
                                            ["images"] = new OpenApiArray
                                            {
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174000"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://cloudinary.com/car-123-image-1.jpg"
                                                    ),
                                                    ["name"] = new OpenApiString("front-view.jpg")
                                                },
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174001"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://cloudinary.com/car-123-image-2.jpg"
                                                    ),
                                                    ["name"] = new OpenApiString("side-view.jpg")
                                                }
                                            }
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString("Cập nhật thành công")
                                    }
                                }
                            }
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Validation errors",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Phải chọn ảnh !")
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
                                            "Không có quyền cập nhật ảnh của xe này"
                                        )
                                    }
                                }
                            }
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Car or image type doesn't exist",
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

    private async Task<IResult> Handle(ISender sender, Guid carId, IFormFileCollection images)
    {
        UploadCarImages.ImageFile[] carFiles =
        [
            .. images.Select(file => new UploadCarImages.ImageFile
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName,
            }),
        ];
        Result<UploadCarImages.Response> result = await sender.Send(
            new UploadCarImages.Command(carId, carFiles)
        );
        return result.MapResult();
    }
}
