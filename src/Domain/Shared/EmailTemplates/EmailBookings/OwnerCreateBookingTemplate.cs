namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class OwnerCreateBookingTemplate
{
    public static string Template(
        string ownerName,
        string carName,
        string customerName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal totalAmount
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.NewBookingBackground)}'>
                    <h2 style='margin: 0;'>Yêu Cầu Đặt Xe Mới</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {ownerName},</p>
                    <p>Bạn có một yêu cầu đặt xe mới cho xe <strong>{carName}</strong>.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.NewBookingBackground)}'>
                        <h3 style='color: {EmailTemplateColors.NewBookingAccent}; margin-top: 0;'>Thông Tin Đặt Xe:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Khách hàng:</strong></td>
                                <td style='text-align: right;'>{customerName}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Thời gian bắt đầu:</strong></td>
                                <td style='text-align: right;'>{startTime:HH:mm dd/MM/yyyy}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Thời gian kết thúc:</strong></td>
                                <td style='text-align: right;'>{endTime:HH:mm dd/MM/yyyy}</td>
                            </tr>
                            <tr style='border-top: 2px solid #FFE0B2;'>
                                <td style='padding: 8px 0;'><strong>Tổng tiền:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.NewBookingAccent}; font-weight: bold;'>{totalAmount:N0} VNĐ</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Vui lòng phản hồi yêu cầu đặt xe trong vòng 24 giờ.</p>
                    </div>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
