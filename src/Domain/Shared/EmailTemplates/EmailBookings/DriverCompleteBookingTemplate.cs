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
        decimal totalAmount,
        string paymentUrl
    )
    {
        return $@"
            <div style='font-family: Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333; line-height: 1.6;'>
                <div style='background-color: {EmailTemplateColors.CompleteHeader}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                    <h2 style='margin: 0;'>Hoàn Thành Chuyến Đi</h2>
                </div>

                <div style='padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;'>
                    <p>Xin chào {customerName},</p>
                    <p>Chuyến đi của bạn với xe {carName} đã hoàn thành.</p>

                    <div style='background-color: {EmailTemplateColors.CompleteBackground}; padding: 20px; border-radius: 8px; margin: 20px 0;'>
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

                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{paymentUrl}' style='background-color: {EmailTemplateColors.CompleteAccent}; color: white; padding: 12px 30px; text-decoration: none; border-radius: 25px; font-weight: bold; display: inline-block;'>
                            Thanh Toán Ngay
                        </a>
                    </div>

                    <p style='text-align: center; color: {EmailTemplateColors.Footer}; margin-top: 30px;'>
                        Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!<br>
                        <small><strong>Cần hỗ trợ?</strong> Hãy trả lời email này</small>
                    </p>
                </div>
            </div>";
    }
}
