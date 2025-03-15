using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_License.Queries;

public class GetLicenseByUserId
{
    public record Query(Guid UserId) : IRequest<Result<Response>>;

    public record Response(
        Guid UserId,
        string UserName,
        string LicenseNumber,
        DateTimeOffset? ExpirationDate,
        string ImageFrontUrl,
        string ImageBackUrl,
        bool? IsApproved,
        string? RejectReason,
        DateTimeOffset? ApproveAt,
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

            string decryptedLicenseNumber = await aesEncryptionService.Decrypt(
                user.EncryptedLicenseNumber,
                decryptedKey,
                user.EncryptionKey.IV
            );

            return new(
                UserId: user.Id,
                UserName: user.Name,
                LicenseNumber: decryptedLicenseNumber,
                ExpirationDate: user.LicenseExpiryDate,
                ImageFrontUrl: user.LicenseImageFrontUrl,
                ImageBackUrl: user.LicenseImageBackUrl,
                IsApproved: user.LicenseIsApproved,
                RejectReason: user.LicenseRejectReason,
                ApproveAt: user.LicenseApprovedAt,
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
        private readonly IAppDBContext _context = context;
        private readonly CurrentUser _currentUser = currentUser;
        private readonly IAesEncryptionService _aesService = aesService;
        private readonly IKeyManagementService _keyService = keyService;
        private readonly EncryptionSettings _encryptionSettings = encryptionSettings;

        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Only admin can view
            if (!_currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            var user = await _context
                .Users.AsNoTracking()
                .Include(u => u.EncryptionKey)
                .FirstOrDefaultAsync(
                    u => u.Id == request.UserId && !u.IsDeleted,
                    cancellationToken
                );

            if (user == null)
                return Result.NotFound("Người dùng không tồn tại");

            if (string.IsNullOrEmpty(user.EncryptedLicenseNumber))
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            return Result.Success(
                await Response.FromEntity(user, _encryptionSettings.Key, _aesService, _keyService),
                "Lấy thông tin giấy phép lái xe thành công"
            );
        }
    }
}
