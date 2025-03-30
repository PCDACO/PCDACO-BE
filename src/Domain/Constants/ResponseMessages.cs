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
    public const string NewPasswordIsSameAsOld = "Mật khẩu mới không được trùng với mật khẩu cũ";

    // USER ROLE
    public const string MustBeConsultantOrTechnician = "Vai trò phải là consultant hoặc technician";
    public const string RoleNotFound = "Không tìm thấy vai trò";

    // GPS DEVICES
    public const string GPSDeviceNotFound = "Không tìm thấy thiết bị GPS";
    public const string GPSDeviceIsExisted = "Thiết bị GPS đã tồn tại";
    public const string GPSDeviceHasCarGPS = "Thiết bị GPS đã được đăng kí sử dụng";
    public const string GPSDeviceIsNotAvailable = "Thiết bị GPS không khả dụng";

    // CAR GPS
    public const string CarGPSIsExisted = "Thiết bị GPS này đang được sử dụng";

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
    public const string CarHadInspectionSchedule = "Xe đã có lịch kiểm định";
    public const string CarMustBeAvailableToBeDisabled =
        "Xe phải đang ở trạng thái có sẵn mới có thể tạm dừng hoạt động";
    public const string CarHasActiveBookings = "Xe đang có lịch đặt, không thể tạm dừng hoạt động";

    public const string CarDisabledSuccessfully = "Xe đã được tạm dừng hoạt động thành công";
    public const string CarEnabledSuccessfully = "Xe đã được kích hoạt lại thành công";

    public const string CarMustBeInactiveToBeEnabled =
        "Xe phải đang ở trạng thái tạm dừng hoạt động mới có thể kích hoạt lại";

    // MODEL
    public const string ModelNotFound = "Không tìm thấy dòng xe";

    // Inspection Schedules
    public const string InspectionScheduleNotFound = "Không tìm thấy lịch kiểm định";
    public const string OnlyUpdatePendingInspectionSchedule =
        "Chỉ có thể cập nhật lịch kiểm định đang chờ duyệt";
    public const string OnlyUpdateInProgressInspectionSchedule =
        "Chỉ có thể duyệt lịch kiểm định đang được tiến hành";
    public const string OnlyDeletePendingInspectionSchedule =
        "Chỉ có thể xóa lịch kiểm định đang chờ duyệt";
    public const string CannotDeleteScheduleHasInspectionDateLessThen1DayFromNow =
        "Không thể xóa lịch kiểm định có ngày kiểm định cách ngày hiện tại ít hơn 1 ngày";
    public const string InspectionScheduleExpired =
        "Chỉ được thực hiện kiểm định trong khoảng 15 phút sau thời gian kiểm định";
    public const string TechnicianHasInspectionScheduleWithinOneHour =
        "Không thể tạo lịch kiểm định có thời gian kiểm định cách nhau ít hơn 1 giờ so với lịch kiểm định khác";
    public const string CarHadExpiredInspectionScheduleWithThisTechnician =
        "Không thể tạo lịch kiểm định với cùng 1 kỹ thuật viên sau khi lịch kiểm định quá hạn. Vui lòng chọn kỹ thuật viên khác";
    public const string HasOverLapScheduleWithTheSameTechnician =
        "Thời gian kiểm định đã được tạo cho kỹ thuật viên này bị trùng với lịch kiểm định đã được duyệt";
    public const string OnlyUpdateSignedOrInprogressInspectionSchedule =
        "Chỉ có thể cập nhật lịch kiểm định đã được ký hợp đồng";

    // Inspection Statuses
    public const string InspectionStatusNotFound = "Không tìm thấy trạng thái kiểm định";
    public const string ApproveStatusNotFound = "Không tìm thấy trạng thái phê duyệt";
    public const string RejectStatusNotFound = "Không tìm thấy trạng thái từ chối phê duyệt";
    public const string InProgressStatusNotAvailable =
        "Trạng thái kiểm định đang tiến hành không có sẵn";
    public const string PendingStatusNotAvailable = "Trạng thái kiểm định chờ duyệt không có sẵn";

    // BOOKING
    public const string BookingNotFound = "Không tìm thấy booking";
    public const string OnlyCreatorCanAccessBooking =
        "Bạn không có quyền thực hiện chức năng này với booking này!";

    // Bank Info
    public const string BankInfoNotFound = "Không tìm thấy thông tin ngân hàng";

    // Bank Account
    public const string BankAccountNotFound = "Không tìm thấy tài khoản ngân hàng";

    // Report
    public const string ReportNotFound = "Không tìm thấy báo cáo";
    public const string ReportNotUnderReviewed = "Báo cáo không ở trạng thái đang chờ duyệt";
}
