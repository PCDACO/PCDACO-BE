using Ardalis.Result;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_User.Queries;

public class GetAllUsersPendingForApprove
{
    public record Query(int PageNumber, int PageSize, string Keyword)
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        string Name,
        string Email,
        string AvatarUrl,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string Role,
        DateTimeOffset CreatedAt,
        string LicenseNumber,
        DateTimeOffset? LicenseExpiryDate,
        string? LicenseImageFrontUrl,
        string? LicenseImageBackUrl,
        bool? IsApprovedLicense,
        string? LicenseRejectReason,
        DateTimeOffset? LicenseApprovedAt,
        DateTimeOffset? LicenseImageUploadedAt
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
            // Decrypt phone
            string decryptedPhone = await aesEncryptionService.Decrypt(
                user.Phone,
                decryptedKey,
                user.EncryptionKey.IV
            );
            // Decrypt license number
            string decryptedLicenseNumber = await aesEncryptionService.Decrypt(
                user.EncryptedLicenseNumber,
                decryptedKey,
                user.EncryptionKey.IV
            );

            return new(
                Id: user.Id,
                Name: user.Name,
                Email: user.Email,
                AvatarUrl: user.AvatarUrl,
                Address: user.Address,
                DateOfBirth: user.DateOfBirth,
                Phone: decryptedPhone,
                Role: user.Role.Name,
                CreatedAt: GetTimestampFromUuid.Execute(user.Id),
                LicenseNumber: decryptedLicenseNumber,
                LicenseExpiryDate: user.LicenseExpiryDate,
                LicenseImageFrontUrl: user.LicenseImageFrontUrl,
                LicenseImageBackUrl: user.LicenseImageBackUrl,
                IsApprovedLicense: user.LicenseIsApproved,
                LicenseRejectReason: user.LicenseRejectReason,
                LicenseApprovedAt: user.LicenseApprovedAt,
                LicenseImageUploadedAt: user.LicenseImageUploadedAt
            );
        }
    }

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesService,
        IKeyManagementService keyService,
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
                    u.Role != null
                    && (u.Role.Name == UserRoleNames.Driver || u.Role.Name == UserRoleNames.Owner)
                    && u.LicenseIsApproved == null // Pending for approve
                    && u.LicenseImageUploadedAt != null // License image uploaded
                    && !u.IsDeleted
                )
                .Where(u =>
                    EF.Functions.ILike(u.Name, $"%{request.Keyword}%")
                    || EF.Functions.ILike(u.Email, $"%{request.Keyword}%")
                );

            // Get total count
            int totalItems = await query.CountAsync(cancellationToken);

            // Get paginated users
            var users = await query
                .OrderByDescending(u => u.LicenseImageUploadedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = await Task.WhenAll(
                users.Select(async u =>
                    await Response.FromEntity(u, encryptionSettings.Key, aesService, keyService)
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
