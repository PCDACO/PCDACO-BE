using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Manufacturer.Commands
{
    public sealed class DeleteManufacturer
    {
        public sealed record Command(Guid Id) : IRequest<Result>;

        public class Handler(IAppDBContext context, CurrentUser currentUser)
            : IRequestHandler<Command, Result>
        {
            public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
            {
                if (!currentUser.User!.IsAdmin())
                    return Result.Error("Bạn không có quyền xóa hãng xe");

                Manufacturer? deletingManufacturer =
                    await context.Manufacturers.FirstOrDefaultAsync(
                        m => m.Id == request.Id && !m.IsDeleted,
                        cancellationToken
                    );
                if (deletingManufacturer is null)
                    return Result.NotFound("Không tìm thấy hãng xe");

                deletingManufacturer.Delete();
                await context.SaveChangesAsync(cancellationToken);
                return Result.SuccessWithMessage("Xóa hãng xe thành công");
            }
        }
    }
}
