using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using UseCases.Abstractions;

namespace UseCases.UC_Car.Queries;

public class GetCurrentLocationByCarId
{
    public sealed record Query(Guid CarId) : IRequest<Result<Response>>;

    public sealed record Response(decimal Longitude, decimal Latitude, DateTimeOffset? UpdatedAt)
    {
        public static Response FromEntity(CarLocation location) =>
            new(location.Longitude, location.Latitude, location.UpdatedAt ?? DateTimeOffset.UtcNow);
    }

    public sealed record CarLocation(
        decimal Longitude,
        decimal Latitude,
        DateTimeOffset? UpdatedAt
    );

    internal sealed class Handler(IAppDBContext context) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            var car = await context
                .Cars.Include(c => c.GPS)
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car is null)
                return Result.NotFound("Không tìm thấy xe");

            if (car.GPS is null)
                return Result.Error("Không tìm thấy GPS của xe");

            if (car.GPS.Location is not Point point)
                return Result.Error("Không tìm thấy vị trí GPS của xe");

            var longitude = (decimal)point.X;
            var latitude = (decimal)point.Y;

            return Result.Success(
                Response.FromEntity(new CarLocation(longitude, latitude, car.GPS.UpdatedAt)),
                "Lấy vị trí xe thành công"
            );
        }
    }
}
