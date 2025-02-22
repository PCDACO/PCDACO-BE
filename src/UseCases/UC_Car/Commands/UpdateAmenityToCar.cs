
using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Commands;

public class UpdateAmenityToCar
{
    public record Command(
        Guid CarId,
        Guid[] AmenityId) : IRequest<Result>;


    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (currentUser.User is null)
                return Result.Unauthorized();
            Car? updatingCar = await context.Cars
                .AsNoTracking()
                .Include(c => c.CarAmenities)
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);
            if (updatingCar is null)
                return Result.NotFound("Không tìm thấy xe");
            if (updatingCar.OwnerId != currentUser.User.Id)
                return Result.Forbidden("Không có quyền thực hiện hành động này");
            List<Amenity> amenities = await context.Amenities
                .Where(a => request.AmenityId.Contains(a.Id))
                .ToListAsync(cancellationToken);
            if (amenities.Count != request.AmenityId.Length)
                return Result.NotFound("1 số tiện ích không tồn tại");
            await context.CarAmenities
                .Where(ca => ca.CarId == request.CarId)
                .ExecuteDeleteAsync(cancellationToken);
            await context.CarAmenities.AddRangeAsync(amenities.Select(a => new CarAmenity()
            {
                CarId = request.CarId,
                AmenityId = a.Id
            }), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage(ResponseMessages.Updated);
        }
    }
}