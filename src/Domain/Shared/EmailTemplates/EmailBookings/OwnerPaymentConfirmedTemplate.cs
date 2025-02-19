namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class OwnerPaymentConfirmedTemplate
{
    public static string Template(
        string ownerName,
        string driverName,
        string carName,
        decimal totalAmount,
        decimal ownerAmount,
        DateTimeOffset paymentDate
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.SuccessHeader)}'>
                    <h2 style='margin: 0;'>Thông Báo Thanh Toán</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {ownerName},</p>
                    <p>Khách hàng <strong>{driverName}</strong> đã hoàn tất thanh toán cho việc thuê xe {carName} của bạn.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.SuccessBackground)}'>
                        <h3 style='color: {EmailTemplateColors.SuccessAccent}; margin-top: 0;'>Chi Tiết Thanh Toán:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Tổng thanh toán:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.SuccessAccent};'>{totalAmount:N0} VNĐ</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Số tiền bạn nhận được:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.SuccessAccent}; font-weight: bold;'>{ownerAmount:N0} VNĐ</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Thời gian thanh toán:</strong></td>
                                <td style='text-align: right;'>{paymentDate:HH:mm dd/MM/yyyy}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Trạng thái:</strong></td>
                                <td style='text-align: right;'>Đã hoàn thành</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Số tiền đã được chuyển vào ví của bạn.</p>
                    </div>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
