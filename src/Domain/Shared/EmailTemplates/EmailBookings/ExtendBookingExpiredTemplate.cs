namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class ExtendBookingExpiredTemplate
{
    public static string Template(
        string name,
        string carName,
        DateTimeOffset originalEndDate,
        DateTimeOffset requestedEndDate
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.WarningHeader)}'>
                    <h2 style='margin: 0;'>Thông Báo Hết Hạn Gia Hạn Thuê Xe</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {name},</p>
                    <p>Yêu cầu gia hạn thuê xe {carName} của bạn đã hết hạn do không thanh toán trong thời gian quy định.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.WarningBackground)}'>
                        <h3 style='margin-top: 0;'>Chi tiết:</h3>
                        <ul>
                            <li>Ngày kết thúc ban đầu: {originalEndDate:dd/MM/yyyy HH:mm}</li>
                            <li>Ngày kết thúc yêu cầu: {requestedEndDate:dd/MM/yyyy HH:mm}</li>
                        </ul>
                    </div>

                    <p>Thời gian thuê xe sẽ được giữ nguyên theo lịch ban đầu.</p>

                    <p style='{EmailTemplateStyles.FooterStyle}'>
                        Nếu bạn vẫn muốn gia hạn, vui lòng tạo yêu cầu mới.<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
