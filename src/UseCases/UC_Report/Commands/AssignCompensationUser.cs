using Ardalis.Result;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailReports;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.EmailService;

namespace UseCases.UC_Report.Commands;

public class AssignCompensationUser
{
    public sealed record Command(
        Guid ReportId,
        Guid UserId,
        string CompensationReason,
        decimal CompensationAmount
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid ReportId)
    {
        public static Response FromEntity(BookingReport entity) => new(entity.Id);
    }

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsConsultant())
                return Result.Forbidden("Bạn không có quyền thực hiện hành động này");

            var report = await context.BookingReports.FirstOrDefaultAsync(
                r => r.Id == request.ReportId,
                cancellationToken
            );

            if (report == null)
                return Result.NotFound("Không tìm thấy báo cáo");

            report.Status = BookingReportStatus.UnderReview;

            var compensationUser = await context.Users.FirstOrDefaultAsync(
                u => u.Id == request.UserId,
                cancellationToken
            );

            if (compensationUser == null)
                return Result.NotFound("Không tìm thấy người dùng");

            report.CompensationPaidUserId = compensationUser.Id;
            report.CompensationReason = request.CompensationReason;
            report.CompensationAmount = request.CompensationAmount;
            report.IsCompensationPaid = false;
            report.ResolvedById = currentUser.User.Id;

            var dueDate = DateTimeOffset.Now.AddDays(5);

            await context.SaveChangesAsync(cancellationToken);

            // Enqueue a background job to send an email to the compensation user
            backgroundJobClient.Enqueue(
                () =>
                    SendEmail(
                        compensationUser.Name,
                        compensationUser.Email,
                        report.Title,
                        report.CompensationAmount.Value,
                        dueDate
                    )
            );

            // Enqueue a background job to ban the compensation user if they don't pay
            backgroundJobClient.Schedule(
                () => BanUser(compensationUser.Id, report.Id, cancellationToken),
                dueDate
            );

            return Result.Success(
                Response.FromEntity(report),
                "Người dùng đã được gán để thanh toán báo cáo"
            );
        }

        public async Task SendEmail(
            string userName,
            string userEmail,
            string reportTitle,
            decimal amount,
            DateTimeOffset dueDate
        )
        {
            var emailTemplate = ReportPaymentNotificationTemplate.Template(
                userName,
                reportTitle,
                amount,
                dueDate
            );

            await emailService.SendEmailAsync(
                userEmail,
                "Thông Báo Thanh Toán Báo Cáo",
                emailTemplate
            );
        }

        public async Task BanUser(Guid userId, Guid reportId, CancellationToken cancellationToken)
        {
            var user = await context.Users.FirstOrDefaultAsync(
                u => u.Id == userId,
                cancellationToken
            );

            if (user == null)
                return;

            var report = await context.BookingReports.FirstOrDefaultAsync(
                r => r.Id == reportId && (bool)!r.IsCompensationPaid!,
                cancellationToken
            );

            if (report == null)
                return;

            user.IsBanned = true;
            user.BannedReason = "Không thanh toán báo cáo đúng hạn";
            user.UpdatedAt = DateTimeOffset.Now;

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
