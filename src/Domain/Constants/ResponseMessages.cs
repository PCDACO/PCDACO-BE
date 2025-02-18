namespace Domain.Constants;

public class ResponseMessages
{
    // Shared
    public const string Created = "Tạo thành công";
    public const string Updated = "Cập nhật thành công";
    public const string Deleted = "Xóa thành công";
    // PERMISSION
    public const string UnauthourizeAccess = "Bạn không có quyền truy cập";
    public const string ForbiddenAudit = "Bạn không có quyền thực hiện thao tác này";
    // USER
    public const string UserNotFound = "Không tìm thấy người dùng";
    // GPS DEVICES
    public const string GPSDeviceNotFound = "Không tìm thấy thiết bị GPS";
    public const string GPSDeviceIsExisted = "Đã tồn tại thiết bị GPS";
    public const string GPSDeviceHasCarGPS = "Thiết bị GPS đã được đăng kí sử dụng";
    // DEVICE STATUS
    public const string DeviceStatusNotFound = "Không tìm thấy trạng thái thiết bị";
}