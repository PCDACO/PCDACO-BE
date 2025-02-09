using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_Model.Queries;

public class GetModelById
{
    public sealed record Query(Guid Id) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid Id,
        string Name,
        DateTimeOffset ReleaseDate,
        DateTimeOffset CreatedAt,
        ManufacturerDetail Manufacturer
    )
    {
        public static Response FromEntity(Model model) =>
            new(
                model.Id,
                model.Name,
                model.ReleaseDate,
                GetTimestampFromUuid.Execute(model.Id),
                ManufacturerDetail.FromEntity(model.Manufacturer!)
            );
    }

    public sealed record ManufacturerDetail(Guid Id, string Name)
    {
        public static ManufacturerDetail FromEntity(Manufacturer manufacturer) =>
            new(manufacturer.Id, manufacturer.Name);
    }

    internal sealed class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var model = await context
                .Models.AsNoTracking()
                .Include(m => m.Manufacturer)
                .FirstOrDefaultAsync(m => m.Id == request.Id && !m.IsDeleted, cancellationToken);

            if (model is null)
                return Result.NotFound("Không tìm thấy mô hình xe");

            return Result.Success(
                Response.FromEntity(model),
                "Lấy thông tin mô hình xe thành công"
            );
        }
    }
}
