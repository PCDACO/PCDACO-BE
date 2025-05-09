using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_User.Queries;

public class GetAllDrivers
{
    public sealed record Query(int PageNumber = 1, int PageSize = 10, string Keyword = "")
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public sealed record Response(
        Guid Id,
        string Name,
        string Email,
        string AvatarUrl,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string Role,
        DateTimeOffset CreatedAt,
        bool? IsApprovedLicense,
        bool IsBanned
    )
    {
        public static async Task<Response> FromEntity(
            User user,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedKey = keyManagementService.DecryptKey(
                user.EncryptionKey.EncryptedKey,
                masterKey
            );
            string decryptedPhone = await aesEncryptionService.Decrypt(
                user.Phone,
                decryptedKey,
                user.EncryptionKey.IV
            );

            return new(
                user.Id,
                user.Name,
                user.Email,
                user.AvatarUrl,
                user.Address,
                user.DateOfBirth,
                decryptedPhone,
                user.Role.Name,
                GetTimestampFromUuid.Execute(user.Id),
                user.LicenseIsApproved,
                user.IsBanned
            );
        }
    }

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền truy cập");

            // Build query
            IQueryable<User> query = context
                .Users.AsNoTracking()
                .Include(u => u.Role)
                .Include(u => u.EncryptionKey)
                .Where(u =>
                    u.Role != null && EF.Functions.ILike(u.Role.Name, "%Driver%") && !u.IsDeleted
                )
                .Where(u =>
                    EF.Functions.ILike(u.Name, $"%{request.Keyword}%")
                    || EF.Functions.ILike(u.Email, $"%{request.Keyword}%")
                );

            // Get total count
            int totalItems = await query.CountAsync(cancellationToken);

            // Get paginated users
            var users = await query
                .OrderByDescending(u => u.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = await Task.WhenAll(
                users.Select(async u =>
                    await Response.FromEntity(
                        u,
                        encryptionSettings.Key,
                        aesEncryptionService,
                        keyManagementService
                    )
                )
            );

            // Check if there are more items
            bool hasNext = query.Skip(request.PageSize * request.PageNumber).Any();

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    responses,
                    totalItems,
                    request.PageNumber,
                    request.PageSize,
                    hasNext
                ),
                "Lấy danh sách người lái xe thành công"
            );
        }
    }
}
