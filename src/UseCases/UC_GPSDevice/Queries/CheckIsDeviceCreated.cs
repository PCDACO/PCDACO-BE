using Ardalis.Result;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.UC_GPSDevice.Queries;

public class CheckIsDeviceCreated
{
    public record Query(string OSBuildId) : IRequest<Result<Response>>;

    public record Response(bool IsCreated);

    public class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            bool isCreated = await context
                .GPSDevices.AsNoTracking()
                .Where(d => !d.IsDeleted)
                .AnyAsync(d => d.OSBuildId == request.OSBuildId, cancellationToken);

            return Result.Success(new Response(isCreated), ResponseMessages.Fetched);
        }
    }
}
