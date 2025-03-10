using Ardalis.Result;
using Domain.Constants.EntityNames;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_User.Queries;

public static class GetCurrentUser
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Name,
        string Email,
        string AvatarUrl,
        string Address,
        DateTimeOffset DateOfBirth,
        string Phone,
        string Role,
        int TotalRent, // Total completed Rent driver has
        int TotalRented, // Total rented owner has
        decimal Balance,
        int TotalCar // Total car owner has
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
            string decryptedPhoneNumber = await aesEncryptionService.Decrypt(
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
                decryptedPhoneNumber,
                user.Role.Name,
                // Total completed Rent in case user has role driver else if user has role owner then 0
                TotalRent: user.Role.Name == UserRoleNames.Driver
                    ? user.Bookings.Count(b =>
                        b.UserId == user.Id
                        && !b.IsDeleted
                        && b.Status.Name == BookingStatusEnum.Completed.ToString()
                    )
                    : 0,
                // Total rented owner has else if user has role driver then 0
                TotalRented: user.Role.Name == UserRoleNames.Owner
                    ? user.Cars.Sum(c =>
                        c.Bookings.Count(b =>
                            !b.IsDeleted && b.Status.Name == BookingStatusEnum.Completed.ToString()
                        )
                    )
                    : 0,
                user.Balance,
                // Total car owner has else if user has role driver then 0
                TotalCar: user.Role.Name == UserRoleNames.Owner
                    ? user.Cars.Count(c => !c.IsDeleted && c.OwnerId == user.Id)
                    : 0
            );
        }
    };

    internal sealed class Handler(
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
            if (currentUser.User is null)
                return Result.Unauthorized("Bạn chưa đăng nhập");
            User? user = await context
                .Users.AsNoTracking()
                .AsSplitQuery()
                .Include(u => u.Role)
                .Include(u => u.EncryptionKey)
                .Include(u => u.Bookings) // Booking of user - driver
                .Include(u => u.Cars.Where(c => !c.IsDeleted))
                .ThenInclude(c => c.Bookings) // Booking of car of owner - owner
                .FirstOrDefaultAsync(
                    u => u.Id == currentUser.User!.Id && !u.IsDeleted,
                    cancellationToken
                );

            if (user is null)
                return Result.NotFound("Không tìm thấy thông tin người dùng");

            return Result.Success(
                await Response.FromEntityAsync(
                    user: user!,
                    masterKey: encryptionSettings.Key,
                    aesEncryptionService: aesEncryptionService,
                    keyManagementService: keyManagementService
                ),
                "Lấy thông tin người dùng thành công"
            );
        }
    }
}
