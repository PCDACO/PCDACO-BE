namespace Domain.Shared.ContractTemplates;

using System.Text;
using Domain.Enums;

public static class CarContractTemplateGenerator
{
    public class CarContractTemplate
    {
        public required string ContractNumber { get; set; }
        public DateTimeOffset ContractDate { get; set; }
        public required string OwnerName { get; set; }
        public required string OwnerLicenseNumber { get; set; }
        public required string OwnerAddress { get; set; }
        public required string TechnicianName { get; set; }
        public required string CarManufacturer { get; set; }
        public required string CarLicensePlate { get; set; }
        public required string CarSeat { get; set; }
        public required string CarColor { get; set; }
        public required string CarDescription { get; set; }
        public required decimal CarPrice { get; set; }
        public required string CarTerms { get; set; }
        public required string InspectionResults { get; set; }
        public required Dictionary<InspectionPhotoType, string> InspectionPhotos { get; set; }
        public required string GPSDeviceId { get; set; }
        public required string OwnerSignatureImageUrl { get; set; } = string.Empty;
        public required string TechnicianSignatureImageUrl { get; set; } = string.Empty;
    }

    public static string GenerateFullContractHtml(CarContractTemplate contractTemplate)
    {
        string inspectionPhotoSection = GenerateInspectionPhotoSection(
            contractTemplate.InspectionPhotos
        );

        string standardClauses =
            @$"
            <div class='clause'>
                <strong>Điều 1: Đối tượng hợp đồng</strong>
                <p>
                    Bên A (Chủ xe) đồng ý đăng ký xe của mình lên nền tảng cho thuê xe, cho phép lắp đặt thiết bị GPS của nền tảng, và tuân thủ các điều khoản, quy định của nền tảng.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 2: Quyền và nghĩa vụ của Bên A (Chủ xe)</strong>
                <p>
                    a) Cam kết xe đăng ký là tài sản hợp pháp, có đầy đủ giấy tờ theo quy định của pháp luật.<br/>
                    b) Đảm bảo xe luôn trong tình trạng hoạt động tốt, an toàn cho người thuê.<br/>
                    c) Thực hiện bảo dưỡng, bảo trì xe định kỳ theo khuyến cáo của nhà sản xuất.<br/>
                    d) Chịu trách nhiệm về tình trạng pháp lý và kỹ thuật của xe.<br/>
                    e) Tuân thủ quy định về giá cho thuê và các chính sách của nền tảng.<br/>
                    f) Mua bảo hiểm đầy đủ cho xe theo quy định.<br/>
                    g) Chịu phí dịch vụ theo thỏa thuận với nền tảng.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 3: Quyền và nghĩa vụ của Nền tảng</strong>
                <p>
                    a) Cung cấp dịch vụ đăng tin, quản lý và kết nối với khách thuê.<br/>
                    b) Hỗ trợ giải quyết tranh chấp giữa chủ xe và khách thuê.<br/>
                    c) Bảo mật thông tin của chủ xe theo quy định.<br/>
                    d) Thực hiện kiểm định và đánh giá xe định kỳ.<br/>
                    e) Có quyền tạm ngừng hoặc chấm dứt hợp đồng nếu phát hiện vi phạm.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 4: Điều khoản tài chính</strong>
                <p>
                    a) Giá cho thuê xe: {contractTemplate.CarPrice:N0} VNĐ/ngày.<br/>
                    b) Phí dịch vụ: 10% trên mỗi giao dịch thành công.<br/>
                    c) Thanh toán được thực hiện thông qua nền tảng.<br/>
                    d) Các khoản phí phát sinh sẽ được thông báo và thỏa thuận trước.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 5: Thiết bị GPS và Giám sát</strong>
                <p>
                    a) Chủ xe đồng ý cho phép nền tảng lắp đặt và duy trì thiết bị GPS (Mã thiết bị: {contractTemplate.GPSDeviceId}) trên xe.<br/>
                    b) Thiết bị GPS được sử dụng để:<br/>
                    &nbsp;&nbsp;&nbsp;- Theo dõi vị trí xe trong quá trình cho thuê<br/>
                    &nbsp;&nbsp;&nbsp;- Đảm bảo an toàn và phòng chống trộm cắp<br/>
                    &nbsp;&nbsp;&nbsp;- Hỗ trợ giải quyết tranh chấp nếu có<br/>
                    c) Chủ xe không được tự ý tháo gỡ hoặc làm hỏng thiết bị GPS.<br/>
                    d) Chi phí lắp đặt và bảo trì thiết bị GPS do nền tảng chi trả.<br/>
                    e) Trong trường hợp thiết bị GPS bị hỏng, chủ xe phải thông báo ngay cho nền tảng.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 6: Kiểm định và Đánh giá Xe</strong>
                <p>
                    Vào ngày {contractTemplate.ContractDate:dd/MM/yyyy}, đại diện kiểm định đã thực hiện kiểm tra xe với các hạng mục sau:
                </p>
                <div class='inspection-details'>
                    <p><strong>1. Tình trạng ngoại thất:</strong></p>
                    <ul>
                        <li>Thân xe, sơn xe</li>
                        <li>Đèn chiếu sáng và đèn tín hiệu</li>
                        <li>Lốp xe và mâm xe</li>
                        <li>Kính chắn gió và cửa kính</li>
                    </ul>

                    <p><strong>2. Tình trạng nội thất:</strong></p>
                    <ul>
                        <li>Ghế ngồi và trang bị nội thất</li>
                        <li>Điều hòa và các nút điều khiển</li>
                        <li>Đồng hồ công tơ mét</li>
                        <li>Dây đai an toàn</li>
                    </ul>

                    <p><strong>3. Kiểm tra cơ bản:</strong></p>
                    <ul>
                        <li>Mức nhiên liệu</li>
                        <li>Vị trí đỗ xe và khả năng tiếp cận</li>
                        <li>Chìa khóa và điều khiển từ xa</li>
                        <li>Khoang hành lý</li>
                    </ul>

                    {inspectionPhotoSection}

                    <p><strong>Kết luận:</strong> {contractTemplate.InspectionResults}</p>
                </div>
            </div>

            <div class='clause'>
                <strong>Điều 7: Điều khoản chung</strong>
                <p>
                    a) Hợp đồng có hiệu lực kể từ ngày ký.<br/>
                    b) Mọi thay đổi trong hợp đồng phải được hai bên đồng ý bằng văn bản.<br/>
                    c) Tranh chấp được giải quyết thông qua thương lượng hoặc tòa án có thẩm quyền.<br/>
                    d) Các bên cam kết thực hiện đúng và đầy đủ các điều khoản của hợp đồng.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 8: Chấm dứt hợp đồng</strong>
                <p>
                    a) Hai bên có quyền chấm dứt hợp đồng với thông báo trước 30 ngày.<br/>
                    b) Nền tảng có quyền đơn phương chấm dứt nếu phát hiện vi phạm nghiêm trọng.<br/>
                    c) Việc chấm dứt hợp đồng không ảnh hưởng đến các giao dịch đang thực hiện.
                </p>
            </div>";

        string fullContractTerms = string.IsNullOrWhiteSpace(contractTemplate.CarTerms)
            ? standardClauses
            : $"<div class='custom-terms'>{contractTemplate.CarTerms}</div><br/>{standardClauses}";

        return @$"
            <!DOCTYPE html>
            <html lang='vi'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Hợp Đồng Đăng Ký Xe – {contractTemplate.ContractNumber}</title>
                    <style>
                        /* Use Roboto font from Google Fonts */
                        @import url('https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700&display=swap');
                        @page {{
                            size: A4;
                        }}
                        body {{
                            margin: 0;
                            padding: 0;
                        }}
                        .contract-container {{
                            font-family: 'Roboto', sans-serif;
                            background-color: #f4f4f4;
                            margin: 0;
                            padding: 20px;
                            color: #333;
                            line-height: 1.6;
                        }}
                        .container {{
                            max-width: 900px;
                            background: #fff;
                            margin: auto;
                            padding: 40px;
                            border-radius: 8px;
                            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
                        }}
                        .header {{
                            text-align: center;
                            margin-bottom: 30px;
                        }}
                        .header h1 {{
                            margin: 0;
                            font-size: 28px;
                            color: #000;
                        }}
                        .header p {{
                            margin: 5px 0;
                            font-size: 14px;
                            color: #555;
                        }}
                        .divider {{
                            border: none;
                            border-top: 3px solid #000;
                            margin: 20px auto;
                            width: 50%;
                        }}
                        .section {{
                            margin-bottom: 25px;
                        }}
                        .section-title {{
                            font-size: 18px;
                            font-weight: 500;
                            color: #000;
                            border-bottom: 1px solid #ddd;
                            padding-bottom: 5px;
                            text-transform: uppercase;
                            margin-bottom: 15px;
                        }}
                        .content p {{
                            margin: 8px 0;
                            text-align: justify;
                        }}
                        .clause {{
                            margin-bottom: 15px;
                        }}
                        .clause strong {{
                            display: block;
                            margin-bottom: 5px;
                            color: #000;
                        }}
                        .inspection-details {{
                            margin: 15px 0;
                            padding: 15px;
                            background-color: #f9f9f9;
                            border-radius: 4px;
                        }}
                        .inspection-details ul {{
                            margin: 10px 0;
                            padding-left: 20px;
                        }}
                        .inspection-details li {{
                            margin: 5px 0;
                        }}
                        .inspection-photos {{
                            display: grid;
                            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                            gap: 15px;
                            margin: 15px 0;
                        }}
                        .photo-item {{
                            text-align: center;
                        }}
                        .photo-item img {{
                            max-width: 100%;
                            height: auto;
                            border-radius: 4px;
                            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                        }}
                        .signature-image {{
                            margin-bottom: 10px;
                            text-align: center;
                        }}
                        .signature-image img {{
                            max-width: 100%;
                            height: auto;
                            max-height: 100px;
                        }}
                        .signature-block {{
                            margin-top: 40px;
                            display: flex;
                            justify-content: space-between;
                            flex-wrap: wrap;
                            gap: 20px;
                        }}
                        .signature {{
                            flex: 1;
                            min-width: 200px;
                            text-align: center;
                        }}
                        .signature img.signature-image {{
                            max-width: 100%;
                            height: auto;
                            object-fit: contain;
                        }}
                        .signature p {{
                            border-top: 1px solid #333;
                            padding-top: 10px;
                            margin-top: 40px;
                            font-weight: 500;
                        }}
                        .footer {{
                            text-align: center;
                            font-size: 13px;
                            color: #777;
                            margin-top: 30px;
                        }}

                        /* Responsive styles */
                        @media (max-width: 768px) {{
                            .container {{
                                padding: 20px;
                            }}
                            .signature-block {{
                                flex-direction: column;
                                align-items: center;
                            }}
                            .signature {{
                                width: 100%;
                                margin-bottom: 30px;
                            }}
                            .signature-image img {{
                                max-height: 80px;
                            }}
                            .signature p {{
                                margin-top: 40px;
                            }}
                            .inspection-photos {{
                                grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
                            }}
                        }}

                        @media (max-width: 480px) {{
                            .container {{
                                padding: 15px;
                            }}
                            .header h1 {{
                                font-size: 24px;
                            }}
                            .section-title {{
                                font-size: 16px;
                            }}
                            .signature-image img {{
                                max-height: 60px;
                            }}
                            .inspection-photos {{
                                grid-template-columns: 1fr;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='contract-container'>
                        <!-- Header Section -->
                        <div class='header'>
                            <h1>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</h1>
                            <p>Độc lập – Tự do – Hạnh phúc</p>
                            <hr class='divider'>
                            <h2>HỢP ĐỒNG ĐĂNG KÝ XE</h2>
                        </div>

                        <!-- Contract Information Section -->
                        <div class='section'>
                            <div class='section-title'>Thông tin hợp đồng</div>
                            <div class='content'>
                                <p><strong>Số hợp đồng:</strong> {contractTemplate.ContractNumber}</p>
                                <p><strong>Ngày ký:</strong> {contractTemplate.ContractDate:dd/MM/yyyy}</p>
                            </div>
                        </div>

                        <!-- Party Information Section -->
                        <div class='section'>
                            <div class='section-title'>Thông tin các bên</div>
                            <div class='content'>
                                <p><strong>BÊN A (CHỦ XE):</strong> {contractTemplate.OwnerName}</p>
                                <p>Số giấy phép lái xe: {contractTemplate.OwnerLicenseNumber}</p>
                                <p>Địa chỉ: {contractTemplate.OwnerAddress}</p>
                                <br/>
                                <p><strong>ĐẠI DIỆN KIỂM ĐỊNH:</strong> {contractTemplate.TechnicianName}</p>
                            </div>
                        </div>

                        <!-- Car Information Section -->
                        <div class='section'>
                            <div class='section-title'>Thông tin xe</div>
                            <div class='content'>
                                <p>Nhãn hiệu xe: {contractTemplate.CarManufacturer}</p>
                                <p>Biển số: {contractTemplate.CarLicensePlate}</p>
                                <p>Loại xe: {contractTemplate.CarSeat} chỗ</p>
                                <p>Màu sắc: {contractTemplate.CarColor}</p>
                                <p>Mô tả: {contractTemplate.CarDescription}</p>
                            </div>
                        </div>

                        <!-- Contract Terms Section -->
                        <div class='section'>
                            <div class='section-title'>Điều khoản hợp đồng</div>
                            <div class='content'>
                                {fullContractTerms}
                            </div>
                        </div>

                        <!-- Signature Section -->
                        <div class='section signature-block'>
                            <div class='signature'>
                                {(string.IsNullOrEmpty(contractTemplate.OwnerSignatureImageUrl) ? "" : $@"
                                <div>
                                    <img class='signature-image' src='{contractTemplate.OwnerSignatureImageUrl}' alt='Chữ ký chủ xe' />
                                </div>")}
                                <p>CHỦ XE<br/>{contractTemplate.OwnerName}</p>
                            </div>
                            <div class='signature'>
                                {(string.IsNullOrEmpty(contractTemplate.TechnicianSignatureImageUrl) ? "" : $@"
                                <div>
                                    <img class='signature-image' src='{contractTemplate.TechnicianSignatureImageUrl}' alt='Chữ ký kiểm định viên' />
                                </div>")}
                                <p>ĐẠI DIỆN KIỂM ĐỊNH<br/>{contractTemplate.TechnicianName}</p>
                            </div>
                        </div>

                        <!-- Footer Section -->
                        <div class='footer'>
                            <p>Hợp đồng được lập thành 02 bản có giá trị pháp lý như nhau, mỗi bên giữ 01 bản.</p>
                        </div>
                    </div>
                </body>
            </html>";
    }

    private static string GenerateInspectionPhotoSection(
        Dictionary<InspectionPhotoType, string> photos
    )
    {
        StringBuilder photoSection = new();
        photoSection.Append(
            "<p><strong>4. Hình ảnh kiểm định:</strong></p><div class='inspection-photos'>"
        );

        foreach (var photo in photos)
        {
            string photoTypeName = GetPhotoTypeDisplayName(photo.Key);
            photoSection.Append(
                @$"
                <div class='photo-item'>
                    <p>{photoTypeName}:</p>
                    <img src='{photo.Value}' alt='{photoTypeName}' style='max-width: 200px; margin: 10px 0;'/>
                </div>"
            );
        }

        photoSection.Append("</div>");

        return photoSection.ToString();
    }

    private static string GetPhotoTypeDisplayName(InspectionPhotoType type) =>
        type switch
        {
            InspectionPhotoType.ExteriorCar => "Ngoại thất xe",
            InspectionPhotoType.FuelGauge => "Đồng hồ nhiên liệu",
            InspectionPhotoType.ParkingLocation => "Vị trí đỗ xe",
            InspectionPhotoType.CarKey => "Chìa khóa xe",
            InspectionPhotoType.TrunkSpace => "Khoang hành lý",
            _ => type.ToString(),
        };
}
