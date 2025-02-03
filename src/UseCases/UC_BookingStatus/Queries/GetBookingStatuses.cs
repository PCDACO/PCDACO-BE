using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_BookingStatus.Queries;

public class GetBookingStatuses
{
    public record Query(int PageSize, int PageNumber)
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(Guid Id, string Name, DateTimeOffset CreatedAt)
    {
        public static Response FromEntity(BookingStatus entity) => new(
            entity.Id,
            entity.Name,
            GetTimestampFromUuid.Execute(entity.Id)
        );
    };

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<BookingStatus> query = context.BookingStatuses
                .OrderByDescending(x => x.Id)
                .Where(x => !x.IsDeleted);
            int count = await query.CountAsync(cancellationToken);
            List<Response> data = await query
                .Skip(request.PageSize * (request.PageNumber - 1))
                .Take(request.PageSize)
                .Select(bs => Response.FromEntity(bs))
                .ToListAsync(cancellationToken);
            return Result.Success(
                new OffsetPaginatedResponse<Response>(
                    data,
                    count,
                    request.PageNumber,
                    request.PageSize
                    ),
                "Lấy trạng thái thành công"
            );
        }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Kích thước trang phải lớn hơn 0");
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0");
        }
    }
}