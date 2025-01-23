using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Manufacturer;

public sealed class UpdateManufacturer
{
    public record Command(Guid Id, string ManufacturerName) : IRequest<Result>;

    private class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");

            // Check if manufacturer is exist
            Manufacturer? updatingManufacturer = await context.Manufacturers.FirstOrDefaultAsync(
                m => m.Id == request.Id,
                cancellationToken
            );
            if (updatingManufacturer is null)
                return Result.NotFound("Không tìm thấy hãng xe");

            // Update the manufacturer
            updatingManufacturer.Name = request.ManufacturerName;
            updatingManufacturer.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Cập nhật hãng xe thành công");
        }
    }
}
