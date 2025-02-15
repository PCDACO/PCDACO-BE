namespace Domain.Shared.EmailTemplates.EmailBookings;

public static class GetBookingApprovedTemplate
{
    public static string Template(string customerName, string carName, bool isApproved)
    {
        var (bgColor, headerColor, accentColor) = isApproved
            ? ("#e8f5e9", "#4CAF50", "#2E7D32")
            : ("#ffebee", "#F44336", "#C62828");

        var status = isApproved ? "chấp thuận" : "từ chối";
        var message = isApproved
            ? "Bây giờ bạn có thể tiến hành thuê theo lịch trình."
            : "Vui lòng liên hệ với bộ phận hỗ trợ của chúng tôi nếu bạn có bất kỳ câu hỏi nào.";

        return $@"
            <div style='font-family: Roboto, Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333; line-height: 1.6;'>
                <div style='background-color: {headerColor}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                    <h2 style='margin: 0;'>Thông Báo Đặt Xe</h2>
                </div>

                <div style='padding: 20px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;'>
                    <p>Xin chào {customerName},</p>
                    <p>Yêu cầu đặt xe {carName} của bạn đã được chủ xe <strong>{status}</strong>.</p>

                    <div style='background-color: {bgColor}; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3 style='color: {accentColor}; margin-top: 0;'>Thông Tin Chi Tiết:</h3>
                        <p style='margin-bottom: 0;'>{message}</p>
                    </div>

                    {(isApproved ? @"
                    <div style='background-color: #fff3e0; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Lưu ý:</strong> Vui lòng đến đúng giờ và mang theo giấy tờ cần thiết khi nhận xe.</p>
                    </div>
                    " : "")}

                    <p style='text-align: center; color: #666; margin-top: 30px;'>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                </div>
            </div>";
    }
}
