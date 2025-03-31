namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class OwnerCancelBookingTemplate
{
    public static string Template(
        string ownerName,
        string carName,
        string customerName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal penaltyAmount,
        string cancelReason,
        bool isOwnerCancelled
    )
    {
        var headerText = isOwnerCancelled ? "Xác Nhận Hủy Đơn" : "Khách Hàng Đã Hủy Đơn";
        var messageText = isOwnerCancelled
            ? "Yêu cầu hủy đơn đặt xe của bạn đã được xử lý thành công."
            : "Khách hàng đã hủy đơn đặt xe.";

        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.RejectedHeader)}'>
                    <h2 style='margin: 0;'>{headerText}</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {ownerName},</p>
                    <p>{messageText}</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.RejectedBackground)}'>
                        <h3 style='color: {EmailTemplateColors.RejectedAccent}; margin-top: 0;'>Chi Tiết Đơn Hủy:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Khách hàng:</strong></td>
                                <td style='text-align: right;'>{customerName}</td>
                            </tr>
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
                            {(penaltyAmount > 0 ? $@"
                            <tr style='border-top: 2px solid #ddd;'>
                                <td style='padding: 8px 0;'><strong>Tiền phạt:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.RejectedAccent}; font-weight: bold;'>{penaltyAmount:N0} VNĐ</td>
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
