namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class DriverCompleteBookingTemplate
{
    public static string Template(
        string customerName,
        string carName,
        decimal totalDistance,
        decimal basePrice,
        decimal excessFee,
        decimal platformFee,
        decimal totalAmount
    )
    {
        return $@"
            <div style='{EmailTemplateStyles.ContainerStyle}'>
                <div style='{EmailTemplateStyles.HeaderStyle(EmailTemplateColors.CompleteHeader)}'>
                    <h2 style='margin: 0;'>Hoàn Thành Chuyến Đi</h2>
                </div>

                <div style='{EmailTemplateStyles.BodyStyle}'>
                    <p>Xin chào {customerName},</p>
                    <p>Chuyến đi của bạn với xe {carName} đã hoàn thành.</p>

                    <div style='{EmailTemplateStyles.DetailBoxStyle(EmailTemplateColors.CompleteBackground)}'>
                        <h3 style='color: {EmailTemplateColors.CompleteAccent}; margin-top: 0;'>Chi Tiết Chuyến Đi:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Tổng quãng đường:</strong></td>
                                <td style='text-align: right;'>{totalDistance:N1} km</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Giá thuê:</strong></td>
                                <td style='text-align: right;'>{basePrice:N0} VNĐ</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Phí phát sinh:</strong></td>
                                <td style='text-align: right;'>{excessFee:N0} VNĐ</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0;'><strong>Phí phát sinh:</strong></td>
                                <td style='text-align: right;'>{platformFee:N0} VNĐ</td>
                            </tr>
                            <tr style='border-top: 2px solid #ddd;'>
                                <td style='padding: 8px 0;'><strong>Tổng thanh toán:</strong></td>
                                <td style='text-align: right; color: {EmailTemplateColors.CompleteAccent}; font-weight: bold;'>{totalAmount:N0} VNĐ</td>
                            </tr>
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
