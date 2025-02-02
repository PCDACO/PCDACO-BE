
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_TransmissionType.Commands;

public class DeleteTransmissionType
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
                return Result.Forbidden("Không có quyền xóa trạng thái");
            // Get transmission type
            TransmissionType? deletingTransmissionType = await context
                .TransmissionTypes
                .Include(x => x.Car)
                .Where(x => !x.IsDeleted)
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            // Check if transmission type is exist
            if (deletingTransmissionType is null)
                return Result.NotFound("Không tìm thấy trạng thái");
            // Check if transmission type is used
            if (deletingTransmissionType.Car!.Count != 0)
                return Result.Error("Không thể xóa trạng thái được sử dụng");
            // Delete transmission type
            deletingTransmissionType.Delete();
            await context.SaveChangesAsync(cancellationToken);
            // Return result
            return Result.SuccessWithMessage("Xóa trạng thái thành công");
        }
    }
}