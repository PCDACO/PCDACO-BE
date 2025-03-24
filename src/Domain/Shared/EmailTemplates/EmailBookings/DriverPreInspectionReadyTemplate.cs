namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverPreInspectionReadyTemplate
{
    public static string Template(
        string driverName,
        string carName,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        string pickupAddress,
        string baseUrl = "http://localhost:8080"
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.SuccessHeader)}'>
                    <h2 style='margin: 0;'>Xe Sẵn Sàng Để Nhận</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {driverName},</p>
                    <p>Chủ xe đã hoàn tất kiểm tra xe {carName} và xe đã sẵn sàng để bạn nhận.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.SuccessBackground)}'>
                        <h3 style='color: {EmailTemplateColors.SuccessAccent}; margin-top: 0;'>Chi Tiết Chuyến Đi:</h3>
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
                                <td style='padding: 8px 0;'><strong>Địa điểm nhận xe:</strong></td>
                                <td style='text-align: right;'>{pickupAddress}</td>
                            </tr>
                        </table>
                    </div>

                    <div style='background-color: {EmailTemplateColors.Warning}; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Nhớ mang theo giấy tờ cần thiết khi nhận xe:</strong></p>
                        <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
                            <li>Căn cước công dân (CCCD)</li>
                            <li>Giấy phép lái xe (GPLX)</li>
                            <li>Giấy tờ thế chấp</li>
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
