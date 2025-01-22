
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Amenity.Commands;

public sealed class UpdateAmenity
{
    public record Command(
        Guid Id,
        string Name,
        string Description
    ) : IRequest<Result>;

    private class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin()) return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");
            Amenity? updatingAmenity = await context.Amenities
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
            if (updatingAmenity is null) return Result.NotFound("Không tìm thấy tiện ích");
            // Update the amenity
            updatingAmenity.Name = request.Name;
            updatingAmenity.Description = request.Description;
            updatingAmenity.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Cập nhật tiện ích thành công");
        }
    }
}