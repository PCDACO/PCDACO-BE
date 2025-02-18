
using Domain.Entities;

namespace Persistance.Bogus;

public class AmenityGenerator
{
    private record AmenityDummy
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
    }
    public static Amenity[] Execute()
    {
        return [.. arrays.Select(feature =>
        {
            bool isRandom = new Random().Next(0, 2) == 1;
            bool isRandomUpdate = new Random().Next(0, 2) == 1;
            return new Amenity
            {
                Name = feature.Name,
                Description = feature.Description,
                IconUrl =feature.IconUrl,
                UpdatedAt = isRandomUpdate ? DateTime.UtcNow : null,
                IsDeleted = isRandom,
                DeletedAt = isRandom ? DateTime.UtcNow : null
            };
        })];
    }

    private static readonly AmenityDummy[] arrays =
    [
        new(){
        Name = "Bản Đồ",
        Description = "Hệ thống bản đồ giúp tài xế định hướng và tìm đường dễ dàng hơn khi di chuyển.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/evc9i1k2drng1a7lm7bt.svg"
    },
    new() {
        Name = "Camera Hành Trình",
        Description = "Camera ghi lại hành trình di chuyển, giúp lưu lại bằng chứng khi xảy ra va chạm.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/xyyiipesinpmlr2al9ah.svg"
},
    new() {
        Name = "Cảnh Báo Tốc Độ",
        Description = "Cảnh báo khi xe vượt quá tốc độ giới hạn, giúp tài xế lái xe an toàn hơn.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764803/amenity-icon/s6cqhwmvarvjpopaj8ts.svg"
    },
    new() {
        Name = "Lốp Dự Phòng",
        Description = "Lốp dự phòng thay thế khi xe bị thủng lốp hoặc hư hỏng lốp trên đường.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/wxd8wdeihezfb44u2rza.svg"
    },
    new() {
        Name = "Camera 360",
        Description = "Hệ thống camera toàn cảnh giúp tài xế quan sát xung quanh xe một cách trực quan.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/qrl8hs1lnw8s9im24iza.svg"
    },
    new() {
        Name = "Cảm Biến Lốp",
        Description = "Cảm biến theo dõi áp suất lốp, cảnh báo khi lốp bị non hơi hoặc quá căng.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/fw8zia6c92zdlmt4gwxu.svg"
    },
    new() {
        Name = "Định Vị GPS",
        Description = "Hệ thống định vị giúp xác định vị trí xe theo thời gian thực.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764804/amenity-icon/sj8tklczqfnn2tfskekq.svg"
    },
    new() {
        Name = "ETC",
        Description = "Hệ thống thu phí không dừng, giúp xe di chuyển qua trạm thu phí mà không cần dừng lại.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764804/amenity-icon/lxuziipunkd0ejk7m1mq.svg"
    },
    new() {
        Name = "Bluetooth",
        Description = "Kết nối không dây giữa điện thoại và xe để nghe nhạc, nhận cuộc gọi rảnh tay.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/pmt7a8hlbny2yy1ydkof.svg"
    },
    new() {
        Name = "Camera Lùi",
        Description = "Hỗ trợ quan sát phía sau khi lùi xe, giúp tránh va chạm.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/ti6manevdxmvybb8yrcs.svg"
    },
    new() {
        Name = "Cửa Sổ Trời",
        Description = "Cửa sổ trên nóc xe, giúp không gian xe thoáng đãng hơn.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764803/amenity-icon/vwqodbsvuauaj9ghz1ok.svg"
    },
    new() {
        Name = "Màn Hình DVD",
        Description = "Màn hình giải trí hiển thị video, bản đồ hoặc thông tin xe.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/n9swnzsyen6gwscs84gm.svg"
    },
    new() {
        Name = "Camera Cập Lề",
        Description = "Camera hỗ trợ đỗ xe, giúp tài xế quan sát khi cập lề đường.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/ixmpvqruaxw66popspzd.svg"
    },
    new() {
        Name = "Cảm Biến Va Chạm",
        Description = "Cảm biến cảnh báo khi xe sắp va chạm với vật cản phía trước hoặc phía sau.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/guxyshbopwmoktxzbgz0.svg"
    },
    new() {
        Name = "Khe Cắm USB",
        Description = "Cổng USB để sạc thiết bị hoặc kết nối với hệ thống giải trí trên xe.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/civrmsfcb0kenvllo6hh.svg"
    },
    new() {
        Name = "Túi Khí An Toàn",
        Description = "Túi khí giúp giảm chấn thương khi xảy ra va chạm.",
        IconUrl = "https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/fzkmgpy7ztsw5ptnyzlf.svg"
    }
    ];
}