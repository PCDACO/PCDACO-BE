using Ardalis.Result;

using Domain.Constants;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Shared;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_User.Queries;

public class GetUserPendingForApproval
{
    public record Query(Guid Id)
            : IRequest<Result<Response>>;

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
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền truy cập");

            // Build query
            User? gettingUser = await context
                 .Users
                 .AsNoTracking()
                 .Include(u => u.Role)
                 .Include(u => u.EncryptionKey)
                 .Where(u =>
                     u.Role != null
                     && (u.Role.Name == UserRoleNames.Driver || u.Role.Name == UserRoleNames.Owner)
                     && u.LicenseIsApproved == null // Pending for approve
                     && u.LicenseImageUploadedAt != null // License image uploaded
                     && !u.IsDeleted
                 )
                 .Where(u => u.Id == request.Id)
                 .FirstOrDefaultAsync();
            if (gettingUser is null)
                return Result.Error(ResponseMessages.UserNotFound);
            Response result =
                await Response
                    .FromEntity(gettingUser, encryptionSettings.Key, aesService, keyService);
            return Result.Success(result, "Lấy danh sách người lái xe thành công");
        }
    }
}