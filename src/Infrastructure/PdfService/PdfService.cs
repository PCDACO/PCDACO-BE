using DinkToPdf;
using UseCases.Services.PdfService;

namespace Infrastructure.PdfService;

public class PdfService : IPdfService
{
    private readonly SynchronizedConverter _converter;

    public PdfService()
    {
        _converter = new SynchronizedConverter(new PdfTools());
    }

    public byte[] ConvertHtmlToPdf(string html)
    {
        var document = new HtmlToPdfDocument()
        {
            GlobalSettings = new GlobalSettings()
            {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait,
                DocumentTitle = "Hợp Đồng Thuê Xe"
            },
            Objects =
            {
                new ObjectSettings()
                {
                    HtmlContent = html,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
        };

        return _converter.Convert(document);
    }
}
