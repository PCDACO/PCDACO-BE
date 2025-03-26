namespace Domain.Shared.EmailTemplates.EmailReports;

public static class ReportPaymentNotificationTemplate
{
    public static string Template(
        string userName,
        string reportTitle,
        decimal amount,
        DateTimeOffset dueDate
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.Warning)}'>
                    <h2 style='margin: 0; color: #D63301;'>Thông Báo Thanh Toán Báo Cáo</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {userName},</p>
                    <p>Chúng tôi gửi thông báo này để nhắc nhở bạn về việc thanh toán phí cho báo cáo <strong>{reportTitle}</strong>.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.Warning)}'>
                        <h3 style='color: #D63301; margin-top: 0;'>Chi Tiết Thanh Toán:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Tiêu đề báo cáo:</strong></td>
                                <td style='text-align: right;'>{reportTitle}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Số tiền cần thanh toán:</strong></td>
                                <td style='text-align: right; color: #D63301; font-weight: bold;'>{amount:N0} VNĐ</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Hạn thanh toán:</strong></td>
                                <td style='text-align: right;'>{dueDate:HH:mm dd/MM/yyyy}</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: #ffebee; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; color: #C62828;'><strong>QUAN TRỌNG - Hậu quả nếu không thanh toán:</strong></p>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Tài khoản của bạn sẽ bị khóa sau 5 ngày nếu không thanh toán</li>
                            <li>Bạn sẽ không thể tạo đơn đặt xe mới cho đến khi cung cấp bằng chứng thanh toán</li>
                        </ul>
                    </div>


                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Sau khi thanh toán, vui lòng:</p>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Lưu lại biên nhận thanh toán</li>
                            <li>Gửi biên nhận cho bộ phận hỗ trợ nếu tài khoản bị khóa</li>
                            <li>Chờ tối đa 24h để tài khoản được mở khóa sau khi cung cấp bằng chứng</li>
                        </ul>
                    </div>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
