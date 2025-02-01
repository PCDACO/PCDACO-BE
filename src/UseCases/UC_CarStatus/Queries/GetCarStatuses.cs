using System.Security.Cryptography;

using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_CarStatus.Queries;

public class GetCarStatuses
{
    public record Query(
        int PageNumber = 1,
        int PageSize = 10,
        string Keyword = ""
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(Guid Id, string Name, DateTimeOffset CreatedAt)
    {
        public static Response FromEntity(CarStatus entity)
            => new(entity.Id, entity.Name, GetTimestampFromUuid.Execute(entity.Id));
    };

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Query car statuses
            IQueryable<CarStatus> query = context
                .CarStatuses.AsNoTracking()
                .Where(cs => !cs.IsDeleted)
                .Where(cs => EF.Functions.ILike(cs.Name, $"%{request.Keyword}%"));
            // Get total result count
            int count = await query.CountAsync(cancellationToken);
            // Get car statuses
            IEnumerable<Response> carStatuses = await query
                .OrderByDescending(cs => cs.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(cs => Response.FromEntity(cs))
                .ToListAsync(cancellationToken);
            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    carStatuses,
                    count,
                    request.PageNumber,
                    request.PageSize
                ),  "Lấy danh sách trạng thái xe thành công"
            );
        }
    }
}