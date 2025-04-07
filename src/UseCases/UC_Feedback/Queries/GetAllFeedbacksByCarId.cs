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

public sealed class GetAllFeedbacksByCarId
{
    public sealed record Query(Guid CarId, int PageNumber = 1, int PageSize = 10)
        : IRequest<Result<Response>>;

    public sealed record FeedbackResponse(
        Guid Id,
        int Rating,
        string Content,
        string FromUserName,
        string ToUserName,
        FeedbackTypeEnum Type,
        DateTimeOffset CreatedAt
    )
    {
        public static FeedbackResponse FromEntity(Feedback feedback) =>
            new(
                feedback.Id,
                feedback.Point,
                feedback.Content,
                feedback.User.Name,
                feedback.Type == FeedbackTypeEnum.ToDriver
                    ? feedback.Booking.User.Name
                    : feedback.Booking.Car.Owner.Name,
                feedback.Type,
                GetTimestampFromUuid.Execute(feedback.Id)
            );
    }

    public sealed record Response(
        CarFeedbackStats Stats,
        OffsetPaginatedResponse<FeedbackResponse> Feedbacks
    );

    public sealed record CarFeedbackStats(
        int TotalFeedbacks,
        double AverageRating,
        Dictionary<int, int> RatingDistribution // Key: rating value (1-5), Value: count
    );

    internal sealed class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Get car and validate access
            var car = await context
                .Cars.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car == null)
                return Result.NotFound("Không tìm thấy xe");

            // Build base query for all car feedbacks
            var baseQuery = context
                .Feedbacks.AsNoTracking()
                .Where(f => f.Booking.CarId == request.CarId && f.Type == FeedbackTypeEnum.ToOwner);

            // Calculate statistics
            var stats = await CalculateCarFeedbackStats(baseQuery, cancellationToken);

            // Build query for paginated feedbacks
            var query = baseQuery
                .AsSplitQuery()
                .Include(f => f.User) // Feedback author
                .Include(f => f.Booking)
                .ThenInclude(b => b.User) // Driver
                .Include(f => f.Booking.Car)
                .ThenInclude(c => c.Owner); // Car owner

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and get results
            var feedbacks = await query
                .OrderByDescending(f => f.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Check if there are more results
            bool hasNext = await query
                .Skip(request.PageSize * request.PageNumber)
                .AnyAsync(cancellationToken);

            var paginatedResponse = OffsetPaginatedResponse<FeedbackResponse>.Map(
                feedbacks.Select(FeedbackResponse.FromEntity),
                totalCount,
                request.PageNumber,
                request.PageSize,
                hasNext
            );

            return Result.Success(new Response(stats, paginatedResponse), ResponseMessages.Fetched);
        }

        private static async Task<CarFeedbackStats> CalculateCarFeedbackStats(
            IQueryable<Feedback> query,
            CancellationToken cancellationToken
        )
        {
            var feedbackStats = await query
                .GroupBy(f => true)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    AverageRating = g.Average(f => f.Point),
                    Distribution = g.GroupBy(f => f.Point)
                        .Select(rg => new { Rating = rg.Key, Count = rg.Count() })
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (feedbackStats == null)
            {
                return new CarFeedbackStats(
                    0,
                    0,
                    new Dictionary<int, int>
                    {
                        { 1, 0 },
                        { 2, 0 },
                        { 3, 0 },
                        { 4, 0 },
                        { 5, 0 }
                    }
                );
            }

            var distribution = await query
                .GroupBy(f => f.Point)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Rating, x => x.Count, cancellationToken);

            // Ensure all rating values (1-5) are present in the dictionary
            for (int i = 1; i <= 5; i++)
            {
                if (!distribution.ContainsKey(i))
                {
                    distribution[i] = 0;
                }
            }

            return new CarFeedbackStats(
                feedbackStats.TotalCount,
                Math.Round(feedbackStats.AverageRating, 1),
                distribution
            );
        }
    }
}
