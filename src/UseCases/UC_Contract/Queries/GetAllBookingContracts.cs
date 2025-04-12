using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Contract.Queries;

public sealed class GetAllBookingContracts
{
    public record Query(
        int PageNumber,
        int PageSize,
        string Keyword = "",
        ContractStatusEnum? Status = null
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        string Terms,
        Guid BookingId,
        Guid CarId,
        string CarModel,
        string LicensePlate,
        Guid OwnerId,
        string OwnerName,
        Guid DriverId,
        string DriverName,
        string Status,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        DateTimeOffset? DriverSignatureDate,
        DateTimeOffset? OwnerSignatureDate,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(Contract contract) =>
            new(
                Id: contract.Id,
                Terms: contract.Terms,
                BookingId: contract.BookingId,
                CarId: contract.Booking.CarId,
                CarModel: contract.Booking.Car.Model.Name,
                LicensePlate: contract.Booking.Car.LicensePlate,
                OwnerId: contract.Booking.Car.OwnerId,
                OwnerName: contract.Booking.Car.Owner.Name,
                DriverId: contract.Booking.UserId,
                DriverName: contract.Booking.User.Name,
                Status: contract.Status.ToString(),
                StartDate: contract.StartDate,
                EndDate: contract.EndDate,
                DriverSignatureDate: contract.DriverSignatureDate,
                OwnerSignatureDate: contract.OwnerSignatureDate,
                CreatedAt: GetTimestampFromUuid.Execute(contract.Id)
            );
    }

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Create query with all required includes
            var query = context
                .Contracts.AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Model)
                .Include(c => c.Booking)
                .ThenInclude(b => b.Car)
                .ThenInclude(c => c.Owner)
                .Include(c => c.Booking)
                .ThenInclude(b => b.User)
                .AsQueryable();

            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
            {
                return Result.Forbidden("Bạn không có quyền xem các hợp đồng này");
            }

            // Apply search filtering if provided
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                query = query.Where(c =>
                    EF.Functions.ILike(c.Booking.Car.LicensePlate, $"%{request.Keyword}%")
                    || EF.Functions.ILike(c.Booking.Car.Owner.Name, $"%{request.Keyword}%")
                    || EF.Functions.ILike(c.Booking.User.Name, $"%{request.Keyword}%")
                );
            }

            // Filter by status if provided
            if (request.Status.HasValue)
            {
                query = query.Where(c => c.Status == request.Status);
            }

            // Get total count for pagination
            int totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var pagedItems = await query
                .OrderByDescending(c => c.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Transform to response objects
            var items = pagedItems.Select(Response.FromEntity).ToList();

            // Calculate hasNext for pagination
            bool hasNext = await query
                .Skip(request.PageNumber * request.PageSize)
                .AnyAsync(cancellationToken);

            // Create paginated response
            var paginatedResponse = new OffsetPaginatedResponse<Response>(
                items,
                totalCount,
                request.PageNumber,
                request.PageSize,
                hasNext
            );

            return Result.Success(paginatedResponse, "Contracts fetched successfully");
        }
    }
}
