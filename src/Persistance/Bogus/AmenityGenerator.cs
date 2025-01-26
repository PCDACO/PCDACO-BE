using Domain.Entities;

using UUIDNext;

namespace Persistance.Bogus;

public class AmenityGenerator
{
    public static Amenity[] Execute()
    {
        return [.. CarFeatures.Select(feature =>
        {
            bool isRandom = new Random().Next(0, 2) == 1;
            bool isRandomUpdate = new Random().Next(0, 2) == 1;
            return new Amenity
            {
                Name = feature.Key,
                Description = feature.Value,
                UpdatedAt = isRandomUpdate ? DateTime.UtcNow : null,
                IsDeleted = isRandom,
                DeletedAt = isRandom ? DateTime.UtcNow : null
            };
        })];
    }

    private readonly static Dictionary<string, string> CarFeatures = new()
    {
            { "Cruise Control", "Hệ thống kiểm soát hành trình, giúp xe duy trì tốc độ ổn định mà không cần giữ chân ga." },
            { "Heated Seats", "Ghế có chức năng sưởi, giữ ấm cơ thể trong thời tiết lạnh." },
            { "Sunroof", "Cửa sổ trời, mang lại ánh sáng tự nhiên và không khí thoáng đãng vào trong xe." },
            { "Bluetooth Connectivity", "Kết nối không dây với thiết bị di động để thực hiện cuộc gọi hoặc nghe nhạc." },
            { "Parking Sensors", "Cảm biến hỗ trợ đỗ xe, giúp phát hiện chướng ngại vật xung quanh xe." },
            { "Rearview Camera", "Camera hỗ trợ quan sát phía sau xe khi lùi." },
            { "Blind Spot Monitor", "Hệ thống cảnh báo điểm mù, giúp lái xe an toàn hơn khi chuyển làn." },
            { "Automatic Climate Control", "Hệ thống điều hòa tự động, điều chỉnh nhiệt độ phù hợp theo cài đặt." },
            { "Keyless Entry", "Hệ thống mở khóa không cần chìa, cho phép mở cửa xe bằng cảm biến." },
            { "Adaptive Headlights", "Đèn pha tự động điều chỉnh hướng sáng theo góc lái để cải thiện tầm nhìn." },
            { "Lane Keeping Assist", "Hệ thống hỗ trợ giữ làn đường, cảnh báo hoặc điều chỉnh khi xe lệch khỏi làn." },
            { "Wireless Charging", "Tính năng sạc không dây dành cho các thiết bị di động tương thích." },
            { "Ventilated Seats", "Ghế thông gió, giữ mát cơ thể khi ngồi lâu trong xe." },
            { "360-Degree Camera", "Camera toàn cảnh, cung cấp hình ảnh 360 độ xung quanh xe." },
            { "Head-Up Display (HUD)", "Màn hình hiển thị thông tin trên kính chắn gió, giúp người lái dễ quan sát mà không rời mắt khỏi đường." },
            { "Automatic Emergency Braking", "Phanh khẩn cấp tự động, hỗ trợ giảm thiểu va chạm." },
            { "Remote Start", "Khởi động từ xa, giúp làm mát hoặc sưởi ấm xe trước khi vào." },
            { "Apple CarPlay/Android Auto", "Tích hợp điện thoại thông minh vào màn hình xe để sử dụng các ứng dụng như bản đồ và nhạc." },
            { "Massage Seats", "Ghế có chức năng massage, giúp giảm mệt mỏi trong các chuyến đi dài." },
            { "Premium Sound System", "Hệ thống âm thanh cao cấp, mang lại trải nghiệm nghe nhạc sống động." },
            { "Electric Tailgate", "Cửa hậu điện, có thể mở hoặc đóng tự động." },
            { "Ambient Lighting", "Hệ thống đèn nội thất, tạo không gian thoải mái và sang trọng trong xe." },
            { "Driver Attention Monitoring", "Hệ thống giám sát sự tập trung của tài xế, cảnh báo khi phát hiện dấu hiệu mất tập trung." },
            { "Cross-Traffic Alert", "Cảnh báo phương tiện cắt ngang, hỗ trợ khi lùi xe." },
            { "Rain-Sensing Wipers", "Gạt mưa tự động kích hoạt khi phát hiện nước trên kính chắn gió." },
            { "All-Wheel Drive (AWD)", "Hệ thống dẫn động bốn bánh, cải thiện độ bám đường trong điều kiện khó khăn." },
            { "Hill Start Assist", "Hỗ trợ khởi hành ngang dốc, ngăn xe bị trôi khi khởi động trên dốc." },
            { "Adaptive Cruise Control", "Kiểm soát hành trình thích ứng, tự động điều chỉnh tốc độ để giữ khoảng cách an toàn với xe phía trước." },
            { "Heated Steering Wheel", "Vô lăng có chức năng sưởi, giữ ấm tay trong thời tiết lạnh." },
            { "Power Adjustable Seats", "Ghế chỉnh điện, dễ dàng điều chỉnh vị trí ngồi phù hợp." },
            { "Split-Folding Rear Seats", "Ghế sau gập linh hoạt, tăng không gian chứa đồ." },
            { "Traffic Sign Recognition", "Hệ thống nhận diện biển báo giao thông và hiển thị cho người lái." },
            { "Night Vision", "Hệ thống quan sát ban đêm, hỗ trợ lái xe trong điều kiện ánh sáng yếu." },
            { "Digital Key", "Chìa khóa kỹ thuật số, sử dụng điện thoại để mở và khởi động xe." },
            { "Over-The-Air Updates", "Cập nhật phần mềm xe từ xa thông qua internet." },
            { "Rear Entertainment System", "Hệ thống giải trí phía sau, bao gồm màn hình và thiết bị phát nhạc/video." },
            { "Power Sliding Doors", "Cửa trượt điện, thuận tiện cho việc lên xuống xe." },
            { "Hands-Free Tailgate", "Cửa hậu rảnh tay, có thể mở bằng cách đá chân dưới cản sau." }
        };
}