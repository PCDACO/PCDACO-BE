using Ardalis.Result;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Shared.EmailTemplates.EmailBookings;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.BackgroundServices.Bookings;
using UseCases.DTOs;
using UseCases.Services.EmailService;
using UUIDNext;

namespace UseCases.UC_Booking.Commands;

public sealed class CreateBooking
{
    public sealed record CreateBookingCommand(
        Guid CarId,
        DateTimeOffset StartTime,
        DateTimeOffset EndTime
    ) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id);

    internal sealed class Handler(
        IAppDBContext context,
        IEmailService emailService,
        IBackgroundJobClient backgroundJobClient,
        BookingReminderJob reminderService,
        CurrentUser currentUser
    ) : IRequestHandler<CreateBookingCommand, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            CreateBookingCommand request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsDriver() && !currentUser.User!.IsOwner())
                return Result.Forbidden("Bạn không có quyền thực hiện chức năng này !");

            if (currentUser.User.IsBanned)
                return Result.Forbidden("Tài khoản của bạn đã bị cấm sử dụng");

            // Check for unpaid compensation
            var hasUnpaidCompensation = await context.BookingReports.AnyAsync(
                r =>
                    r.CompensationPaidUserId == currentUser.User.Id
                    && r.CompensationAmount.HasValue
                    && (!r.IsCompensationPaid.HasValue || !r.IsCompensationPaid.Value),
                cancellationToken
            );

            if (hasUnpaidCompensation)
                return Result.Error(
                    "Bạn có khoản bồi thường chưa thanh toán. Vui lòng thanh toán trước khi đặt xe mới."
                );

            // Verify driver license first
            var license = await context.Users.FirstOrDefaultAsync(
                u =>
                    u.Id == currentUser.User.Id
                    && u.LicenseIsApproved == true
                    && u.LicenseExpiryDate > request.EndTime,
                cancellationToken
            );

            if (license == null)
                return Result.Error(
                    "Bằng lái xe của bạn không hợp lệ hoặc đã hết hạn. Vui lòng cập nhật thông tin bằng lái xe trước khi đặt xe."
                );

            // Check if driver has active bookings
            var activeBookingCount = await context.Bookings.CountAsync(
                b =>
                    b.UserId == currentUser.User!.Id
                    && b.Status != BookingStatusEnum.Completed
                    && b.Status != BookingStatusEnum.Cancelled
                    && b.Status != BookingStatusEnum.Expired
                    && b.Status != BookingStatusEnum.Rejected,
                cancellationToken
            );

            if (activeBookingCount > 0)
                return Result.Error("Bạn chỉ có thể đặt tối đa 1 đơn cùng lúc");

            // Check if car exists
            var car = await context
                .Cars.AsSplitQuery()
                .AsNoTracking()
                .Include(x => x.Owner)
                .Include(x => x.Model)
                .Where(c => c.Status == CarStatusEnum.Available)
                .FirstOrDefaultAsync(
                    x => x.Id == request.CarId,
                    cancellationToken: cancellationToken
                );

            if (car == null)
                return Result<Response>.NotFound("Không tìm thấy xe phù hợp");

            // Check for booking conflicts with already approved/active bookings only
            bool hasConflict = await context
                .Bookings.AsNoTracking()
                .AnyAsync(
                    b =>
                        b.CarId == request.CarId
                        && (
                            b.Status == BookingStatusEnum.Approved
                            || b.Status == BookingStatusEnum.ReadyForPickup
                            || b.Status == BookingStatusEnum.Ongoing
                        )
                        && (
                            // Check if any dates overlap
                            b.StartTime.Date <= request.EndTime.Date
                            && b.EndTime.Date >= request.StartTime.Date
                        ),
                    cancellationToken
                );

            if (hasConflict)
            {
                return Result.Conflict(
                    "Xe đã được đặt trong khoảng thời gian này. Vui lòng chọn ngày khác."
                );
            }

            const decimal platformFeeRate = 0.1m;

            Guid bookingId = Uuid.NewDatabaseFriendly(Database.PostgreSql);
            var totalBookingDay = Math.Ceiling((request.EndTime - request.StartTime).TotalDays);
            var basePrice = car.Price * (decimal)totalBookingDay;
            var platformFee = basePrice * platformFeeRate;
            var totalAmount = basePrice + platformFee;

            var booking = new Booking
            {
                Id = bookingId,
                UserId = currentUser.User.Id,
                CarId = request.CarId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                ActualReturnTime = request.EndTime, // Update later when user return car
                BasePrice = basePrice,
                PlatformFee = platformFee,
                ExcessDay = 0,
                ExcessDayFee = 0,
                TotalAmount = totalAmount,
                Note = string.Empty,
            };

            string standardClauses = GetStandardContractClauses(
                basePrice,
                booking.StartTime,
                booking.EndTime,
                (int)totalBookingDay
            );
            string fullTerms = string.IsNullOrWhiteSpace(car.Terms)
                ? standardClauses
                : car.Terms + "<br/><br/>" + standardClauses;

            var contract = new Contract
            {
                BookingId = booking.Id,
                StartDate = booking.StartTime,
                EndDate = booking.EndTime,
                Terms = fullTerms,
                DriverSignatureDate = DateTimeOffset.UtcNow,
            };

            context.Bookings.Add(booking);
            context.Contracts.Add(contract);
            await context.SaveChangesAsync(cancellationToken);

            backgroundJobClient.Enqueue(
                () =>
                    SendEmail(
                        request.StartTime,
                        request.EndTime,
                        totalAmount,
                        currentUser.User.Name,
                        currentUser.User.Email,
                        car.Owner.Name,
                        car.Owner.Email,
                        car.Model.Name
                    )
            );

            // Schedule automated reminders and expiration
            await reminderService.ScheduleReminders(booking.Id);

            return Result<Response>.Success(new Response(bookingId), ResponseMessages.Created);
        }

        private static string GetStandardContractClauses(
            decimal rentalPrice,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            int rentalPediod
        )
        {
            return @$"
                    <div class='clause'>
                    <strong>Điều 1: Đối tượng hợp đồng</strong>
                    <p>
                        Bên A đồng ý cho Bên B thuê xe với các thông tin như đã mô tả ở phần thông tin xe thuê.
                    </p>
                    </div>
                    <div class='clause'>
                        <strong>Điều 2: Thời hạn hợp đồng</strong>
                        <p>
                            Hợp đồng có hiệu lực từ {startDate:dd/MM/yyyy} đến {endDate:dd/MM/yyyy}. Thời hạn thuê: {rentalPediod} ngày.
                        </p>
                    </div>
                    <div class='clause'>
                        <strong>Điều 3: Giá thuê và phương thức thanh toán</strong>
                        <p>
                            Giá thuê xe: {rentalPrice} VNĐ. Giá trên chưa bao gồm các khoản đền bù, VAT và phụ phí (nếu có).
                            Phương thức thanh toán được thỏa thuận giữa hai bên.
                        </p>
                    </div>
                    <div class='clause'>
                        <strong>Điều 4: Quyền và nghĩa vụ của Bên A (Chủ xe)</strong>
                        <p>
                            a) Bên A cam kết bàn giao xe đúng như mô tả và đảm bảo xe có đầy đủ giấy tờ hợp pháp. <br/>
                            b) Bên A có trách nhiệm bảo trì, bảo dưỡng xe định kỳ, đảm bảo xe luôn trong tình trạng sử dụng tốt. <br/>
                            c) Nếu xảy ra tranh chấp về quyền sở hữu hoặc sử dụng xe, Bên A chịu trách nhiệm giải quyết theo pháp luật.
                        </p>
                    </div>
                    <div class='clause'>
                        <strong>Điều 5: Quyền và nghĩa vụ của Bên B (Người thuê xe)</strong>
                        <p>
                            a) Bên B có trách nhiệm sử dụng xe đúng mục đích, bảo quản xe cẩn thận và không được tự ý thay đổi cấu trúc xe nếu chưa được Bên A đồng ý.<br/>
                            b) Bên B cam kết thanh toán đầy đủ số tiền thuê theo thỏa thuận. <br/>
                            c) Trong trường hợp Bên B vi phạm quy định giao thông, gây ra tai nạn, hoặc không tuân thủ các điều khoản bảo quản xe, Bên B phải chịu phạt và bồi thường thiệt hại cho Bên A theo quy định của pháp luật.
                        </p>
                    </div>
                    <div class='clause'>
                        <strong>Điều 6: Phạt vi phạm và xử lý sự cố</strong>
                        <p>
                            a) Nếu trong quá trình thuê xe, Bên B bị xử phạt vì vi phạm giao thông, Bên B chịu trách nhiệm thanh toán phạt và bồi thường thiệt hại liên quan. <br/>
                            b) Nếu xe bị hư hỏng do lỗi của Bên B mà không thông báo kịp thời cho Bên A, Bên B sẽ bị phạt theo mức đã thỏa thuận và phải bồi thường thiệt hại. <br/>
                            c) Nếu sự cố không được thông báo đúng thời hạn, Bên A có quyền đơn phương chấm dứt hợp đồng.
                        </p>
                    </div>
                    <div class='clause'>
                        <strong>Điều 7: Điều khoản chung</strong>
                        <p>
                            a) Hai bên cam kết thực hiện đầy đủ các điều khoản của hợp đồng này. <br/>
                            b) Mọi tranh chấp phát sinh từ hợp đồng sẽ được thương lượng giải quyết; nếu không đạt được thỏa thuận, tranh chấp sẽ được giải quyết tại Tòa án có thẩm quyền. <br/>
                            c) Hợp đồng được lập thành 02 bản có giá trị pháp lý như nhau, mỗi bên giữ 01 bản.
                        </p>
                    </div>";
        }

        public async Task SendEmail(
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            decimal totalAmount,
            string driverName,
            string driverEmail,
            string ownerName,
            string ownerEmail,
            string carModelName
        )
        {
            var driverEmailTemplate = DriverCreateBookingTemplate.Template(
                driverName,
                carModelName,
                startTime,
                endTime,
                totalAmount
            );

            await emailService.SendEmailAsync(driverEmail, "Xác nhận đặt xe", driverEmailTemplate);

            var ownerEmailTemplate = OwnerCreateBookingTemplate.Template(
                ownerName,
                carModelName,
                driverName,
                startTime,
                endTime,
                totalAmount
            );

            await emailService.SendEmailAsync(ownerEmail, "Yêu Cầu Đặt Xe Mới", ownerEmailTemplate);
        }
    }

    public sealed class Validator : AbstractValidator<CreateBookingCommand>
    {
        private const int MAX_BOOKING_DAYS = 30;
        private const double MIN_HOURS_BEFORE_START = 1.5;
        private const int EARLIST_HOUR = 6;
        private const int LATEST_HOUR = 22;

        public Validator()
        {
            RuleFor(x => x.CarId).NotEmpty().WithMessage("Car không được để trống");

            RuleFor(x => x.StartTime)
                .NotEmpty()
                .WithMessage("Phải chọn thời gian bắt đầu thuê")
                .GreaterThan(DateTime.UtcNow.AddHours(MIN_HOURS_BEFORE_START))
                .WithMessage("Thời gian bắt đầu thuê phải sau một tiếng rưỡi")
                .Must(
                    (command, endTime) =>
                    {
                        var duration = (int)(endTime - command.StartTime).TotalDays;
                        return duration <= MAX_BOOKING_DAYS;
                    }
                )
                .WithMessage($"Thời gian thuê phải từ 1 đến {MAX_BOOKING_DAYS} ngày");

            RuleFor(x => x.EndTime)
                .NotEmpty()
                .WithMessage("Phải chọn thời gian kết thúc thuê")
                .GreaterThan(x => x.StartTime)
                .WithMessage("Thời gian kết thúc thuê phải sau thời gian bắt đầu thuê");
        }
    }
}
