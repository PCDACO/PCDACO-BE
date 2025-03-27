using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.UC_Car.Queries;

public sealed class GetListUnavailableDatesOfCar
{
    public record Query(Guid CarId) : IRequest<Result<List<Response>>>;

    public record Response(DateTimeOffset Date, bool IsAvailable)
    {
        public static Response FromEntity(CarAvailability availability) =>
            new(availability.Date, availability.IsAvailable);
    }

    internal sealed class Handler(IAppDBContext context)
        : IRequestHandler<Query, Result<List<Response>>>
    {
        public async Task<Result<List<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var car = await context.Cars.FirstOrDefaultAsync(
                c => c.Id == request.CarId && !c.IsDeleted,
                cancellationToken
            );

            if (car is null)
                return Result.NotFound(ResponseMessages.CarNotFound);

            var unavailableDates = await context
                .CarAvailabilities.AsNoTracking()
                .Where(ca => ca.CarId == request.CarId && !ca.IsAvailable && !ca.IsDeleted)
                .ToListAsync(cancellationToken);

            return Result.Success(
                unavailableDates.Select(Response.FromEntity).ToList(),
                ResponseMessages.Fetched
            );
        }
    }
}
