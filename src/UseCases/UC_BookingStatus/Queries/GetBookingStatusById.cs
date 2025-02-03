
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_BookingStatus.Queries;

public class GetBookingStatusById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(BookingStatus entity) => new(
            entity.Id,
            entity.Name,
            GetTimestampFromUuid.Execute(entity.Id)
        );
    };

    private class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Response? result = await context.BookingStatuses
                .Where(bs => bs.Id == request.Id)
                .Where(bs => !bs.IsDeleted)
                .Select(bs => Response.FromEntity(bs))
                .FirstOrDefaultAsync(cancellationToken);
            if (result is null)
                return Result.NotFound("Không tìm thấy trạng thái");
            return Result.Success(result, "Lấy trạng thái thành công");
        }
    }
}