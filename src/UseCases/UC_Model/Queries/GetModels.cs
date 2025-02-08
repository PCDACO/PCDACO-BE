using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Model.Queries;

public sealed class GetModels
{
    public sealed record Query(int PageNumber = 1, int PageSize = 10, string Keyword = "")
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

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
    };

    public sealed class Handler(IAppDBContext context)
        : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            IQueryable<Model> query = context
                .Models.AsNoTracking()
                .Include(m => m.Manufacturer)
                .Where(m => !m.IsDeleted)
                .Where(m =>
                    EF.Functions.ILike(m.Name, $"%{request.Keyword}%")
                    || EF.Functions.ILike(m.Manufacturer.Name, $"%{request.Keyword}%")
                );

            int totalItems = await query.CountAsync(cancellationToken);

            var models = await query
                .OrderByDescending(m => m.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(m => Response.FromEntity(m))
                .ToListAsync(cancellationToken);

            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    models,
                    totalItems,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ),
                "Lấy danh sách mô hình xe thành công"
            );
        }
    }
}
