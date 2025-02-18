namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverApproveBookingTemplate
{
    public static string Template(
        string customerName,
        string carName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal totalAmount,
        bool isApproved
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

        return $@"
            <div style='font-family: Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333; line-height: 1.6;'>
                <div style='background-color: {headerColor}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                    <h2 style='margin: 0;'>Thông Báo Đặt Xe</h2>
                </div>

                <div style='padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;'>
                    <p>Xin chào {customerName},</p>
                    <p>Yêu cầu đặt xe {carName} của bạn đã được chủ xe <strong>{status}</strong>.</p>

                    <div style='background-color: {bgColor}; padding: 20px; border-radius: 8px; margin: 20px 0;'>
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
                    " : "")}

                    <p style='text-align: center; color: {EmailTemplateColors.Footer}; margin-top: 30px;'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
