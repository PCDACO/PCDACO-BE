
using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_GPSDevice.Commands;

public class UpdateGPSDevice
{
    public record Command(
        Guid Id,
        string Name) : IRequest<Result<Response>>;

    public record Response(
        Guid Id
    );

    public record Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // check permission
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            // get the device
            GPSDevice? updatingGPSDevice = await context.GPSDevices
                .Where(gps => !gps.IsDeleted)
                .Where(gps => gps.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (updatingGPSDevice is null)
                return Result.Error(ResponseMessages.GPSDeviceNotFound);
            // update the device
            updatingGPSDevice.Update(request.Name);
            // save to DB
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(updatingGPSDevice.Id), ResponseMessages.Updated);
        }
    }
}