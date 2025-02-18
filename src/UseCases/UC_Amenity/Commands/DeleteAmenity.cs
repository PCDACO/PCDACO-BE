using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Amenity.Commands;

public sealed class DeleteAmenity
{
    public record Command(
        Guid Id
    ) : IRequest<Result>;

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin()) return Result.Forbidden(ResponseMessages.ForbiddenAudit);
            Amenity? deletingAmenity = await context.Amenities.FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);
            if (deletingAmenity is null)
                return Result.Error(ResponseMessages.AmenitiesNotFound);
            deletingAmenity.Delete();
            await context.SaveChangesAsync(cancellationToken);
            return Result.SuccessWithMessage(ResponseMessages.Deleted);
        }
    }
}
