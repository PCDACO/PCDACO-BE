using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_License.Queries;

public class GetLicenseById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string UserName,
        string LicenseNumber,
        DateTimeOffset ExpirationDate,
        string ImageFrontUrl,
        string ImageBackUrl,
        bool? IsApproved,
        string? RejectReason,
        DateTimeOffset? ApproveAt,
        DateTimeOffset CreatedAt
    )
    {
        public static async Task<Response> FromEntity(
            License license,
            string masterKey,
            IAesEncryptionService aesEncryptionService,
            IKeyManagementService keyManagementService
        )
        {
            string decryptedKey = keyManagementService.DecryptKey(
                license.EncryptionKey.EncryptedKey,
                masterKey
            );

            string decryptedLicenseNumber = await aesEncryptionService.Decrypt(
                license.EncryptedLicenseNumber,
                decryptedKey,
                license.EncryptionKey.IV
            );

            return new(
                Id: license.Id,
                UserName: license.User.Name,
                LicenseNumber: decryptedLicenseNumber,
                ExpirationDate: DateTimeOffset.Parse(license.ExpiryDate),
                ImageFrontUrl: license.LicenseImageFrontUrl,
                ImageBackUrl: license.LicenseImageBackUrl,
                IsApproved: license.IsApprove,
                RejectReason: license.RejectReason,
                ApproveAt: license.ApprovedAt,
                CreatedAt: GetTimestampFromUuid.Execute(license.Id)
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
            // Only admin or license owner can view
            if (!_currentUser.User!.IsAdmin() && !_currentUser.User.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            var license = await _context
                .Licenses.AsNoTracking()
                .Include(l => l.User)
                .Include(l => l.EncryptionKey)
                .FirstOrDefaultAsync(l => l.Id == request.Id && !l.IsDeleted, cancellationToken);

            if (license == null)
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            // Check if user is owner of license or admin
            if (!_currentUser.User.IsAdmin() && license.UserId != _currentUser.User.Id)
                return Result.Forbidden("Bạn không có quyền xem giấy phép này");

            return Result.Success(
                await Response.FromEntity(
                    license,
                    _encryptionSettings.Key,
                    _aesService,
                    _keyService
                ),
                "Lấy thông tin giấy phép lái xe thành công"
            );
        }
    }
}
