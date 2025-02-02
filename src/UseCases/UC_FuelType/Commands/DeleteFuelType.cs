
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_FuelType.Commands;

public class DeleteFuelType
{
    public record Command(Guid Id) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check permission
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Chỉ admin mới có quyền thực hiện chức năng này");
            // Get fuel type
            FuelType? deletingFuelType = await context.FuelTypes
                .Include(x => x.Cars)
                .Where(x => !x.IsDeleted)
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            // Check if fuel type is exist
            if (deletingFuelType is null)
                return Result.NotFound("Không tìm thấy loại nhiên liệu");
            // Check if fuel type is used
            if (deletingFuelType.Cars.Count != 0)
                return Result.Error("Không thể xóa loại nhiên liệu được sử dụng");
            // Delete fuel type
            deletingFuelType.Delete();
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.SuccessWithMessage("Xóa loại nhiên liệu thành công");
        }
    }
}