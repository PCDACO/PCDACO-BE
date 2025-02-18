using Ardalis.Result;

using Domain.Constants;
using Domain.Data;
using Domain.Entities;
using Domain.Shared;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_GPSDevice.Commands;

public class CreateGPSDevice : BaseEntity
{
    public record Command(
        string Name
    ) : IRequest<Result<Response>>;
    public record Response(
        Guid Id
    );

    public class Handler(
        IAppDBContext context,
        DeviceStatusesData deviceStatusesData,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // check permission
            if (!currentUser.User!.IsAdmin()) return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            // check if status is valid
            Guid? activeStatusId = deviceStatusesData.Statuses
               .Where(ds => ds.Name == DeviceStatusNames.Available)
               .Select(ds => ds.Id)
               .FirstOrDefault();
            if (activeStatusId is null) return Result.Error(ResponseMessages.DeviceStatusNotFound);
            Console.Write(activeStatusId);
            // check if the name is existed
            if (await context.GPSDevices.Where(d => EF.Functions.ILike(d.Name, $"%{request.Name}%")).AnyAsync(cancellationToken))
                return Result.Error(ResponseMessages.GPSDeviceIsExisted);
            // init new object
            GPSDevice addingDevice = new()
            {
                Name = request.Name,
                StatusId = activeStatusId.Value
            };
            // save to db
            await context.GPSDevices.AddAsync(addingDevice, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(addingDevice.Id), ResponseMessages.Created);
        }
    }
}