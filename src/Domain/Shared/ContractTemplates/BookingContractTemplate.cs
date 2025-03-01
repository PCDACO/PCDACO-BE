namespace Domain.Shared.ContractTemplates;

public static class ContractTemplateGenerator
{
    public class ContractTemplate
    {
        public required string ContractNumber { get; set; }
        public DateTimeOffset ContractDate { get; set; }
        public required string OwnerName { get; set; }
        public required string OwnerLicenseNumber { get; set; }
        public required string OwnerAddress { get; set; }
        public required string DriverName { get; set; }
        public required string DriverLicenseNumber { get; set; }
        public required string DriverAddress { get; set; }
        public required string CarManufacturer { get; set; }
        public required string CarLicensePlate { get; set; }
        public required string CarSeat { get; set; }
        public required string CarColor { get; set; }
        public required string CarDetail { get; set; }
        public required string CarTerms { get; set; }
        public required string RentalPrice { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public int RentalPeriod => (EndDate - StartDate).Days;
    }

    public static string GenerateFullContractHtml(ContractTemplate contractTemplate)
    {
        string standardClauses =
            @$"
                <div class='clause'>
                    <strong>Điều 1: Đối tượng hợp đồng</strong>
                    <p>
                        Bên A đồng ý cho Bên B thuê xe với các thông tin như đã mô tả ở phần thông tin xe thuê.
                    </p>
                </div>
                <div class='clause'>
                    <strong>Điều 2: Thời hạn hợp đồng</strong>
                    <p>
                        Hợp đồng có hiệu lực từ {contractTemplate.StartDate:dd/MM/yyyy} đến {contractTemplate.EndDate:dd/MM/yyyy}. Thời hạn thuê: {contractTemplate.RentalPeriod} ngày.
                    </p>
                </div>
                <div class='clause'>
                    <strong>Điều 3: Giá thuê và phương thức thanh toán</strong>
                    <p>
                        Giá thuê xe: {contractTemplate.RentalPrice} VNĐ. Giá trên chưa bao gồm các khoản đền bù, VAT và phụ phí (nếu có).
                        Phương thức thanh toán được thỏa thuận giữa hai bên.
                    </p>
                </div>
                <div class='clause'>
                    <strong>Điều 4: Quyền và nghĩa vụ của Bên A (Chủ xe)</strong>
                    <p>
                        a) Bên A cam kết bàn giao xe đúng như mô tả và đảm bảo xe có đầy đủ giấy tờ hợp pháp. <br/>
                        b) Bên A có trách nhiệm bảo trì, bảo dưỡng xe định kỳ, đảm bảo xe luôn trong tình trạng sử dụng tốt. <br/>
                        c) Nếu xảy ra tranh chấp về quyền sở hữu hoặc sử dụng xe, Bên A chịu trách nhiệm giải quyết theo pháp luật.
                    </p>
                </div>
                <div class='clause'>
                    <strong>Điều 5: Quyền và nghĩa vụ của Bên B (Người thuê xe)</strong>
                    <p>
                        a) Bên B có trách nhiệm sử dụng xe đúng mục đích, bảo quản xe cẩn thận và không được tự ý thay đổi cấu trúc xe nếu chưa được Bên A đồng ý.<br/>
                        b) Bên B cam kết thanh toán đầy đủ số tiền thuê theo thỏa thuận. <br/>
                        c) Trong trường hợp Bên B vi phạm quy định giao thông, gây ra tai nạn, hoặc không tuân thủ các điều khoản bảo quản xe, Bên B phải chịu phạt và bồi thường thiệt hại cho Bên A theo quy định của pháp luật.
                    </p>
                </div>
                <div class='clause'>
                    <strong>Điều 6: Phạt vi phạm và xử lý sự cố</strong>
                    <p>
                        a) Nếu trong quá trình thuê xe, Bên B bị xử phạt vì vi phạm giao thông, Bên B chịu trách nhiệm thanh toán phạt và bồi thường thiệt hại liên quan. <br/>
                        b) Nếu xe bị hư hỏng do lỗi của Bên B mà không thông báo kịp thời cho Bên A, Bên B sẽ bị phạt theo mức đã thỏa thuận và phải bồi thường thiệt hại. <br/>
                        c) Nếu sự cố không được thông báo đúng thời hạn, Bên A có quyền đơn phương chấm dứt hợp đồng.
                    </p>
                </div>
                <div class='clause'>
                    <strong>Điều 7: Điều khoản chung</strong>
                    <p>
                        a) Hai bên cam kết thực hiện đầy đủ các điều khoản của hợp đồng này. <br/>
                        b) Mọi tranh chấp phát sinh từ hợp đồng sẽ được thương lượng giải quyết; nếu không đạt được thỏa thuận, tranh chấp sẽ được giải quyết tại Tòa án có thẩm quyền. <br/>
                        c) Hợp đồng được lập thành 02 bản có giá trị pháp lý như nhau, mỗi bên giữ 01 bản.
                    </p>
                </div>";

        string fullContractTerms = string.IsNullOrWhiteSpace(contractTemplate.CarTerms)
            ? standardClauses
            : $"<div class='custom-terms'>{contractTemplate.CarTerms}</div><br/>{standardClauses}";

        // Return the complete HTML template.
        return @$"
                    <!DOCTYPE html>
                    <html lang='vi'>
                        <head>
                            <meta charset='UTF-8'>
                            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                            <title>Hợp Đồng Thuê Xe – {contractTemplate.ContractNumber}</title>
                            <style>
                                /* Use Roboto font from Google Fonts */
                                @import url('https://fonts.googleapis.com/css2?family=Roboto:wght@400;500;700&display=swap');

                                body {{
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
                                .signature-block {{
                                margin-top: 40px;
                                display: flex;
                                justify-content: space-between;
                                }}
                                .signature {{
                                width: 40%;
                                text-align: center;
                                }}
                                .signature p {{
                                border-top: 1px solid #333;
                                padding-top: 10px;
                                margin-top: 60px;
                                font-weight: 500;
                                }}
                                .footer {{
                                text-align: center;
                                font-size: 13px;
                                color: #777;
                                margin-top: 30px;
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
                                    <h2>HỢP ĐỒNG THUÊ XE</h2>
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
                                        <p><strong>BÊN CHO THUÊ (Bên A):</strong> {contractTemplate.OwnerName}</p>
                                        <p>Số giấy phép lái xe: {contractTemplate.OwnerLicenseNumber}</p>
                                        <p>Địa chỉ: {contractTemplate.OwnerAddress}</p>
                                        <br/>
                                        <p><strong>BÊN THUÊ (Bên B):</strong> {contractTemplate.DriverName}</p>
                                        <p>Số giấy phép lái xe: {contractTemplate.DriverLicenseNumber}</p>
                                        <p>Địa chỉ: {contractTemplate.DriverAddress}</p>
                                    </div>
                                </div>

                                <!-- Car Details Section -->
                                <div class='section'>
                                    <div class='section-title'>Thông tin xe thuê</div>
                                    <div class='content'>
                                        <p>Nhãn hiệu xe: {contractTemplate.CarManufacturer} – Biển số: {contractTemplate.CarLicensePlate}</p>
                                        <p>Loại xe: {contractTemplate.CarSeat} chỗ, Màu sắc: {contractTemplate.CarColor}</p>
                                        <p>Mô tả: {contractTemplate.CarDetail}</p>
                                    </div>
                                </div>

                                <!-- Rental Terms Section -->
                                <div class='section'>
                                    <div class='section-title'>Điều khoản thuê xe</div>
                                    <div class='content'>
                                        {fullContractTerms}
                                    </div>
                                </div>

                                <!-- Signature Section -->
                                <div class='section signature-block'>
                                    <div class='signature'>
                                        <p>BÊN CHO THUÊ (Bên A)<br/>{contractTemplate.OwnerName}</p>
                                    </div>
                                    <div class='signature'>
                                         <p>BÊN THUÊ (Bên B)<br/>{contractTemplate.DriverName}</p>
                                    </div>
                                </div>

                                <!-- Footer Section -->
                                <div class='footer'>
                                <p>Hợp đồng được lập thành 02 bản có giá trị pháp lý như nhau, mỗi bên giữ 01 bản.</p>
                                </div>
                            </div>
                        </body>
                    </html>
                    ";
    }
}
