using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ardalis.Result;

using Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Utils;

namespace UseCases.UC_CarStatus.Queries;

public class GetCarStatusById
{
    public record Query(
        Guid Id
    ) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt
    )
    {
        public static Response FromEntity(CarStatus entity)
            => new(entity.Id, entity.Name, GetTimestampFromUuid.Execute(entity.Id));
    };

    public class Handler(
        IAppDBContext context
    ) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Response? result = await context.CarStatuses
                .Where(bs => bs.Id == request.Id)
                .Where(bs => !bs.IsDeleted)
                .Select(bs => Response.FromEntity(bs))
                .FirstOrDefaultAsync(cancellationToken);
            if (result is null)
                return Result.NotFound("Không tìm thấy trạng thái xe");
            return Result.Success(result, "Lấy trạng thái xe thành công");
        }
    }
}