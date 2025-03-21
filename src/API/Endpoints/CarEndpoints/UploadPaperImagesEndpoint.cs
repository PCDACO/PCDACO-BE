using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Car.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.CarEndpoints;

public class UploadPaperImagesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/cars/{carId}/paper-images", Handle)
            .WithSummary("Upload car paper/document images")
            .WithTags("Cars")
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Upload and update paper/document images for a specific car (e.g., registration, insurance).

                    Access Control:
                    - Requires authentication
                    - Only the car owner can upload paper images

                    Upload Process:
                    - Replaces all existing paper images
                    - Uploads images to Cloudinary
                    - Stores image metadata in database

                    Image Handling:
                    - Multiple document images can be uploaded simultaneously
                    - Images are categorized as 'paper' type
                    - Existing images of type 'paper' are removed
                    - Unique naming: Car-{carId}-Image-{index}

                    Note: This is a complete replacement operation.
                    All existing paper images will be replaced with the new ones.
                    This endpoint is specifically for document-related images,
                    not for general car photos.
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
                                                        "https://cloudinary.com/car-123-paper-1.jpg"
                                                    ),
                                                    ["name"] = new OpenApiString("registration.jpg")
                                                },
                                                new OpenApiObject
                                                {
                                                    ["id"] = new OpenApiString(
                                                        "123e4567-e89b-12d3-a456-426614174001"
                                                    ),
                                                    ["url"] = new OpenApiString(
                                                        "https://cloudinary.com/car-123-paper-2.jpg"
                                                    ),
                                                    ["name"] = new OpenApiString("insurance.jpg")
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
                            Description = "Not Found - Car or paper image type doesn't exist",
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
        UploadPaperImages.ImageFile[] imageFiles =
        [
            .. images.Select(file => new UploadPaperImages.ImageFile
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName,
            }),
        ];
        Result<UploadPaperImages.Response> result = await sender.Send(
            new UploadPaperImages.Command(carId, imageFiles)
        );
        return result.MapResult();
    }
}
