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
