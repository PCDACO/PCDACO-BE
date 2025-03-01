namespace UseCases.Services.PdfService;

public interface IPdfService
{
    byte[] ConvertHtmlToPdf(string html);
}
