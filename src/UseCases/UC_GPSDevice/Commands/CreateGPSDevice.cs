using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_GPSDevice.Commands;

public class CreateGPSDevice : BaseEntity
{
    public record Command(string OSBuildId, string Name) : IRequest<Result<Response>>;

    public record Response(Guid Id);

    public class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // check permission
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            // check if status is valid
            // check if the OSBuildId is existed
            if (
                await context
                    .GPSDevices.Where(d =>
                        EF.Functions.ILike(d.OSBuildId, $"%{request.OSBuildId}%")
                    )
                    .AnyAsync(cancellationToken)
            )
                return Result.Error(ResponseMessages.GPSDeviceIsExisted);
            // init new object
            GPSDevice addingDevice = new()
            {
                OSBuildId = request.OSBuildId,
                Name = request.Name,
                Status = Domain.Enums.DeviceStatusEnum.Available,
            };
            // save to db
            await context.GPSDevices.AddAsync(addingDevice, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(addingDevice.Id), ResponseMessages.Created);
        }
    }
}
