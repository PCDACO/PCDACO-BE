using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_CarReport.Commands;

public sealed class CreateCarReport
{
    public record Command(Guid CarId, string Title, string Description, CarReportType ReportType)
        : IRequest<Result<Response>>;

    public record Response(Guid Id)
    {
        public static Response FromEntity(CarReport report) => new(report.Id);
    }

    internal class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Check if car exists and belongs to the current user
            var car = await context
                .Cars.Include(c => c.Owner)
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car is null)
                return Result.NotFound(ResponseMessages.CarNotFound);

            if (car.OwnerId != currentUser.User!.Id)
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            var activeBooking = await context
                .Bookings.Where(b =>
                    b.CarId == request.CarId
                    && (
                        b.Status == BookingStatusEnum.Pending
                        || b.Status == BookingStatusEnum.Approved
                        || b.Status == BookingStatusEnum.ReadyForPickup
                        || b.Status == BookingStatusEnum.Ongoing
                    )
                )
                .AnyAsync(cancellationToken);

            if (activeBooking)
                return Result.Error("Không thể báo cáo xe đang có booking hoạt động");

            var report = new CarReport
            {
                CarId = request.CarId,
                ReportedById = currentUser.User.Id,
                Title = request.Title,
                Description = request.Description,
                ReportType = request.ReportType,
                Status = CarReportStatus.Pending
            };

            await context.CarReports.AddAsync(report, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(report), ResponseMessages.Created);
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CarId).NotEmpty().WithMessage("Phải chọn xe cần báo cáo");

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Tiêu đề không được để trống")
                .MaximumLength(100)
                .WithMessage("Tiêu đề không được quá 100 ký tự");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Mô tả không được để trống")
                .MaximumLength(1000)
                .WithMessage("Mô tả không được quá 1000 ký tự");

            RuleFor(x => x.ReportType).IsInEnum().WithMessage("Loại báo cáo không hợp lệ");
        }
    }
}
