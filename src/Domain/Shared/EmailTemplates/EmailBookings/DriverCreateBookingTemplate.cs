namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverCreateBookingTemplate
{
    public static string Template(
        string customerName,
        string carName,
        DateTime startTime,
        DateTime endTime,
        decimal totalAmount
    )
    {
        return $@"
            <div style='font-family: Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333; line-height: 1.6;'>
                <div style='background-color: {EmailTemplateColors.SuccessHeader}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                    <h2 style='margin: 0;'>Xác Nhận Đặt Xe</h2>
                </div>

                <div style='padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;'>
                    <p>Xin chào {customerName},</p>
                    <p>Yêu cầu đặt xe của bạn đã được tạo thành công và đang chờ chủ xe xác nhận.</p>

                    <div style='background-color: {EmailTemplateColors.SuccessBackground}; padding: 20px; border-radius: 8px; margin: 20px 0;'>
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

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Vui lòng chờ xác nhận từ chủ xe. Bạn sẽ nhận được email thông báo ngay khi chủ xe phản hồi.</p>
                    </div>

                    <p style='text-align: center; color: {EmailTemplateColors.Footer}; margin-top: 30px;'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
