namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverBookingOverdueTemplate
{
    public static string Template(
        string customerName,
        string carName,
        DateTimeOffset endTime,
        DateTimeOffset currentTime
    )
    {
        var hoursLate = Math.Ceiling((currentTime - endTime).TotalHours);

        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.RejectedHeader)}'>
                    <h2 style='margin: 0;'>Cảnh Báo: Xe Chưa Được Trả</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {customerName},</p>
                    <p>Chúng tôi nhận thấy xe {carName} chưa được trả đúng hạn.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.RejectedBackground)}'>
                        <h3 style='color: {EmailTemplateColors.RejectedAccent}; margin-top: 0;'>Chi Tiết Quá Hạn:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Thời gian kết thúc:</strong></td>
                                <td style='text-align: right;'>{endTime:HH:mm dd/MM/yyyy}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Thời gian hiện tại:</strong></td>
                                <td style='text-align: right;'>{currentTime:HH:mm dd/MM/yyyy}</td>
                            </tr>
                            <tr style='border-top: 2px solid #ddd;'>
                                <td style='padding: 8px 0;'><strong>Số giờ quá hạn:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.RejectedAccent}; font-weight: bold;'>{hoursLate} giờ</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; color: #D63301;'><strong>QUAN TRỌNG:</strong></p>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Vui lòng trả xe ngay lập tức để tránh phí phát sinh</li>
                            <li>Phí phát sinh sẽ được tính 120% giá thuê mỗi ngày quá hạn</li>
                            <li>Việc trả xe trễ ảnh hưởng đến người thuê tiếp theo</li>
                            <li>Tài khoản của bạn có thể bị hạn chế nếu tiếp tục trễ hạn</li>
                        </ul>
                    </div>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã hợp tác!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
