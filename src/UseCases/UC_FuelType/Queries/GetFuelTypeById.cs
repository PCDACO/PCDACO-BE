
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.Utils;

namespace UseCases.UC_FuelType.Queries;

public class GetFuelTypeById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(Guid Id, string Name, DateTimeOffset CreatedAt)
    {
        public static Response FromEntity(FuelType entity) =>
            new(entity.Id, entity.Name, GetTimestampFromUuid.Execute(entity.Id));
    };

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Response? result = await context.FuelTypes
                .Where(ft => ft.Id == request.Id)
                .Where(ft => !ft.IsDeleted)
                .Select(ft => Response.FromEntity(ft))
                .FirstOrDefaultAsync(cancellationToken)!;
            if (result is null)
                return Result.NotFound("Không tìm thấy loại nhiên liệu");
            return Result.Success(result, "Lấy loại nhân liệu thành công");
        }
    }
}