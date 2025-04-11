using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Queries;

public sealed class GetAllBookings
{
    public sealed record Query(
        int PageNumber = 1,
        int PageSize = 10,
        string? SearchTerm = null,
        int[]? BookingStatuses = null,
        bool? IsPaid = null
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        string CarName,
        string DriverName,
        string OwnerName,
        decimal TotalAmount,
        decimal TotalDistance,
        bool IsPaid,
        bool IsRefund,
        string Status,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime,
        DateTimeOffset ActualReturnTime
    )
    {
        public static Response FromEntity(Booking booking) =>
            new(
                booking.Id,
                booking.Car.Model.Name,
                booking.User.Name, // Driver
                booking.Car.Owner.Name, // Owner
                booking.TotalAmount,
                booking.TotalDistance,
                booking.IsPaid,
                booking.IsRefund,
                booking.Status.ToString(),
                booking.StartTime,
                booking.EndTime,
                booking.ActualReturnTime
            );
    };

    internal sealed class Handler(
        IAppDBContext context,
        ILogger<Handler> logger,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = context
                .Bookings.Include(b => b.Car)
                .ThenInclude(c => c.Model)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .Include(b => b.User)
                .AsQueryable();

            logger.LogInformation("{UserRole}", currentUser.User?.Role);

            // TODO: Get all Bookings when Owner create

            /// Filter by user role
            if (currentUser.User!.IsDriver())
                query = query.Where(b => b.UserId == currentUser.User.Id);

            if (currentUser.User.IsOwner())
                query = query.Where(b => b.Car.OwnerId == currentUser.User.Id);

            // Apply filters
            if (request.BookingStatuses != null && request.BookingStatuses.Length > 0)
            {
                var statuses = request.BookingStatuses.Select(s => (BookingStatusEnum)s).ToList();
                query = query.Where(b => statuses.Contains(b.Status));
            }

            if (request.IsPaid.HasValue)
                query = query.Where(b => b.IsPaid == request.IsPaid.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                query = query.Where(b =>
                    EF.Functions.ILike(b.Car.Model.Name, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(b.User.Name, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(b.Car.Owner.Name, $"%{request.SearchTerm}%")
                );

            // Order by Id descending (newest first)
            query = query.OrderByDescending(b => b.Id);

            var totalCount = await query.CountAsync(cancellationToken);

            var bookings = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var hasNext = await query
                .Skip(request.PageSize * request.PageNumber)
                .AnyAsync(cancellationToken: cancellationToken);

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    bookings.Select(Response.FromEntity),
                    totalCount,
                    request.PageSize,
                    request.PageNumber,
                    hasNext
                ),
                "Lấy danh sách đặt xe thành công"
            );
        }
    }
}
