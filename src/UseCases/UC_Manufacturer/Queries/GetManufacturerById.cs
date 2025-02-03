using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_Manufacturer.Queries;

public sealed class GetManufacturerById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(Guid Id, string Name, DateTimeOffset CreatedAt)
    {
        public static Response FromEntity(Manufacturer manufacturer) =>
            new(manufacturer.Id, manufacturer.Name, GetTimestampFromUuid.Execute(manufacturer.Id));
    };

    public class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            Manufacturer? manufacturer = await context
                .Manufacturers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (manufacturer is null)
                return Result.NotFound("Không tìm thấy hãng xe");

            return Result.Success(
                Response.FromEntity(manufacturer),
                "Lấy thông tin hãng xe thành công"
            );
        }
    }
}
