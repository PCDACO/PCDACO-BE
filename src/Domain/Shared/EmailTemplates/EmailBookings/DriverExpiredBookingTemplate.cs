namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverExpiredBookingTemplate
{
    public static string Template(
        string driverName,
        string carName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal totalAmount
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.RejectedHeader)}'>
                    <h2 style='margin: 0;'>Thông Báo Đặt Xe Hết Hạn</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {driverName},</p>
                    <p>Yêu cầu đặt xe của bạn cho xe <strong>{carName}</strong> đã hết hạn.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.RejectedBackground)}'>
                        <h3 style='color: {EmailTemplateColors.RejectedAccent}; margin-top: 0;'>Chi Tiết Đặt Xe:</h3>
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
                                <td style='text-align: right; color: {EmailTemplateColors.RejectedAccent}; font-weight: bold;'>{totalAmount:N0} VNĐ</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Vui lòng kiểm tra lại yêu cầu đặt xe của bạn và thực hiện các bước cần thiết để tránh tình trạng hết hạn trong tương lai.</p>
                    </div>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
