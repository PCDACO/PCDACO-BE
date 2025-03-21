using Carter;
using MediatR;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using UseCases.UC_Contract.Queries;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace API.Endpoints.ContractEndpoints;

public class GetCarContractEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/car-contracts/{id:guid}", Handle)
            .WithSummary("Get car inspection contract")
            .WithTags("Contracts")
            .RequireAuthorization()
            .WithOpenApi(operation =>
                new(operation)
                {
                    Description = """
                    Retrieve the car inspection contract in PDF format.

                    Contract includes:
                    - Contract details and date
                    - Car owner information
                    - Technician information
                    - Car details and specifications
                    - Inspection results and photos
                    - GPS device information (if approved)

                    Notes:
                    - Returns PDF file for download
                    - File name format: HopDongKiemDinh_[timestamp].pdf
                    - All sensitive information is decrypted in the PDF
                    """,

                    Responses =
                    {
                        ["200"] = new()
                        {
                            Description = "Success - Returns PDF file",
                            Content =
                            {
                                ["application/pdf"] = new()
                                {
                                    Schema = new() { Type = "string", Format = "binary" },
                                    Example = new OpenApiString("PDF file content")
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

    private static async Task<IResult> Handle(ISender sender, Guid id)
    {
        var result = await sender.Send(new GetCarContract.Query(id));

        if (!result.IsSuccess)
            return Results.NotFound(result.Errors);

        return Results.File(result.Value.PdfFile, "application/pdf", result.Value.FileName);
    }
}
