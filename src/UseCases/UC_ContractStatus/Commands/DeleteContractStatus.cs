
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_ContractStatus.Commands;

public class DeleteContractStatus
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
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác");
            ContractStatus? deletingContractStatus = await context.ContractStatuses
                .Include(x => x.Contracts)
                .Where(x => !x.IsDeleted)
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (deletingContractStatus is null)
                return Result.Error("Không tìm thấy trạng thái");
            if (deletingContractStatus.Contracts.Count != 0)
                return Result.Error("Không thể xóa trạng thái hợp đồng được sử dụng");
            deletingContractStatus.Delete();
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage("Xóa trạng thái hợp đồng thành công");
        }
    }
}