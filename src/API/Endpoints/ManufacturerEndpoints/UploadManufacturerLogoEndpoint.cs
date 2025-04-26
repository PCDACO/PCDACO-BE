using API.Utils;
using Ardalis.Result;
using Carter;
using Domain.Constants;
using MediatR;
using Microsoft.OpenApi.Any;
using UseCases.UC_Manufacturer.Commands;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ManufacturerEndpoints;

public class UploadManufacturerLogoEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/manufacturers/{id:guid}/logo", Handle)
            .WithSummary("Upload manufacturer logo image")
            .WithTags("Manufacturers")
            .DisableAntiforgery()
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Upload a logo image for a specific manufacturer.

                    Access Control:
                    - Requires authentication
                    - Only available to users with Admin role

                    Upload Process:
                    - Validates manufacturer exists
                    - Uploads SVG image to Cloudinary
                    - Updates manufacturer record with new logo URL

                    Image Requirements:
                    - Format: SVG only
                    - Maximum file size: 10MB
                    - Image will be named using pattern: Manufacturer-{id}-Logo-{uuid}

                    Notes:
                    - Previous logo will be replaced automatically
                    - Administrative audit trail is maintained
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Logo uploaded successfully",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["value"] = new OpenApiObject
                                        {
                                            ["manufacturerId"] = new OpenApiString(
                                                "123e4567-e89b-12d3-a456-426614174000"
                                            ),
                                            ["logoUrl"] = new OpenApiString(
                                                "https://res.cloudinary.com/example/image/upload/v1234567890/manufacturer-logo/Manufacturer-123e4567-e89b-12d3-a456-426614174000-Logo-456e6789-f89b-13d3-c456-326614174321.svg"
                                            ),
                                        },
                                        ["isSuccess"] = new OpenApiBoolean(true),
                                        ["message"] = new OpenApiString(
                                            "Cập nhật logo hãng xe thành công"
                                        ),
                                    },
                                },
                            },
                        },
                        ["400"] = new()
                        {
                            Description = "Bad Request - Invalid file format or size",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Validation failed"),
                                        ["errors"] = new OpenApiArray
                                        {
                                            new OpenApiString("Chỉ chấp nhận các định dạng: .svg"),
                                            new OpenApiString(
                                                "Kích thước ảnh logo không được lớn hơn 10MB"
                                            ),
                                        },
                                    },
                                },
                            },
                        },
                        ["401"] = new() { Description = "Unauthorized - User not authenticated" },
                        ["403"] = new()
                        {
                            Description = "Forbidden - User not authorized (non-admin)",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString(
                                            ResponseMessages.ForbiddenAudit
                                        ),
                                    },
                                },
                            },
                        },
                        ["404"] = new()
                        {
                            Description = "Not Found - Manufacturer doesn't exist",
                            Content =
                            {
                                ["application/json"] = new()
                                {
                                    Example = new OpenApiObject
                                    {
                                        ["isSuccess"] = new OpenApiBoolean(false),
                                        ["message"] = new OpenApiString("Không tìm thấy hãng xe"),
                                    },
                                },
                            },
                        },
                    },
                }
            );
    }

    private async Task<IResult> Handle(
        ISender sender,
        Guid id,
        IFormFile logo,
        CancellationToken cancellationToken
    )
    {
        // Create a copy stream
        using var logoStream = new MemoryStream();
        await logo.CopyToAsync(logoStream, cancellationToken);
        logoStream.Position = 0; // Reset position to the beginning

        Result<UploadManufacturerLogo.Response> result = await sender.Send(
            new UploadManufacturerLogo.Command(id, logoStream),
            cancellationToken
        );
        return result.MapResult();
    }
}
