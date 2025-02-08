using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Net.payOS.Types;
using UseCases.Abstractions;

namespace UseCases.UC_Booking.Commands;

public sealed class ProcessPaymentWebhook
{
    public sealed record Command(WebhookType WebhookType) : IRequest<Result>;

    public class Handler(IAppDBContext context) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // Verify webhook data
            WebhookData webhookData = request.WebhookType.data;

            if (webhookData == null)
                return Result.Error("Dữ liệu webhook không hợp lệ");

            // Extract bookingId from signature
            if (!Guid.TryParse(request.WebhookType.signature, out Guid bookingId))
                return Result.Error("BookingId không hợp lệ");

            int result = await context
                .Bookings.AsSplitQuery()
                .Where(o => o.Id == bookingId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsPaid, true), cancellationToken);

            return result == 0 ? Result.NotFound("Không tìm thấy booking") : Result.Success();
        }
    }
}
