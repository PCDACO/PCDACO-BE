using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Queries;

public sealed class GetAllBookings
{
    public sealed record Query(
        int Limit,
        Guid? LastId,
        string? SearchTerm = null,
        Guid? BookingStatusId = null,
        bool? IsPaid = null
    ) : IRequest<Result<CursorPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        string CarName,
        string DriverName,
        string OwnerName,
        decimal TotalAmount,
        bool IsPaid,
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
                booking.IsPaid,
                booking.Status.Name,
                booking.StartTime,
                booking.EndTime,
                booking.ActualReturnTime
            );
    };

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<CursorPaginatedResponse<Response>>>
    {
        public async Task<Result<CursorPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var query = context
                .Bookings.Include(b => b.Status)
                .Include(b => b.Car)
                .ThenInclude(c => c.Model)
                .Include(b => b.Car)
                .ThenInclude(c => c.Owner)
                .Include(b => b.User)
                .AsQueryable();

            /// Filter by user role
            if (currentUser.User!.IsDriver())
                query = query.Where(b => b.UserId == currentUser.User.Id);

            if (currentUser.User.IsOwner())
                query = query.Where(b => b.Car.OwnerId == currentUser.User.Id);

            // Apply filters
            if (request.BookingStatusId.HasValue)
                query = query.Where(b => b.StatusId == request.BookingStatusId);

            if (request.IsPaid.HasValue)
                query = query.Where(b => b.IsPaid == request.IsPaid.Value);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                query = query.Where(b =>
                    EF.Functions.ILike(b.Car.Model.Name, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(b.User.Name, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(b.Car.Owner.Name, $"%{request.SearchTerm}%")
                );

            // Apply cursor pagination
            if (request.LastId.HasValue)
                query = query.Where(b => b.Id.CompareTo(request.LastId.Value) < 0);

            // Order by Id descending (newest first)
            query = query.OrderByDescending(b => b.Id);

            // Get total count and items
            var totalCount = await query.CountAsync(cancellationToken);
            var bookings = await query
                .Take(request.Limit)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Determine if there are more items
            var hasMore = bookings.Count > request.Limit;

            var lastId = bookings.LastOrDefault()?.Id;

            return Result.Success(
                new CursorPaginatedResponse<Response>(
                    bookings.Select(Response.FromEntity),
                    totalCount,
                    request.Limit,
                    lastId,
                    hasMore
                )
            );
        }
    }
}
