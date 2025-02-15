using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_License.Queries;

public class GetAllLicensesForApprove
{
    public record Query(int PageNumber, int PageSize, string Keyword)
        : IRequest<Result<OffsetPaginatedResponse<Response>>>;

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
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        private readonly IAppDBContext _context = context;
        private readonly CurrentUser _currentUser = currentUser;
        private readonly IAesEncryptionService _aesService = aesService;
        private readonly IKeyManagementService _keyService = keyService;
        private readonly EncryptionSettings _encryptionSettings = encryptionSettings;

        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (!_currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này");

            var query = _context
                .Licenses.AsNoTracking()
                .Include(l => l.User)
                .Include(l => l.EncryptionKey)
                .Where(l => !l.IsDeleted && l.IsApprove == null)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                query = query.Where(l =>
                    EF.Functions.ILike(l.User.Name, $"%{request.Keyword}%")
                    || EF.Functions.ILike(l.User.Email, $"%{request.Keyword}%")
                );
            }

            var count = await query.CountAsync(cancellationToken);
            var licenses = await query
                .OrderByDescending(l => l.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var responses = await Task.WhenAll(
                licenses.Select(async l =>
                    await Response.FromEntity(l, _encryptionSettings.Key, _aesService, _keyService)
                )
            );

            return Result.Success(
                OffsetPaginatedResponse<Response>.Map(
                    responses.AsEnumerable(),
                    count,
                    request.PageNumber,
                    request.PageSize
                )
            );
        }
    }
}
