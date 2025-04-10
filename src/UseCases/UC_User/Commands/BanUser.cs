using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_User.Commands;

public class BanUser
{
    public sealed record Command(Guid UserId, string BannedReason) : IRequest<Result<Response>>;

    public sealed record Response(Guid UserId, bool IsBanned, string BannedReason)
    {
        public static Response FromEntity(User entity) =>
            new(entity.Id, entity.IsBanned, entity.BannedReason);
    };

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsConsultant() && !currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện hành động này");

            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Id == request.UserId,
                cancellationToken
            );

            if (user == null)
                return Result.NotFound("Không tìm thấy người dùng");

            if (user.IsBanned)
                return Result.Error("Người dùng đã bị cấm");

            user.IsBanned = true;
            user.BannedReason = request.BannedReason;

            if (user.IsOwner())
            {
                await context
                    .Cars.Where(c => c.OwnerId == user.Id)
                    .ExecuteUpdateAsync(
                        c => c.SetProperty(c => c.Status, CarStatusEnum.Inactive),
                        cancellationToken: cancellationToken
                    );
            }

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(user), "Người dùng đã bị cấm");
        }
    }
}
