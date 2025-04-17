using Markdig;

namespace Domain.Shared.ContractTemplates;

public static class ContractTemplateGenerator
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

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
        public required string PickupAddress { get; set; }
        public int RentalPeriod => (EndDate - StartDate).Days;
        public required string OwnerSignatureImageUrl { get; set; }
        public required string DriverSignatureImageUrl { get; set; }
    }

    public static string GenerateFullContractHtml(ContractTemplate contractTemplate)
    {
        string standardClauses =
            @$"
                <div class='clause'>
                    <strong>Điều 1: Đối tượng hợp đồng</strong>
                    <p>
                        Bên A đồng ý cho Bên B thuê xe với các thông tin như đã mô tả ở phần thông tin xe thuê.
                        Việc giao nhận xe phải được thực hiện tại địa điểm đã thỏa thuận: {contractTemplate.PickupAddress}.
                        Bên B phải sử dụng tính năng 'Bắt đầu chuyến đi' và 'Kết thúc chuyến đi' trên ứng dụng khi giao nhận xe.
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
                        Giá thuê xe: {contractTemplate.RentalPrice} VNĐ/ngày. Giá trên chưa bao gồm các khoản đền bù, VAT và phụ phí (nếu có).
                        <br/>
                        Phí dịch vụ (phí nền tảng): 10% tổng giá trị đơn đặt xe.
                        <br/>
                        Tổng tiền thanh toán = (Giá thuê xe × Số ngày thuê) + Phí dịch vụ (10%)
                        <br/>
                        <br/>
                        Phương thức thanh toán: Bắt buộc thanh toán qua hệ thống bằng mã QR hoặc chuyển khoản.
                        <br/>
                        Thời hạn thanh toán: Trong vòng 12 giờ kể từ khi được chủ xe phê duyệt.
                        <br/>
                        Nếu không thanh toán đúng hạn, đơn đặt xe sẽ tự động hết hạn.
                        <br/>
                        Chỉ được phép bắt đầu chuyến đi sau khi đã thanh toán đầy đủ.
                        <br/>
                        <br/>
                        Ví dụ minh họa:
                        <br/>
                        - Giá thuê xe: {contractTemplate.RentalPrice} VNĐ/ngày
                        <br/>
                        - Số ngày thuê: {contractTemplate.RentalPeriod} ngày
                        <br/>
                        - Tổng giá thuê: {contractTemplate.RentalPrice} × {contractTemplate.RentalPeriod} = {decimal.Parse(contractTemplate.RentalPrice) * contractTemplate.RentalPeriod:N0} VNĐ
                        <br/>
                        - Phí dịch vụ (10%): {decimal.Parse(contractTemplate.RentalPrice) * contractTemplate.RentalPeriod * 0.1m:N0} VNĐ
                        <br/>
                        - Tổng tiền thanh toán: {decimal.Parse(contractTemplate.RentalPrice) * contractTemplate.RentalPeriod * 1.1m:N0} VNĐ
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
                </div>

                <div class='clause'>
                    <strong>Điều 8: Điều khoản về thế chấp và xử lý phạt nguội</strong>
                    <p>
                        a) Trước khi nhận xe, Bên B (Người thuê xe) phải nộp tiền thế chấp tối thiểu là 15.000.000 VNĐ bằng tiền mặt.
                        <br/>
                        b) Số tiền thế chấp sẽ được giữ bởi Bên A (Chủ xe) trong vòng 1 tháng sau khi kết thúc chuyến đi.
                        <br/>
                        c) Trong thời gian 1 tháng, nếu có phạt nguội (ví dụ vi phạm giao thông như vượt đèn đỏ, xử phạt nguội, v.v.),
                        Bên A có quyền trừ số tiền phạt vào số tiền thế chấp. Trong trường hợp tiền thế chấp không đủ,
                        Bên B phải thanh toán khoản chênh lệch trong vòng 5 ngày kể từ khi nhận thông báo.
                        <br/>
                        d) Sau 1 tháng, nếu không còn phạt phát sinh hoặc sau khi đã trừ hết phạt,
                        Bên A bắt buộc hoàn trả lại số tiền thế chấp còn lại cho Bên B.
                        <br/>
                        e) Nếu sau 1 tháng, giấy phạt mới được gửi về, Bên A sẽ báo cáo lại hệ thống,
                        và theo đó Bên B phải thanh toán số tiền phạt phát sinh theo chính sách của nền tảng.
                        <br/>
                        f) Nếu Bên B không thanh toán số tiền phát sinh (bao gồm khoản chênh lệch từ tiền thế chấp hoặc phạt mới)
                        và không cung cấp biên lai xác nhận thanh toán trong thời gian quy định, thì Bên B sẽ không được phép đặt đơn thuê xe mới
                        cho đến khi các khoản nợ được thanh toán đầy đủ và có biên lai chứng từ hợp lệ.
                    </p>
                </div>

                <div class='clause'>
                    <strong>Điều 9: Quy định về vi phạm và hạn chế quyền đặt xe</strong>
                    <p>
                        a) Hệ thống sẽ theo dõi và ghi nhận các hành vi vi phạm, bao gồm số lần hủy đơn và các vi phạm giao thông,
                        từ phía Bên B (Người thuê xe).
                        <br/>
                        b) Nếu Bên B có số lần hủy đơn vượt quá giới hạn (ví dụ: ≥5 lần trong 30 ngày) hoặc có vi phạm lặp lại,
                        hệ thống sẽ áp dụng hạn chế tự động đối với quyền đặt xe của Bên B.
                        <br/>
                        c) Bên B sẽ nhận thông báo về hạn chế này và hướng dẫn liên hệ với bộ phận chăm sóc khách hàng để giải quyết,
                        hoặc nộp đơn khiếu nại nếu có trường hợp đặc biệt.
                    </p>
                </div>

                <div class='clause'>
                    <strong>Điều 10: Giải quyết tranh chấp và khiếu nại</strong>
                    <p>
                        a) Trong trường hợp có tranh chấp liên quan đến việc trả xe, phạt nguội, hoặc thiệt hại hiệu quả, cả hai bên
                        có trách nhiệm cung cấp đầy đủ bằng chứng như hình ảnh kiểm định, biên lai thanh toán, báo cáo vi phạm, v.v.
                        <br/>
                        b) Hệ thống sẽ chuyển thông tin tranh chấp tới Ban quản trị để xem xét và đưa ra quyết định cuối cùng,
                        dựa trên các chính sách đã được công bố.
                        <br/>
                        c) Kết quả giải quyết tranh chấp sẽ được thông báo tới cả Bên A và Bên B thông qua hệ thống và email.
                        <br/>
                        d) Nếu không đạt được thỏa thuận, tranh chấp sẽ được giải quyết tại Tòa án có thẩm quyền.
                    </p>
                </div>

                <div class='clause'>
                    <strong>Điều 11: Quy định về vệ sinh và nhiên liệu xe</strong>
                    <p>
                        a) Điều kiện giao xe:
                        <br/>
                        - Bên A (Chủ xe) có trách nhiệm bàn giao xe trong tình trạng sạch sẽ, vệ sinh cả nội thất và ngoại thất.
                        <br/>
                        - Mức nhiên liệu khi giao xe phải đạt tối thiểu 90% (hoặc đồng hồ báo F - Full).
                        <br/>
                        - Hai bên sẽ chụp ảnh và ghi nhận tình trạng vệ sinh, mức nhiên liệu vào biên bản bàn giao xe.
                        <br/>
                        <br/>
                        b) Điều kiện trả xe:
                        <br/>
                        - Bên B (Người thuê) phải trả xe trong tình trạng sạch sẽ như khi nhận xe.
                        <br/>
                        - Mức nhiên liệu khi trả xe phải tương đương với mức khi nhận xe (tối thiểu 90%).
                        <br/>
                        <br/>
                        c) Phí phạt:
                        <br/>
                        - Nếu xe được trả về trong tình trạng bẩn (nội thất hoặc ngoại thất):
                          + Phí vệ sinh cơ bản: 200.000 VNĐ
                          + Phí vệ sinh đặc biệt (nếu có vết bẩn khó làm sạch): 500.000 VNĐ
                        <br/>
                        - Nếu mức nhiên liệu khi trả xe thấp hơn quy định:
                          + Phí bù nhiên liệu: Giá nhiên liệu hiện hành × (Mức nhiên liệu thiếu)
                          + Phí phạt bổ sung: 200.000 VNĐ
                        <br/>
                        <br/>
                        d) Quy trình xử lý:
                        <br/>
                        - Khi trả xe, hai bên sẽ kiểm tra và ghi nhận tình trạng vệ sinh, mức nhiên liệu vào biên bản.
                        <br/>
                        - Nếu phát hiện vi phạm, Bên A có quyền giữ lại một phần tiền đặt cọc để chi trả các khoản phí phạt.
                        <br/>
                        - Bên B có quyền tự xử lý vệ sinh xe hoặc đổ xăng bổ sung trong vòng 2 giờ kể từ thời điểm trả xe
                          để tránh các khoản phí phạt.
                    </p>
                </div>

                <div class='clause'>
                    <strong>Điều 12: Chính sách hủy chuyến và hoàn tiền</strong>
                    <p>
                        a) Quy định hủy chuyến từ phía Bên B (Người thuê):
                        <br/>
                        - Hủy trước 7 ngày so với ngày bắt đầu chuyến đi: Hoàn 100% tổng tiền thuê
                        <br/>
                        - Hủy trước 5-7 ngày: Hoàn 50% tổng tiền thuê
                        <br/>
                        - Hủy trước 3-5 ngày: Hoàn 30% tổng tiền thuê
                        <br/>
                        - Hủy trong vòng 3 ngày trước chuyến đi: Không hoàn tiền
                        <br/>
                        <br/>
                        b) Quy định hủy chuyến từ phía Bên A (Chủ xe):
                        <br/>
                        - Hủy trong vòng 24 giờ trước chuyến đi: Phạt 50% tổng giá trị chuyến đi
                        <br/>
                        - Hủy trong vòng 1-3 ngày trước chuyến đi: Phạt 30% tổng giá trị chuyến đi
                        <br/>
                        - Hủy trong vòng 3-7 ngày trước chuyến đi: Phạt 10% tổng giá trị chuyến đi
                        <br/>
                        - Nếu chủ xe hủy chuyến, khách thuê sẽ được hoàn 100% số tiền đã thanh toán
                        <br/>
                        <br/>
                        c) Giới hạn hủy chuyến:
                        <br/>
                        - Bên B chỉ được phép hủy tối đa 5 chuyến trong vòng 30 ngày
                        <br/>
                        - Nếu vượt quá giới hạn này, tài khoản sẽ bị hạn chế đặt xe trong thời gian nhất định
                        <br/>
                        <br/>
                        d) Quy trình hoàn tiền:
                        <br/>
                        - Số tiền hoàn trả sẽ được chuyển vào ví điện tử của người thuê
                        <br/>
                        - Người thuê có thể đặt lệnh rút tiền để rút về tài khoản ngân hàng
                        <br/>
                        - Thời gian xử lý hoàn tiền: trong vòng 24 giờ sau khi hủy chuyến
                        <br/>
                        - Tiền hoàn trả sẽ được chia theo tỷ lệ: 90% từ chủ xe và 10% từ phí quản trị
                    </p>
                </div>";

        string renderedCarTerms = string.IsNullOrWhiteSpace(contractTemplate.CarTerms)
            ? string.Empty
            : Markdown.ToHtml(contractTemplate.CarTerms, Pipeline);

        string fullContractTerms = string.IsNullOrWhiteSpace(contractTemplate.CarTerms)
            ? standardClauses
            : $@"<div class='custom-terms'>
                    <div class='markdown-content'>
                        {renderedCarTerms}
                    </div>
                </div>
                <br/>
                {standardClauses}";

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
                                @page {{
                                    size: A4;
                                    margin: 2cm;
                                }}
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
                                .markdown-content {{
                                    padding: 15px;
                                    background-color: #f8f9fa;
                                    border-radius: 4px;
                                }}
                                .markdown-content h1,
                                .markdown-content h2,
                                .markdown-content h3,
                                .markdown-content h4,
                                .markdown-content h5,
                                .markdown-content h6 {{
                                    margin-top: 1em;
                                    margin-bottom: 0.5em;
                                    color: #2c3e50;
                                }}
                                .markdown-content p {{
                                    margin: 0.5em 0;
                                }}
                                .markdown-content ul,
                                .markdown-content ol {{
                                    padding-left: 1.5em;
                                    margin: 0.5em 0;
                                }}
                                .markdown-content code {{
                                    background-color: #f1f1f1;
                                    padding: 0.2em 0.4em;
                                    border-radius: 3px;
                                    font-family: monospace;
                                }}
                                .markdown-content blockquote {{
                                    border-left: 4px solid #ddd;
                                    padding-left: 1em;
                                    margin: 1em 0;
                                    color: #666;
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
                                        {(string.IsNullOrEmpty(contractTemplate.OwnerSignatureImageUrl) ? "" : $@"
                                        <div class='signature-image'>
                                            <img src='{contractTemplate.OwnerSignatureImageUrl}' alt='Chữ ký chủ xe' />
                                        </div>")}
                                        <p>BÊN CHO THUÊ (Bên A)<br/>{contractTemplate.OwnerName}</p>
                                    </div>
                                    <div class='signature'>
                                        {(string.IsNullOrEmpty(contractTemplate.DriverSignatureImageUrl) ? "" : $@"
                                        <div class='signature-image'>
                                            <img src='{contractTemplate.DriverSignatureImageUrl}' alt='Chữ ký bên thuê xe' />
                                        </div>")}
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
