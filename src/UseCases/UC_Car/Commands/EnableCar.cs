using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Commands;

public sealed class EnableCar
{
    public record Command(Guid Id) : IRequest<Result<Response>>;

    public record Response(Guid Id, string Status)
    {
        public static Response FromEntity(Car car)
        {
            return new Response(car.Id, car.Status.ToString());
        }
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Check if user is an owner
            if (!currentUser.User!.IsOwner())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Check if car exists and not deleted
            var car = await context
                .Cars.Where(c => !c.IsDeleted)
                .Where(c => c.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (car is null)
                return Result.NotFound(ResponseMessages.CarNotFound);

            // Check if user is the owner of the car
            if (car.OwnerId != currentUser.User!.Id)
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);

            // Check if car is in the inactive status
            if (car.Status != CarStatusEnum.Inactive)
                return Result.Error(ResponseMessages.CarMustBeInactiveToBeEnabled);

            // Check if has inprogress schedule with type change gps then return error
            var inProgressSchedule = await context
                .InspectionSchedules.AsNoTracking()
                .Where(i =>
                    i.CarId == request.Id
                    && i.Status != InspectionScheduleStatusEnum.Approved
                    && i.Status != InspectionScheduleStatusEnum.Rejected
                    && i.Type == InspectionScheduleType.ChangeGPS
                )
                .FirstOrDefaultAsync(cancellationToken);

            if (inProgressSchedule is not null)
                return Result.Error(
                    "Không thể kích hoạt lại xe khi có lịch kiểm định đổi thiết bị chưa được xử lí"
                );

            // Check if car does not have any gps attached then return error
            var carGps = await context
                .CarGPSes.AsNoTracking()
                .Where(c => c.CarId == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (carGps is null)
                return Result.Error("Xe chưa được gán thiết bị gps không thể kích hoạt lại");

            // Update car status to Available
            car.Status = CarStatusEnum.Available;
            car.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                Response.FromEntity(car),
                ResponseMessages.CarEnabledSuccessfully
            );
        }
    }
}
