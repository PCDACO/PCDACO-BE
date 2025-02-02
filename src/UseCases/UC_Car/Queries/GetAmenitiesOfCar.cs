using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_Car.Queries;

public sealed class GetAmenitiesOfCar
{
    public record Query(
        Guid Id,
        int PageNumber = 1,
        int PageSize = 10,
        string Keyword = ""
        ) : IRequest<Result<OffsetPaginatedResponse<Response>>>;

    public record Response(
        Guid Id,
        string Name,
        string Description,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(Amenity amenity)
                    => new(
                        amenity.Id,
                        amenity.Name,
                        amenity.Description,
                        GetTimestampFromUuid.Execute(amenity.Id)
                    );
    };

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Query, Result<OffsetPaginatedResponse<Response>>>
    {
        public async Task<Result<OffsetPaginatedResponse<Response>>> Handle(Query request, CancellationToken cancellationToken)
        {
            Car? gettingCar = await context.Cars
                .AsNoTracking()
                .Include(u => u.CarAmenities).ThenInclude(ca => ca.Amenity)
                .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
            if (gettingCar is null)
                return Result.NotFound("Không tìm thấy xe");
            if (gettingCar.OwnerId != currentUser.User!.Id)
                return Result.Forbidden("Bạn không có quyền truy cập");
            return Result.Success(OffsetPaginatedResponse<Response>.Map(
                gettingCar.CarAmenities.Select(ca => Response.FromEntity(ca.Amenity)),
                gettingCar.CarAmenities.Count,
                request.PageNumber,
                request.PageSize
            ), "Lấy danh sách tiện ích của xe thành công");
        }
    }
}