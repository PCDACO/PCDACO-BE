using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_InspectionSchedule.Queries;

public class GetSignedOrInprogressInspectionScheduleForCurrentUser
{
    public record Query() : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        DateTimeOffset Date,
        string OwnerName,
        string Address,
        string LicensePlate,
        string Status,
        InspectionScheduleType Type,
        ContractDetail? ContractDetail
    )
    {
        public static Response FromEntity(InspectionSchedule inspectionSchedule)
        {
            return new(
                Id: inspectionSchedule.Id,
                Date: inspectionSchedule.InspectionDate,
                OwnerName: inspectionSchedule.Car.Owner.Name,
                Address: inspectionSchedule.InspectionAddress,
                LicensePlate: inspectionSchedule.Car.LicensePlate,
                Status: inspectionSchedule.Status.ToString(),
                Type: inspectionSchedule.Type,
                ContractDetail: inspectionSchedule.Car.Contract != null
                    ? new ContractDetail(
                        Id: inspectionSchedule.Car.Contract!.Id,
                        Terms: inspectionSchedule.Car.Contract.Terms,
                        Status: inspectionSchedule.Car.Contract.Status.ToString(),
                        OwnerSignatureDate: inspectionSchedule.Car.Contract.OwnerSignatureDate,
                        TechnicianSignatureDate: inspectionSchedule
                            .Car
                            .Contract
                            .TechnicianSignatureDate,
                        InspectionResults: inspectionSchedule.Car.Contract.InspectionResults,
                        GPSDeviceId: inspectionSchedule.Car.Contract.GPSDeviceId
                    )
                    : null
            );
        }
    };

    public sealed record ContractDetail(
        Guid Id,
        string Terms,
        string Status,
        DateTimeOffset? OwnerSignatureDate,
        DateTimeOffset? TechnicianSignatureDate,
        string? InspectionResults,
        Guid? GPSDeviceId
    );

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            if (currentUser.User is null)
            {
                return Result.Unauthorized(ResponseMessages.UnauthourizeAccess);
            }

            // Check if car is not attached to any gps then return error
            InspectionSchedule? result = await context
                .InspectionSchedules.AsNoTracking()
                .AsSplitQuery()
                .Include(i => i.Car)
                .ThenInclude(c => c.Owner)
                .Include(i => i.Car)
                .ThenInclude(c => c.GPS)
                .Include(i => i.Car)
                .ThenInclude(c => c.CarAmenities)
                .ThenInclude(ca => ca.Amenity)
                .Include(i => i.Car)
                .ThenInclude(c => c.FuelType)
                .Include(i => i.Car)
                .ThenInclude(c => c.TransmissionType)
                .Include(i => i.Car)
                .ThenInclude(c => c.Model)
                .Include(i => i.Car)
                .ThenInclude(i => i.Contract)
                .Include(i => i.Technician)
                .Where(i => !i.IsDeleted)
                .Where(i =>
                    i.Status == InspectionScheduleStatusEnum.Signed
                    || i.Status == InspectionScheduleStatusEnum.InProgress
                )
                .Where(i => i.TechnicianId == currentUser.User!.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null)
            {
                return Result.NotFound(ResponseMessages.InspectionScheduleNotFound);
            }

            return Result.Success(Response.FromEntity(result), ResponseMessages.Fetched);
        }
    }
}
