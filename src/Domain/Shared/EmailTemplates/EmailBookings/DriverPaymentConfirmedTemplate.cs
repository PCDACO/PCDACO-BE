namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverPaymentConfirmedTemplate
{
    public static string Template(
        string customerName,
        string carName,
        decimal amount,
        DateTimeOffset paymentDate
    )
    {
        return $@"
            <div style='font-family: Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333; line-height: 1.6;'>
                <div style='background-color: {EmailTemplateColors.SuccessHeader}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                    <h2 style='margin: 0;'>Xác Nhận Thanh Toán</h2>
                </div>

                <div style='padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;'>
                    <p>Xin chào {customerName},</p>
                    <p>Chúng tôi đã nhận được thanh toán của bạn cho việc thuê xe {carName}.</p>

                    <div style='background-color: {EmailTemplateColors.SuccessBackground}; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: {EmailTemplateColors.SuccessAccent}; margin-top: 0;'>Chi Tiết Thanh Toán:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Số tiền:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.SuccessAccent}; font-weight: bold;'>{amount:N0} VNĐ</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Thời gian thanh toán:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.SuccessAccent};'>{paymentDate::HH:mm dd/MM/yyyy}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Trạng thái:</strong></td>
                                <td style='text-align: right;'>Đã hoàn thành</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Vui lòng lưu giữ email này làm bằng chứng thanh toán.</p>
                    </div>

                    <p style='text-align: center; color: {EmailTemplateColors.Footer}; margin-top: 30px;'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
