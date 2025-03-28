using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.UC_Car.Queries;

public sealed class GetListUnavailableDatesOfCar
{
    public record Query(Guid CarId, int? Month = null, int? Year = null)
        : IRequest<Result<List<Response>>>;

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

            // query
            IQueryable<CarAvailability> query = context
                .CarAvailabilities.AsNoTracking()
                .Where(ca => ca.CarId == request.CarId && !ca.IsAvailable && !ca.IsDeleted);

            // Apply month/year filters if provided
            if (request.Month.HasValue || request.Year.HasValue)
            {
                // Filter by month if provided
                if (request.Month.HasValue)
                {
                    int month = request.Month.Value;
                    // Validate month value
                    if (month < 1 || month > 12)
                        return Result.Error("Tháng không hợp lệ");

                    query = query.Where(ca => ca.Date.Month == month);
                }

                // Filter by year if provided
                if (request.Year.HasValue)
                {
                    int year = request.Year.Value;
                    query = query.Where(ca => ca.Date.Year == year);
                }
                // If only month is specified, use current year
                else if (request.Month.HasValue)
                {
                    int currentYear = DateTimeOffset.UtcNow.Year;
                    query = query.Where(ca => ca.Date.Year == currentYear);
                }
            }

            var unavailableDates = await query.ToListAsync(cancellationToken);

            return Result.Success(
                unavailableDates.Select(Response.FromEntity).ToList(),
                ResponseMessages.Fetched
            );
        }
    }
}
