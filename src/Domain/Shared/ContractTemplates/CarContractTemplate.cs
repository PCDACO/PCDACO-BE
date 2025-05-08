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
                    a) Cam kết xe đăng ký là tài sản hợp pháp, có đầy đủ giấy tờ theo quy định của pháp luật và cung cấp cho Nền tảng khi có yêu cầu.<br/>
                    b) Đảm bảo xe luôn trong tình trạng hoạt động tốt, an toàn cho người thuê.<br/>
                    c) Thực hiện bảo dưỡng, bảo trì xe định kỳ theo khuyến cáo của nhà sản xuất và cung cấp bằng chứng khi Nền tảng yêu cầu.<br/>
                    d) Chịu trách nhiệm về tình trạng pháp lý và kỹ thuật của xe.<br/>
                    e) Tuân thủ quy định về giá cho thuê và các chính sách của nền tảng.<br/>
                    f) Mua bảo hiểm trách nhiệm dân sự bắt buộc và khuyến khích mua bảo hiểm vật chất thân vỏ còn hiệu lực cho xe theo quy định pháp luật.<br/>
                    g) Chịu phí dịch vụ theo thỏa thuận với nền tảng.<br/>
                    h) Thông báo kịp thời cho Nền tảng về bất kỳ thay đổi nào liên quan đến tình trạng pháp lý, kỹ thuật của xe (VD: sửa chữa lớn, hết hạn đăng kiểm, tai nạn...).
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 3: Quyền và nghĩa vụ của Nền tảng</strong>
                <p>
                    a) Cung cấp dịch vụ đăng tin, quản lý và kết nối với khách thuê.<br/>
                    b) Hỗ trợ giải quyết tranh chấp giữa chủ xe và khách thuê.<br/>
                    c) Bảo mật thông tin của chủ xe theo quy định.<br/>
                    d) Thực hiện kiểm định và đánh giá xe định kỳ.<br/>
                    e) Có quyền tạm ngừng hoặc chấm dứt hợp đồng nếu phát hiện vi phạm.<br/>
                    f) Có quyền yêu cầu Bên A cung cấp các giấy tờ liên quan đến xe (đăng ký, đăng kiểm, bảo hiểm) và bằng chứng bảo dưỡng.
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
                    &nbsp;&nbsp;&nbsp;- Thu thập dữ liệu hành trình phục vụ quản lý, vận hành và cải thiện dịch vụ (đảm bảo tuân thủ quy định bảo mật dữ liệu). <br/>
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

                    <p><strong>Kết luận:</strong> {contractTemplate.InspectionResults}</p>
                </div>
            </div>

            <div class='clause'>
                <strong>Điều 7: Đăng kiểm xe</strong>
                <p>
                    a) Bên A (Chủ xe) có trách nhiệm đảm bảo xe luôn có giấy chứng nhận đăng kiểm còn hiệu lực theo quy định của pháp luật Việt Nam trong suốt thời gian tham gia nền tảng.<br/>
                    b) Bên A phải cung cấp bản sao giấy chứng nhận đăng kiểm hợp lệ cho Nền tảng khi đăng ký và khi có yêu cầu.<br/>
                    c) Mọi chi phí liên quan đến việc đăng kiểm xe do Bên A chịu trách nhiệm.<br/>
                    d) Bên A phải thông báo ngay lập tức cho Nền tảng nếu giấy chứng nhận đăng kiểm bị thu hồi hoặc xe không còn đủ điều kiện lưu hành an toàn.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 8: Bảo hiểm và Xử lý Thiệt hại</strong>
                <p>
                    a) Bên A chịu trách nhiệm mua và duy trì bảo hiểm TNDS bắt buộc và bảo hiểm vật chất thân vỏ (khuyến nghị) cho xe trong suốt thời gian hợp đồng.<br/>
                    b) Khi xảy ra sự kiện bảo hiểm (tai nạn, hư hỏng, mất cắp...), Bên A và/hoặc người thuê (tùy tình huống) có trách nhiệm thông báo ngay cho Nền tảng và công ty bảo hiểm.<br/>
                    c) Quy trình xử lý bồi thường bảo hiểm sẽ tuân theo quy định của công ty bảo hiểm và thỏa thuận giữa Bên A và Nền tảng (nếu có chính sách hỗ trợ riêng).<br/>
                    d) Trách nhiệm đối với phần miễn thường và các chi phí không được bảo hiểm chi trả sẽ được xác định dựa trên nguyên nhân gây ra thiệt hại và theo chính sách giải quyết của Nền tảng.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 9: Các hành vi bị cấm</strong>
                <p>
                    Xe đăng ký trên Nền tảng không được sử dụng cho các mục đích sau:<br/>
                    &nbsp;&nbsp;&nbsp;- Chở hàng cấm, hàng hóa bất hợp pháp.<br/>
                    &nbsp;&nbsp;&nbsp;- Tham gia đua xe, lái thử tốc độ hoặc các hoạt động nguy hiểm tương tự.<br/>
                    &nbsp;&nbsp;&nbsp;- Sử dụng vào mục đích phạm tội hoặc vi phạm pháp luật.<br/>
                    &nbsp;&nbsp;&nbsp;- Cho thuê lại hoặc giao xe cho người khác không có trong hợp đồng thuê xe qua Nền tảng.<br/>
                    &nbsp;&nbsp;&nbsp;- Lái xe khi đã sử dụng rượu bia hoặc chất kích thích.<br/>
                    Vi phạm các quy định này có thể dẫn đến việc chấm dứt hợp đồng ngay lập tức và Bên A có thể phải chịu trách nhiệm pháp lý liên quan.
                </p>
            </div>


            <div class='clause'>
                <strong>Điều 10: Điều khoản chung</strong>
                <p>
                    a) Hợp đồng có hiệu lực kể từ ngày ký.<br/>
                    b) Mọi thay đổi trong hợp đồng phải được hai bên đồng ý bằng văn bản.<br/>
                    c) Tranh chấp được giải quyết thông qua thương lượng hoặc tòa án có thẩm quyền.<br/>
                    d) Các bên cam kết thực hiện đúng và đầy đủ các điều khoản của hợp đồng.
                </p>
            </div>

            <div class='clause'>
                <strong>Điều 11: Chấm dứt hợp đồng</strong>
                <p>
                    a) Hai bên có quyền chấm dứt hợp đồng với thông báo trước 30 ngày.<br/>
                    b) Nền tảng có quyền đơn phương chấm dứt ngay lập tức nếu phát hiện Bên A vi phạm nghiêm trọng các điều khoản hợp đồng, bao gồm nhưng không giới hạn: xe không có đăng kiểm hợp lệ, giấy tờ xe không hợp pháp, xe bị sử dụng vào mục đích cấm, không hợp tác xử lý các vấn đề phát sinh.<br/>
                    c) Việc chấm dứt hợp đồng không ảnh hưởng đến các giao dịch đang thực hiện hoặc các nghĩa vụ tài chính còn tồn đọng.<br/>
                    d) Khi hợp đồng chấm dứt, Bên A có trách nhiệm phối hợp với Nền tảng để tháo dỡ thiết bị GPS (nếu có) và hoàn tất các nghĩa vụ còn lại.
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
                        .footer {{
                            text-align: center;
                            font-size: 13px;
                            color: #777;
                            margin-top: 30px;
                        }}
                        .photo-item img {{
                            max-width: 100%;
                            height: auto;
                            border-radius: 4px;
                            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                        }}
                        .signature-block {{
                            margin-top: 40px;
                            display: flex;
                            justify-content: space-between;
                            padding: 20px 40px;
                        }}
                        .signature {{
                            flex: 1;
                            max-width: 45%;
                            text-align: center;
                            display: flex;
                            flex-direction: column;
                            align-items: center;
                        }}
                        .signature-image {{
                            width: 200px;
                            height: 100px;
                            margin-bottom: 20px;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                        }}
                        .signature-image img {{
                            max-width: 100%;
                            max-height: 100%;
                            object-fit: contain;
                        }}
                        .signature p {{
                            border-top: 1px solid #333;
                            padding-top: 10px;
                            margin-top: 10px;
                            font-weight: 500;
                            width: 100%;
                        }}

                        /* Responsive styles */
                        @media (max-width: 768px) {{
                            .container {{
                                padding: 20px;
                            }}
                            .signature-block {{
                                flex-direction: column;
                                align-items: center;
                                padding: 20px;
                            }}
                            .signature {{
                                max-width: 100%;
                                width: 100%;
                                margin-bottom: 30px;
                            }}
                            .signature:last-child {{
                                margin-bottom: 0;
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
                    <div class='container'>
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
                        <div class='section'>
                            <div class='signature-block'>
                                <div class='signature'>
                                    {(string.IsNullOrEmpty(contractTemplate.OwnerSignatureImageUrl) ? "" : $@"
                                    <div class='signature-image'>
                                        <img src='{contractTemplate.OwnerSignatureImageUrl}' alt='Chữ ký chủ xe' />
                                    </div>")}
                                    <p>CHỦ XE<br/>{contractTemplate.OwnerName}</p>
                                </div>
                                <div class='signature'>
                                    {(string.IsNullOrEmpty(contractTemplate.TechnicianSignatureImageUrl) ? "" : $@"
                                    <div class='signature-image'>
                                        <img src='{contractTemplate.TechnicianSignatureImageUrl}' alt='Chữ ký kiểm định viên' />
                                    </div>")}
                                    <p>ĐẠI DIỆN KIỂM ĐỊNH<br/>{contractTemplate.TechnicianName}</p>
                                </div>
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
}
