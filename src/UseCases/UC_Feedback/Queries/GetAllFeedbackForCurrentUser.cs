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

public sealed class GetAllFeedbackForCurrentUser
{
    public sealed record Query(int PageNumber = 1, int PageSize = 10, string Keyword = "")
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        int Rating,
        string Content,
        string FromUserName,
        string CarModelName,
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
                feedback.Booking.Car.Model.Name,
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
            // Ensure user is either Driver or Owner
            if (!currentUser.User!.IsDriver() && !currentUser.User.IsOwner())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Build query based on user type
            var query = context
                .Feedbacks.AsNoTracking()
                .Include(f => f.User)
                .Include(f => f.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Model)
                .Where(f => !f.IsDeleted)
                .AsQueryable();

            // Filter to get feedbacks for the current user
            if (currentUser.User.IsDriver())
            {
                // Get feedbacks where the current user was the driver and feedback came from owners (Type = Owner)
                query = query.Where(f =>
                    f.Booking.UserId == currentUser.User.Id && f.Type == FeedbackTypeEnum.ToDriver
                );
            }
            else if (currentUser.User.IsOwner())
            {
                // Get feedbacks where the current user was the owner and feedback came from drivers (Type = Driver)
                query = query.Where(f =>
                    f.Booking.Car.OwnerId == currentUser.User.Id
                    && f.Type == FeedbackTypeEnum.ToOwner
                );
            }

            // Apply keyword filter if provided
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                query = query.Where(f =>
                    EF.Functions.ILike(f.Content, $"%{request.Keyword}%")
                    || EF.Functions.ILike(f.User.Name, $"%{request.Keyword}%")
                );
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and get results
            var feedbacks = await query
                .OrderByDescending(f => f.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Check if there are more results
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();

            // Map to response
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
