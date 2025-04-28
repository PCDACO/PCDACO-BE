using Ardalis.Result;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UseCases.Services.PayOSService;

namespace UseCases.UC_Booking.Commands;

public sealed class ProcessBookingPayment
{
    public sealed record Command(Guid BookingId) : IRequest<Result<Response>>;

    public sealed record Response(
        long OrderCode,
        decimal TotalDistance,
        decimal ExcessDays,
        decimal ExcessFee,
        decimal BasePrice,
        decimal PlatformFee,
        decimal TotalAmount,
        string PaymentUrl,
        string QrCode
    );

    internal sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IPaymentService paymentService
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsDriver())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này!");

            var booking = await context
                .Bookings.Include(x => x.Car)
                .FirstOrDefaultAsync(x => x.Id == request.BookingId, cancellationToken);

            if (booking == null)
                return Result.NotFound("Không tìm thấy booking");

            if (booking.UserId != currentUser.User.Id)
                return Result.Forbidden(
                    "Bạn không có quyền thực hiện chức năng này với booking này!"
                );

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

            if (
                booking.Status == BookingStatusEnum.Pending
                || booking.Status == BookingStatusEnum.Rejected
                || booking.Status == BookingStatusEnum.Cancelled
                || booking.Status == BookingStatusEnum.Expired
            )
                return Result.Error(
                    "Chỉ có thể thanh toán chuyến đi khi đã được chủ xe chấp thuận!"
                );

            var paymentAmount = booking.ExtensionAmount ?? booking.TotalAmount;
            var description = booking.ExtensionAmount.HasValue
                ? "Thanh toan gia han"
                : "Thanh toan don hang";

            // Create payment link
            var paymentResult = await paymentService.CreatePaymentLinkAsync(
                booking.Id,
                paymentAmount,
                description,
                currentUser.User.Name
            );

            booking.PayOSOrderCode = paymentResult.OrderCode;

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(
                new Response(
                    OrderCode: paymentResult.OrderCode,
                    TotalDistance: booking.TotalDistance / 1000, // Convert to kilometers
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
