using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Driver.Queries;

public static class GetLicenseByCurrentDriver
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string LicenseNumber,
        DateTimeOffset ExpirationDate,
        string? ImageFrontUrl,
        string? ImageBackUrl,
        bool? IsApproved,
        string? RejectReason,
        DateTimeOffset? ApprovedAt,
        DateTimeOffset CreatedAt
    )
    {
        public static async Task<Response> FromEntityAsync(
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
                LicenseNumber: decryptedLicenseNumber,
                ExpirationDate: DateTimeOffset.Parse(license.ExpiryDate),
                ImageFrontUrl: license.LicenseImageFrontUrl,
                ImageBackUrl: license.LicenseImageBackUrl,
                IsApproved: license.IsApprove,
                RejectReason: license.RejectReason,
                ApprovedAt: license.ApprovedAt,
                CreatedAt: GetTimestampFromUuid.Execute(license.Id)
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
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");
            var license = await context
                .Licenses.AsNoTracking()
                .Include(l => l.EncryptionKey)
                .FirstOrDefaultAsync(
                    l => l.UserId == currentUser.User!.Id && !l.IsDeleted,
                    cancellationToken
                );

            if (license is null)
                return Result.NotFound("Không tìm thấy giấy phép lái xe");

            return Result.Success(
                await Response.FromEntityAsync(
                    license: license,
                    masterKey: encryptionSettings.Key,
                    aesEncryptionService: aesEncryptionService,
                    keyManagementService: keyManagementService
                ),
                "Lấy thông tin giấy phép lái xe thành công"
            );
        }
    }
}
