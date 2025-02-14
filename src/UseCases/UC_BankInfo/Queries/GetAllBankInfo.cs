using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;

namespace UseCases.UC_BankInfo.Queries;

public sealed class GetAllBankInfo
{
    public record Query(string? SearchTerm) : IRequest<Result<IQueryable<Response>>>;

    public record Response(
        Guid Id,
        Guid BankLookUpId,
        string Name,
        string Code,
        int Bin,
        string ShortName,
        string LogoUrl,
        string IconUrl,
        string SwiftCode,
        int LookupSupported
    )
    {
        public static Response FromEntity(BankInfo bankInfo) =>
            new(
                bankInfo.Id,
                bankInfo.BankLookUpId,
                bankInfo.Name,
                bankInfo.Code,
                bankInfo.Bin,
                bankInfo.ShortName,
                bankInfo.LogoUrl,
                bankInfo.IconUrl,
                bankInfo.SwiftCode,
                bankInfo.LookupSupported
            );
    };

    public class Handler(IAppDBContext context)
        : IRequestHandler<Query, Result<IQueryable<Response>>>
    {
        public async Task<Result<IQueryable<Response>>> Handle(
            Query request,
            CancellationToken cancellationToken
        )
        {
            // Query bank info
            IQueryable<BankInfo> query = context
                .BankInfos.AsNoTracking()
                .OrderByDescending(a => a.Id)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(a =>
                    EF.Functions.ILike(a.Name, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(a.Code, $"%{request.SearchTerm}%")
                    || EF.Functions.ILike(a.ShortName, $"%{request.SearchTerm}%")
                );
            }

            var result = await query.ToListAsync(cancellationToken);

            return Result.Success(
                result.Select(Response.FromEntity).AsQueryable(),
                "Danh sách ngân hàng"
            );
        }
    }
}
