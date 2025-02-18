using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_GPSDevice.Commands;

public class DeleteGPSDevice
{
    public record Command(Guid Id) : IRequest<Result>;

    public record Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // check permission
            if (!currentUser.User!.IsAdmin()) return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            // get gps device
            GPSDevice? deletingGPSDevice = await context.GPSDevices
                .Include(gps => gps.GPS)
                .Where(gps => !gps.IsDeleted)
                .Where(gps => gps.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            // if not have device then return error
            if (deletingGPSDevice is null) return Result.Error(ResponseMessages.GPSDeviceNotFound);
            // if already have device then return error
            if (deletingGPSDevice.GPS is not null) return Result.Error(ResponseMessages.GPSDeviceHasCarGPS);
            // delete device
            deletingGPSDevice.Delete();
            // save to db
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage(ResponseMessages.Deleted);
        }
    }
}