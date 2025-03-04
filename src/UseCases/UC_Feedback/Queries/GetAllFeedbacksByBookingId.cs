using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Feedback.Queries;

public sealed class GetAllFeedbacksByBookingId
{
    public sealed record Query(
        Guid BookingId,
        int PageNumber = 1,
        int PageSize = 10,
        string Keyword = ""
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        int Rating,
        string Content,
        string FromUserName,
        string ToUserName,
        FeedbackTypeEnum Type,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(Feedback feedback) =>
            new(
                feedback.Id,
                feedback.Point,
                feedback.Content,
                feedback.User.Name,
                feedback.Type == FeedbackTypeEnum.Owner
                    ? feedback.Booking.User.Name
                    : feedback.Booking.Car.Owner.Name,
                feedback.Type,
                GetTimestampFromUuid.Execute(feedback.Id)
            );
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Get booking and validate access
            var booking = await context
                .Bookings.AsNoTracking()
                .Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy đơn đặt xe");

            // Only allow access for the driver, owner, or admin
            if (
                !currentUser.User!.IsAdmin()
                && !currentUser.User!.IsDriver()
                && !currentUser.User!.IsOwner()
                && booking.UserId != currentUser.User.Id
                && booking.Car.OwnerId != currentUser.User.Id
            )
            {
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            }

            // Build query
            var query = context
                .Feedbacks.AsNoTracking()
                .AsSplitQuery()
                .Include(f => f.User) // Feedback author
                .Include(f => f.Booking)
                .ThenInclude(b => b.User) // Driver
                .Include(f => f.Booking.Car)
                .ThenInclude(c => c.Owner) // Car owner
                .Where(f => f.BookingId == request.BookingId && !f.IsDeleted);

            // Apply keyword filter if provided
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                query = query.Where(f =>
                    EF.Functions.ILike(f.Content, $"%{request.Keyword}%") //find matching content
                    || EF.Functions.ILike(f.User.Name, $"%{request.Keyword}%") //find matching feedback author
                    || (
                        f.Type == FeedbackTypeEnum.Owner
                        && EF.Functions.ILike(f.Booking.User.Name, $"%{request.Keyword}%")
                    ) //find matching feedback receiver - driver
                    || (
                        f.Type == FeedbackTypeEnum.Driver
                        && EF.Functions.ILike(f.Booking.Car.Owner.Name, $"%{request.Keyword}%")
                    ) //find matching feedback receiver - car owner
                );
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and get results
            var feedbacks = await query
                .OrderByDescending(f => f.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Check if there are more results
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    feedbacks.Select(Response.FromEntity),
                    totalCount,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ),
                ResponseMessages.Fetched
            );
        }
    }
}
