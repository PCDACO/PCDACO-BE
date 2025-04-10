using Ardalis.Result;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Report.Commands;

public class ProvideCompensationPaidImage
{
    public sealed record Command(Guid ReportId, Stream Images) : IRequest<Result<Response>>;

    public sealed record Response(Guid ReportId, string ImageUrl)
    {
        public static Response FromEntity(BookingReport entity) =>
            new(entity.Id, entity.CompensationPaidImageUrl!);
    }

    internal sealed class Handler(
        IAppDBContext context,
        ICloudinaryServices cloudinaryServices,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsDriver() && !currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");

            var report = await context.BookingReports.FirstOrDefaultAsync(
                r => r.Id == request.ReportId,
                cancellationToken
            );

            if (report == null)
                return Result.NotFound("Không tìm thấy báo cáo");

            string proofUrl = await cloudinaryServices.UploadCompensationPaidImageAsync(
                $"Report-{report.Id}-CompensationPaidImage",
                request.Images,
                cancellationToken
            );

            report.CompensationPaidImageUrl = proofUrl;
            report.CompensationPaidAt = DateTimeOffset.UtcNow;
            report.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Result<Response>.Success(
                Response.FromEntity(report),
                "Ảnh đã được cung cấp thành công"
            );
        }
    }
}
