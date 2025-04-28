using Ardalis.Result;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.Services.PaymentTokenService;
using UseCases.Services.PayOSService;

namespace UseCases.UC_Booking.Commands;

public sealed class ProcessBookingPaymentByToken
{
    public sealed record Command(string Token) : IRequest<Result<ProcessBookingPayment.Response>>;

    internal sealed class Handler(
        IAppDBContext context,
        IPaymentService paymentService,
        IPaymentTokenService paymentTokenService
    ) : IRequestHandler<Command, Result<ProcessBookingPayment.Response>>
    {
        public async Task<Result<ProcessBookingPayment.Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // Validate and decode the token
            var bookingId = await paymentTokenService.ValidateTokenAsync(request.Token);
            if (bookingId == null)
                return Result.Error("Token không hợp lệ");

            var booking = await context
                .Bookings.Include(x => x.Car)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (
                booking.Status != BookingStatusEnum.Approved
                && booking.Status != BookingStatusEnum.ReadyForPickup
                && booking.Status != BookingStatusEnum.Ongoing
            )
                return Result.Error("Chỉ có thể thanh toán chuyến đi khi được phê duyệt!");

            // Check payment status based on payment type
            bool isExtensionPayment = booking.ExtensionAmount.HasValue;

            if (isExtensionPayment)
            {
                if (booking.IsExtensionPaid == true)
                    return Result.Error("Phí gia hạn này đã được thanh toán!");
            }
            else
            {
                if (booking.IsPaid)
                    return Result.Error("Chuyến đi này đã được thanh toán!");
            }

            var paymentAmount = booking.ExtensionAmount ?? booking.TotalAmount;
            var description = isExtensionPayment ? "Thanh toan gia han" : "Thanh toan don hang";

            // Create payment link
            var paymentResult = await paymentService.CreatePaymentLinkAsync(
                booking.Id,
                paymentAmount,
                description,
                booking.User.Name
            );

            booking.PayOSOrderCode = paymentResult.OrderCode;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new ProcessBookingPayment.Response(
                    OrderCode: paymentResult.OrderCode,
                    TotalDistance: booking.TotalDistance / 1000,
                    ExcessDays: booking.ExcessDay,
                    ExcessFee: booking.ExcessDayFee,
                    BasePrice: booking.BasePrice,
                    PlatformFee: booking.PlatformFee,
                    TotalAmount: paymentAmount,
                    PaymentUrl: paymentResult.CheckoutUrl,
                    QrCode: paymentResult.QrCode
                )
            );
        }
    }
}
