using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Model.Commands;

public sealed class DeleteModel
{
    public record Command(Guid Id) : IRequest<Result>;

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Error("Bạn không có quyền thực hiện chức năng này !");

            var model = await context.Models.FirstOrDefaultAsync(
                m => m.Id == request.Id && !m.IsDeleted,
                cancellationToken
            );

            if (model is null)
                return Result.Error("Mô hình xe không tồn tại");

            // Check if model is being used by any cars
            var hasRelatedCars = await context.Cars.AnyAsync(
                c => c.ModelId == model.Id && !c.IsDeleted,
                cancellationToken
            );

            if (hasRelatedCars)
                return Result.Error("Không thể xóa mô hình xe đang được sử dụng");

            model.Delete();

            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Xóa mô hình xe thành công");
        }
    }
}
