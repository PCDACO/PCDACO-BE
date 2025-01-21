using Ardalis.Result;

using Domain.Entities;

using MediatR;

using UseCases.Abstractions;
using UseCases.DTOs;
namespace UseCases.UC_Amenity.Commands;

public sealed class CreateAmenity
{
    public record Command(
        string Name,
        string Description
    ) : IRequest<Result<Response>>;

    public record Response(
        Guid Id
    )
    {
        public static Response FromEntity(Amenity amenity)
        {
            return new Response(amenity.Id);
        }
    };

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
        ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin()) return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");
            Amenity amenity = new()
            {
                Name = request.Name,
                Description = request.Description
            };
            await context.Amenities.AddAsync(amenity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Created(Response.FromEntity(amenity));
        }
    }
}