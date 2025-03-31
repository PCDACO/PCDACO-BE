namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverCancelBookingTemplate
{
    public static string Template(
        string driverName,
        string carName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal refundAmount,
        string cancelReason,
        bool isOwnerCancelled
    )
    {
        var headerText = isOwnerCancelled ? "Chủ Xe Đã Hủy Đơn" : "Xác Nhận Hủy Đơn";
        var messageText = isOwnerCancelled
            ? "Chủ xe đã hủy đơn đặt xe của bạn."
            : "Yêu cầu hủy đơn đặt xe của bạn đã được xử lý thành công.";

        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.RejectedHeader)}'>
                    <h2 style='margin: 0;'>{headerText}</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {driverName},</p>
                    <p>{messageText}</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.RejectedBackground)}'>
                        <h3 style='color: {EmailTemplateColors.RejectedAccent}; margin-top: 0;'>Chi Tiết Đơn Hủy:</h3>
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
                            <tr>
                                <td style='padding: 8px 0;'><strong>Lý do hủy:</strong></td>
                                <td style='text-align: right;'>{cancelReason}</td>
                            </tr>
                            {(refundAmount > 0 ? $@"
                            <tr style='border-top: 2px solid #ddd;'>
                                <td style='padding: 8px 0;'><strong>Số tiền hoàn trả:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.RejectedAccent}; font-weight: bold;'>{refundAmount:N0} VNĐ</td>
                            </tr>" : "")}
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
