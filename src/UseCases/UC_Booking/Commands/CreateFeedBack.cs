using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Booking.Commands;

public sealed class CreateFeedBack
{
    private const int FeedbackTimeLimit = 7; // days

    public sealed record Command(Guid BookingId, int Rating, string Content) : IRequest<Result>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(Feedback feedback) => new(feedback.Id);
    }

    private static bool IsWithinFeedbackPeriod(Booking booking)
    {
        return (DateTimeOffset.UtcNow - booking.ActualReturnTime).TotalDays <= FeedbackTimeLimit;
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (currentUser.User == null)
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            var booking = await context
                .Bookings.Include(b => b.Status)
                .Include(b => b.Feedbacks)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.Error("Không tìm thấy booking");

            // Check if the user is the driver and the booking is not his
            if (currentUser.User.IsDriver() && booking.UserId != currentUser.User.Id)
                return Result.Error("Chỉ người thuê xe mới có thể đánh giá chủ xe");

            // Check if the user is the owner and the booking is not his
            if (currentUser.User.IsOwner())
            {
                var car = await context.Cars.FirstOrDefaultAsync(
                    c => c.Id == booking.CarId && c.OwnerId == currentUser.User.Id,
                    cancellationToken
                );

                if (car == null)
                    return Result.Error("Chỉ chủ xe mới có thể đánh giá người thuê");
            }

            if (!IsWithinFeedbackPeriod(booking))
                return Result.Error(
                    $"Chỉ có thể đánh giá trong vòng {FeedbackTimeLimit} ngày sau khi kết thúc chuyến đi"
                );

            var existedFeedback = booking.Feedbacks.FirstOrDefault(f =>
                f.UserId == currentUser.User.Id
            );

            if (existedFeedback != null)
                return Result.Error("Feedback đã tồn tại");

            if (booking.Status.Name != BookingStatusEnum.Completed.ToString())
                return Result.Error("Chỉ có thể tạo feedback khi chuyến đi đã hoàn thành");

            var feedbackType = currentUser.User.IsDriver()
                ? FeedbackTypeEnum.Driver
                : FeedbackTypeEnum.Owner;

            var feedback = new Feedback
            {
                BookingId = booking.Id,
                UserId = currentUser.User.Id,
                Point = request.Rating,
                Content = request.Content,
                Type = feedbackType
            };

            context.Feedbacks.Add(feedback);
            await context.SaveChangesAsync(cancellationToken);

            return Result.SuccessWithMessage("Tạo feedback thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Nội dung feedback không được để trống")
                .MaximumLength(500)
                .WithMessage("Nội dung feedback không được vượt quá 500 ký tự");
            ;

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5)
                .WithMessage("Điểm đánh giá phải nằm trong khoảng từ 1 đến 5");
        }
    }
}
