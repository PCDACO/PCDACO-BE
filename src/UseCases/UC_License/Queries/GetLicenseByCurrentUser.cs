using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_License.Queries;

public static class GetLicenseByCurrentUser
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(
        Guid UserId,
        string LicenseNumber,
        DateTimeOffset? ExpirationDate,
        string? ImageFrontUrl,
        string? ImageBackUrl,
        bool? IsApproved,
        string? RejectReason,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset? LicenseImageUploadedAt
    )
    {
        public static async Task<Response> FromEntityAsync(
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
            string decryptedLicenseNumber = await aesEncryptionService.Decrypt(
                user.EncryptedLicenseNumber,
                decryptedKey,
                user.EncryptionKey.IV
            );
            return new(
                UserId: user.Id,
                LicenseNumber: decryptedLicenseNumber,
                ExpirationDate: user.LicenseExpiryDate,
                ImageFrontUrl: user.LicenseImageFrontUrl,
                ImageBackUrl: user.LicenseImageBackUrl,
                IsApproved: user.LicenseIsApproved,
                RejectReason: user.LicenseRejectReason,
                ApprovedAt: user.LicenseApprovedAt,
                LicenseImageUploadedAt: user.LicenseImageUploadedAt
            );
        }
    };

    internal class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var user = await context
                .Users.AsNoTracking()
                .Include(u => u.EncryptionKey)
                .FirstOrDefaultAsync(
                    u => u.Id == currentUser.User!.Id && !u.IsDeleted,
                    cancellationToken
                );

            if (user is null)
                return Result.Error("Người dùng không tồn tại");

            if (string.IsNullOrEmpty(user.EncryptedLicenseNumber))
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            return Result.Success(
                await Response.FromEntityAsync(
                    user: user,
                    masterKey: encryptionSettings.Key,
                    aesEncryptionService: aesEncryptionService,
                    keyManagementService: keyManagementService
                ),
                "Lấy thông tin giấy phép lái xe thành công"
            );
        }
    }
}
