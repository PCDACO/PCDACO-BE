using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Contract.Queries;

public class GetAllCarContracts
{
    public record Query(
        int PageNumber,
        int PageSize,
        string Keyword,
        CarContractStatusEnum? Status = null
    ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        string Terms,
        Guid CarId,
        string CarModel,
        string LicensePlate,
        Guid OwnerId,
        string OwnerName,
        Guid? TechnicianId,
        string? TechnicianName,
        string Status,
        DateTimeOffset? OwnerSignatureDate,
        DateTimeOffset? TechnicianSignatureDate,
        string? InspectionResults,
        Guid? GpsDeviceId,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(CarContract contract) =>
            new(
                Id: contract.Id,
                Terms: contract.Terms,
                CarId: contract.CarId,
                CarModel: contract.Car.Model.Name,
                LicensePlate: contract.Car.LicensePlate,
                OwnerId: contract.Car.OwnerId,
                OwnerName: contract.Car.Owner.Name,
                TechnicianId: contract.TechnicianId,
                TechnicianName: contract.Technician?.Name,
                Status: contract.Status.ToString(),
                OwnerSignatureDate: contract.OwnerSignatureDate,
                TechnicianSignatureDate: contract.TechnicianSignatureDate,
                InspectionResults: contract.InspectionResults,
                GpsDeviceId: contract.GPSDeviceId,
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
            // Check if user is admin
            if (!currentUser.User!.IsAdmin())
            {
                return Result.Forbidden("Không có quyền truy cập danh sách hợp đồng");
            }

            // Start building the query
            IQueryable<CarContract> query = context
                .CarContracts.AsNoTracking()
                .Include(c => c.Car)
                .ThenInclude(car => car.Model)
                .Include(c => c.Car)
                .ThenInclude(car => car.Owner)
                .ThenInclude(o => o.EncryptionKey)
                .Include(c => c.Technician)
                .AsSplitQuery();

            // Apply filtering
            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                string keyword = request.Keyword.Trim().ToLower();
                query = query.Where(c =>
                    EF.Functions.ILike(c.Car.LicensePlate, $"%{keyword}%")
                    || EF.Functions.ILike(c.Car.Owner.Name, $"%{keyword}%")
                    || (
                        c.Technician != null
                        && EF.Functions.ILike(c.Technician.Name, $"%{keyword}%")
                    )
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

            return Result.Success(paginatedResponse, ResponseMessages.Fetched);
        }
    }
}
