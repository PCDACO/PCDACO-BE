using API.Utils;
using Ardalis.Result;
using Carter;
using MediatR;
using UseCases.Tests.Query;

namespace API.Endpoints.TestEndpoints.Query;

public class TestEndpoint : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/test", Handle)
        .Produces<string>(200)
        .Produces(404)
        .WithSummary("Description")
        .WithTags("Test");
    }

    private static async Task<Microsoft.AspNetCore.Http.IResult> 
        Handle(ISender sender)
    {
        Result<string> result = await sender.Send(new Test.Query());
        return result.MapResult();
    }
}