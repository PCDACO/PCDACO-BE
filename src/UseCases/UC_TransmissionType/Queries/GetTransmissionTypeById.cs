
using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_TransmissionType.Queries;

public class GetTransmissionTypeById
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(Guid Id, string Name, DateTimeOffset CreatedAt)
    {
        public static Response FromEntity(TransmissionType entity) =>
            new(entity.Id, entity.Name, GetTimestampFromUuid.Execute(entity.Id));
    };

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Response? result = await context.TransmissionTypes
                .Where(t => t.Id == request.Id)
                .Where(t => !t.IsDeleted)
                .Select(t => Response.FromEntity(t))
                .FirstOrDefaultAsync(cancellationToken)!;
            if (result is null)
                return Result.NotFound("Không tìm thấy trạng thái");
            return Result.Success(result, "Lấy trạng thái thành công");
        }
    }
}