namespace Domain.Constants;

public class ResponseMessages
{
    // Shared
    public const string Created = "Tạo thành công";
    public const string Updated = "Cập nhật thành công";
    public const string Deleted = "Xóa thành công";

    public const string Fetched = "Lấy dữ liệu thành công";

    // PERMISSION
    public const string UnauthourizeAccess = "Bạn không có quyền truy cập";
    public const string ForbiddenAudit = "Bạn không có quyền thực hiện thao tác này";

    // USER
    public const string UserNotFound = "Không tìm thấy người dùng";
    public const string OldPasswordIsInvalid = "Mật khẩu cũ không đúng";
    public const string EmailAddressIsExisted = "Email đã tồn tại";
    public const string PhoneNumberIsExisted = "Số điện thoại đã tồn tại";
    public const string TechnicianNotFound = "Không tìm thấy kiểm định viên";

    // USER ROLE
    public const string MustBeConsultantOrTechnician = "Vai trò phải là consultant hoặc technician";
    public const string RoleNotFound = "Không tìm thấy vai trò";

    // GPS DEVICES
    public const string GPSDeviceNotFound = "Không tìm thấy thiết bị GPS";
    public const string GPSDeviceIsExisted = "Thiết bị GPS đã tồn tại";
    public const string GPSDeviceHasCarGPS = "Thiết bị GPS đã được đăng kí sử dụng";

    // DEVICE STATUS
    public const string DeviceStatusNotFound = "Không tìm thấy trạng thái thiết bị";

    // AMENITIES
    public const string AmenitiesNotFound = "Không tìm thấy tiện nghi";

    // TRANSMISSION TYPES
    public const string TransmissionTypeNotFound = "Không tìm thấy kiểu truyền động";

    // FUEL TYPES
    public const string FuelTypeNotFound = "Không tìm thấy kiểu nhiên liệu";

    // CAR STATUSES
    public const string CarStatusNotFound = "Không tìm thấy trạng thái của xe";

    // CAR
    public const string CarNotFound = "Không tìm thấy xe";
    public const string CarIsNotInPending = "Xe không ở trạng thái chờ duyệt";

    // MODEL
    public const string ModelNotFound = "Không tìm thấy dòng xe";

    // Inspection Schedules
    public const string InspectionScheduleNotFound = "Không tìm thấy lịch kiểm định";
    public const string OnlyUpdatePendingInspectionSchedule =
        "Chỉ có thể cập nhật lịch kiểm định đang chờ duyệt";

    // Inspection Statuses
    public const string InspectionStatusNotFound = "Không tìm thấy trạng thái kiểm định";
    public const string ApproveStatusNotFound = "Không tìm thấy trạng thái phê duyệt";
}
