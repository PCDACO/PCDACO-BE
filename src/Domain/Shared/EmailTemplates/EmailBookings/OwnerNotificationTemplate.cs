namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class OwnerNotificationTemplate
{
    public static string Template(
        string ownerName,
        string driverName,
        string carName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal totalAmount
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.SuccessHeader)}'>
                    <h2 style='margin: 0;'>Thông Báo Đặt Xe</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {ownerName},</p>
                    <p>Khách hàng <strong>{driverName}</strong> đã đặt xe <strong>{carName}</strong>.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.SuccessBackground)}'>
                        <h3 style='color: {EmailTemplateColors.SuccessAccent}; margin-top: 0;'>Chi Tiết Đặt Xe:</h3>
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
                                <td style='text-align: right; color: {EmailTemplateColors.SuccessAccent}; font-weight: bold;'>{totalAmount:N0} VNĐ</td>
                            </tr>
                        </table>
                    </div>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
