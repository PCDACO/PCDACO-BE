using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_GPSDevice.Commands;

public class UpdateGPSDevice
{
    public record Command(Guid Id, string Name, DeviceStatusEnum Status)
        : IRequest<Result<Response>>;

    public record Response(Guid Id);

    public record Handler(IAppDBContext Context, CurrentUser CurrentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // check permission
            if (!CurrentUser.User!.IsAdmin())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            // get the device
            GPSDevice? updatingGPSDevice = await Context
                .GPSDevices.Where(gps => !gps.IsDeleted)
                .Where(gps => gps.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (updatingGPSDevice is null)
                return Result.Error(ResponseMessages.GPSDeviceNotFound);
            // check only device not in use can be updated
            if (updatingGPSDevice.Status == DeviceStatusEnum.InUsed)
                return Result.Error("Thiết bị đang được sử dụng không thể cập nhật");
            // update the device
            updatingGPSDevice.Update(request.Name, request.Status);
            // save to DB
            await Context.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(updatingGPSDevice.Id), ResponseMessages.Updated);
        }
    }
}
