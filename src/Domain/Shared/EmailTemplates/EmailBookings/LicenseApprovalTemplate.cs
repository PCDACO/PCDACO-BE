namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class LicenseApprovalTemplate
{
    public static string Template(string userName, bool isApproved, string? rejectReason = null)
    {
        var (bgColor, headerColor, accentColor) = isApproved
            ? (
                EmailTemplateColors.SuccessBackground,
                EmailTemplateColors.SuccessHeader,
                EmailTemplateColors.SuccessAccent
            )
            : (
                EmailTemplateColors.RejectedBackground,
                EmailTemplateColors.RejectedHeader,
                EmailTemplateColors.RejectedAccent
            );

        var status = isApproved ? "phê duyệt" : "từ chối";
        var message = isApproved
            ? "Giấy phép lái xe của bạn đã được phê duyệt thành công."
            : "Giấy phép lái xe của bạn đã bị từ chối.";

        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(headerColor)}'>
                    <h2 style='margin: 0;'>Thông Báo Phê Duyệt Giấy Phép Lái Xe</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {userName},</p>
                    <p>{message}</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(bgColor)}'>
                        <h3 style='color: {accentColor}; margin-top: 0;'>Chi Tiết:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Trạng thái:</strong></td>
                                <td style='text-align: right; color: {accentColor}; font-weight: bold;'>{status.ToUpper()}</td>
                            </tr>
                            {(!isApproved && !string.IsNullOrEmpty(rejectReason) ? $@"
                            <tr>
                                <td style='padding: 8px 0;'><strong>Lý do từ chối:</strong></td>
                                <td style='text-align: right;'>{rejectReason}</td>
                            </tr>" : "")}
                        </table>
                    </div>

                    {(!isApproved ? $@"
                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Vui lòng kiểm tra lại thông tin giấy phép lái xe của bạn và nộp lại nếu cần thiết.</p>
                    </div>" : "")}

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
