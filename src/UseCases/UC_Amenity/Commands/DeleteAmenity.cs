using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Amenity.Commands;

public sealed class DeleteAmenity
{
    public record Command(
        Guid Id
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin()) return Result.Forbidden("Bạn không có quyền xóa tiện nghi");
            Amenity? deletingAmenity = await context.Amenities.FirstOrDefaultAsync(a => a.Id == request.Id &&
                !a.IsDeleted, cancellationToken);
            if (deletingAmenity is null)
                return Result.Error("Không tìm thấy tiện nghi");
            deletingAmenity.Delete();
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Xóa tiện nghi thành công");
        }
    }
}