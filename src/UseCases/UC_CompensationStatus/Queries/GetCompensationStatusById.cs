
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_CompensationStatus.Queries;

public class GetCompensationStatusById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(Domain.Entities.CompensationStatus entity) =>
            new Response(
                entity.Id,
                entity.Name,
                GetTimestampFromUuid.Execute(entity.Id)
            );
    };

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Unauthorized("Bạn không có quyền truy cập");
            Response? gettingEntity = await context.CompensationStatuses
                .Where(e => e.Id == request.Id && !e.IsDeleted)
                .Select(e => Response.FromEntity(e))
                .FirstOrDefaultAsync(cancellationToken);
            if (gettingEntity is null)
                return Result.NotFound("Không tìm thấy trạng thái bồi thường");
            return Result.Success(gettingEntity, "Lấy trạng thái bồi thường thành công");
        }
    }
}