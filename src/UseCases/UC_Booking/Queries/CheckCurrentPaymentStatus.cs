using Ardalis.Result;
using Domain.Constants;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;

namespace UseCases.UC_Booking.Queries;

public class CheckCurrentPaymentStatus
{
    public record Query(long OrderCode) : IRequest<Result>;

    internal sealed class Handler(
            IAppDBContext context
            ) : IRequestHandler<Query, Result>
    {
        public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
        {
            Guid? gettingBooking = await context
                .Bookings.AsNoTracking()
                .Where(b => !b.IsDeleted)
                .Where(b => b.PayOSOrderCode == request.OrderCode)
                .Select(b => b.Id)
                .FirstOrDefaultAsync();

            if (gettingBooking == null || gettingBooking == Guid.Empty)
            {
                return Result.NotFound(ResponseMessages.BookingNotFound);
            }

            bool isTransactionExist = await context
                .Transactions.AsNoTracking()
                .Where(t => t.BookingId == gettingBooking)
                .Where(t => t.Status == Domain.Enums.TransactionStatusEnum.Completed)
                .AnyAsync();

            if (!isTransactionExist)
            {
                return Result.NotFound(ResponseMessages.TransactionNotFound);
            }

            return Result.SuccessWithMessage(ResponseMessages.Fetched);
        }
    }
}