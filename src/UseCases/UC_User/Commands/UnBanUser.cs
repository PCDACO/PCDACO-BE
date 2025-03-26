using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_User.Commands;

public class UnBanUser
{
    public sealed record Command(Guid UserId) : IRequest<Result<Response>>;

    public sealed record Response(Guid UserId, bool IsBanned)
    {
        public static Response FromEntity(User entity) => new(entity.Id, entity.IsBanned);
    };

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsConsultant())
                return Result.Forbidden("Bạn không có quyền thực hiện hành động này");

            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Id == request.UserId,
                cancellationToken
            );

            if (user == null)
                return Result.NotFound("Không tìm thấy người dùng");

            if (!user.IsBanned)
                return Result.Error("Người dùng không bị cấm");

            user.IsBanned = false;
            user.BannedReason = string.Empty;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(Response.FromEntity(user), "Đã hủy cấm người dùng");
        }
    }
}
