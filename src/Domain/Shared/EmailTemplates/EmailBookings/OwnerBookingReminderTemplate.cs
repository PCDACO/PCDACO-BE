using System;

namespace Domain.Shared.EmailTemplates.EmailBookings
{
    public static class OwnerBookingReminderTemplate
    {
        public static string Template(
            string ownerName,
            string driverName,
            string carName,
            DateTime bookingCreatedTime,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            decimal totalAmount,
            int reminderLevel
        )
        {
            // Determine the reminder message based on the level.
            string reminderMessage = reminderLevel switch
            {
                1 => "Đây là lời nhắc nhở đầu tiên. Vui lòng kiểm tra yêu cầu đặt xe của bạn.",
                2
                    => "Đây là lời nhắc nhở thứ hai. Yêu cầu đặt xe vẫn chưa được xử lý. Vui lòng phản hồi ngay.",
                _
                    => "Đây là lời nhắc cuối cùng. Nếu không phản hồi, yêu cầu đặt xe sẽ bị hủy tự động."
            };

            return $@"
                <div style='{EmailTemplateStyles.ContainerStyle}'>
                    <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.NewBookingHeader)}'>
                        <h2 style='margin: 0;'>Nhắc Nhở: Yêu Cầu Đặt Xe Chưa Phản Hồi</h2>
                    </div>

                    <div style='{EmailTemplateStyles.BodyStyle}'>
                        <p>Xin chào {ownerName},</p>
                        <p>Bạn có một yêu cầu đặt xe từ {driverName} cho xe <strong>{carName}</strong> được tạo vào {bookingCreatedTime:HH:mm dd/MM/yyyy} đang chờ phản hồi của bạn.</p>

                        <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.NewBookingBackground)}'>
                            <h3 style='color: {EmailTemplateColors.NewBookingAccent}; margin-top: 0;'>Chi Tiết Yêu Cầu:</h3>
                            <table style='width: 100%; border-collapse: collapse;'>
                                <tr>
                                    <td style='padding: 8px 0;'><strong>Xe:</strong></td>
                                    <td style='text-align: right;'>{carName}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0;'><strong>Thời gian bắt đầu:</strong></td>
                                    <td style='text-align: right;'>{startTime:HH:mm dd/MM/yyyy}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 8px 0;'><strong>Thời gian kết thúc:</strong></td>
                                    <td style='text-align: right;'>{endTime:HH:mm dd/MM/yyyy}</td>
                                </tr>
                                <tr style='border-top: 2px solid #ddd;'>
                                    <td style='padding: 8px 0;'><strong>Tổng tiền:</strong></td>
                                    <td style='text-align: right; color: {EmailTemplateColors.NewBookingAccent}; font-weight: bold;'>{totalAmount:N0} VNĐ</td>
                                </tr>
                            </table>
                        </div>

                        <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>Lời nhắc:</strong> {reminderMessage}</p>
                        </div>

                        <p style='{EmailTemplateStyles.FooterStyle}'>
                            Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                            <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                        </p>
                    </div>
                </div>";
        }
    }
}
