namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverApproveBookingTemplate
{
    public static string Template(
        string customerName,
        string carName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal totalAmount,
        bool isApproved,
        string? paymentToken = null,
        string baseUrl = "http://localhost:8080"
    )
    {
        var (bgColor, headerColor, accentColor) = isApproved
            ? (
                EmailTemplateColors.SuccessBackground,
                EmailTemplateColors.SuccessHeader,
                EmailTemplateColors.SuccessAccent
            )
            : (
                EmailTemplateColors.RejectedBackground,
                EmailTemplateColors.SuccessHeader,
                EmailTemplateColors.RejectedAccent
            );

        var status = isApproved ? "chấp thuận" : "từ chối";

        var paymentButton =
            paymentToken != null
                ? $@"
                    <div style='text-align: center; margin: 20px 0;'>
                        <a href='{baseUrl}/api/bookings/payment/{paymentToken}' style='
                            background-color: {EmailTemplateColors.SuccessAccent};
                            color: white;
                            padding: 12px 24px;
                            text-decoration: none;
                            border-radius: 4px;
                            display: inline-block;
                        '>
                            Thanh toán ngay
                        </a>
                    </div>
                "
                : "";

        return $@"
            <div style=' {EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(headerColor)}'>
                    <h2 style='margin: 0;'>Thông Báo Đặt Xe</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {customerName},</p>
                    <p>Yêu cầu đặt xe {carName} của bạn đã được chủ xe <strong>{status}</strong>.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(bgColor)}'>
                        <h3 style='color: {accentColor}; margin-top: 0;'>Chi Tiết Đặt Xe:</h3>
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
                                <td style='text-align: right; style='color: {accentColor}; font-weight: bold;'>{totalAmount:N0} VNĐ</td>
                            </tr>
                        </table>
                    </div>

                    {(isApproved ? $@"
                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Vui lòng đến đúng giờ và mang theo giấy tờ cần thiết khi nhận xe:</p>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Căn cước công dân (CCCD)</li>
                            <li>Giấy phép lái xe (GPLX)</li>
                            <li>Giấy tờ thế chấp</li>
                        </ul>
                    </div>
                    {paymentButton}
                    " : "")}

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
