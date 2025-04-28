namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class ExtendBookingSuccessTemplate
{
    public static string Template(
        string name,
        string carName,
        DateTimeOffset oldStartDate,
        DateTimeOffset oldEndDate,
        DateTimeOffset newStartDate,
        DateTimeOffset newEndDate,
        decimal? additionalAmount = null
    )
    {
        var additionalAmountHtml = additionalAmount.HasValue
            ? $@"<tr style='border-top: 2px solid #ddd;'>
                <td style='padding: 8px 0;'><strong>Phí bổ sung:</strong></td>
                <td style='text-align: right; color: {EmailTemplateColors.SuccessAccent}; font-weight: bold;'>{additionalAmount:N0} VNĐ</td>
            </tr>"
            : "";

        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.SuccessHeader)}'>
                    <h2 style='margin: 0;'>Thông Báo Thay Đổi Thời Gian Thuê Xe</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {name},</p>
                    <p>Thời gian thuê xe {carName} đã được cập nhật thành công.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.SuccessBackground)}'>
                        <h3 style='color: {EmailTemplateColors.SuccessAccent}; margin-top: 0;'>Chi Tiết Thay Đổi:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Thời gian cũ:</strong></td>
                                <td style='text-align: right;'>{oldStartDate:HH:mm dd/MM/yyyy} - {oldEndDate:HH:mm dd/MM/yyyy}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Thời gian mới:</strong></td>
                                <td style='text-align: right;'>{newStartDate:HH:mm dd/MM/yyyy} - {newEndDate:HH:mm dd/MM/yyyy}</td>
                            </tr>
                            {additionalAmountHtml}
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
