using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Commands;

public sealed class DeleteCar
{
    public record Command(
        Guid Id
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");
            Car? deletingCar = await context.Cars.Where(c => c.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
            if (deletingCar is null)
                return Result.NotFound("Không tìm thấy xe cần xóa");
            if (deletingCar.OwnerId != currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền xóa xe này");
            // Soft delete image car
            await context.ImageCars.Where(ic => ic.CarId == deletingCar.Id)
                .ExecuteUpdateAsync(ic =>
                ic.SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                .SetProperty(i => i.IsDeleted, true)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow
                ), cancellationToken);
            // Soft delete car amenities
            await context.CarAmenities.Where(ca => ca.CarId == deletingCar.Id)
                .ExecuteUpdateAsync(ca =>
                ca.SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                .SetProperty(i => i.IsDeleted, true)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow
                ), cancellationToken);
            // Soft delete car statistics
            await context.CarStatistics.Where(cs => cs.CarId == deletingCar.Id)
                .ExecuteUpdateAsync(cs =>
                cs.SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                .SetProperty(i => i.IsDeleted, true)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow
                ), cancellationToken);
            // Soft delete car reports
            await context.CarReports.Where(cr => cr.CarId == deletingCar.Id)
                .ExecuteUpdateAsync(cr =>
                cr.SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                .SetProperty(i => i.IsDeleted, true)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow
                ), cancellationToken);
            // Soft delete car encrypted key
            await context.EncryptionKeys.Where(cek => cek.Id == deletingCar.EncryptionKeyId)
                .ExecuteUpdateAsync(cek =>
                cek.SetProperty(i => i.DeletedAt, DateTime.UtcNow)
                .SetProperty(i => i.IsDeleted, true)
                .SetProperty(i => i.UpdatedAt, DateTime.UtcNow
                ), cancellationToken);
            deletingCar.Delete();
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}