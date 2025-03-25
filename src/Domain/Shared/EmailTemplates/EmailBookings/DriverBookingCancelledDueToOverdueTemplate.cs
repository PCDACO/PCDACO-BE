namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverBookingCancelledDueToOverdueTemplate
{
    public static string Template(
        string customerName,
        string carName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        decimal compensationAmount
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.RejectedHeader)}'>
                    <h2 style='margin: 0;'>Thông Báo Hủy Đặt Xe</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {customerName},</p>
                    <p>Rất tiếc phải thông báo rằng đơn đặt xe {carName} của bạn đã bị hủy do người thuê trước chưa trả xe đúng hạn.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.RejectedBackground)}'>
                        <h3 style='color: {EmailTemplateColors.RejectedAccent}; margin-top: 0;'>Chi Tiết Đặt Xe Bị Hủy:</h3>
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
                                <td style='padding: 8px 0;'><strong>Bồi thường:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.CompleteAccent}; font-weight: bold;'>{compensationAmount:N0} VNĐ</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Thông tin bồi thường:</strong></p>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Toàn bộ số tiền đặt cọc (nếu có) sẽ được hoàn trả</li>
                            <li>Bạn sẽ nhận được khoản bồi thường như chi tiết bên trên</li>
                            <li>Tiền sẽ được chuyển vào ví của bạn trong hệ thống</li>
                            <li>Bạn có thể sử dụng số tiền này cho lần đặt xe tiếp theo</li>
                        </ul>
                    </div>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Chúng tôi thành thật xin lỗi vì sự bất tiện này!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
