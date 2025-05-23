namespace UseCases.Abstractions;

public interface ICloudinaryServices
{
    Task<string> UploadCarImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadPaperImageAsync(
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
    Task<string> UploadAmenityIconAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadBookingInspectionImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadTransactionProofAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadManufacturerLogoAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadCompensationPaidImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
    Task<string> UploadInspectionSchedulePhotosAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    );
}
