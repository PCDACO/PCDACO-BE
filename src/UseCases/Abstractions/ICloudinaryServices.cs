namespace UseCases.Abstractions;

public interface ICloudinaryServices
{
    Task<string> UploadCarImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadReportImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadUserImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadFeedbackImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadDriverLicenseImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
}
