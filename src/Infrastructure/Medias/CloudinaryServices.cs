using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using UseCases.Abstractions;

namespace Infrastructure.Medias;

public class CloudinaryServices(Cloudinary cloudinary) : ICloudinaryServices
{
    public async Task<string> UploadCarImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "car",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(
            uploadParams,
            cancellationToken
        );
        Console.WriteLine("upload" + uploadResult.Url);
        return uploadResult.Url?.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadPaperImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "car-paper",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(
            uploadParams,
            cancellationToken
        );
        return uploadResult.Url?.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadReportImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "report",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(
            uploadParams,
            cancellationToken
        );
        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadUserImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "user",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(
            uploadParams,
            cancellationToken
        );
        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadFeedbackImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "feedback",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(
            uploadParams,
            cancellationToken
        );
        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadDriverLicenseImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        if (image == null)
        {
            throw new ArgumentNullException(nameof(image));
        }

        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "driver-licenses",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };

        try
        {
            ImageUploadResult uploadResult = await cloudinary.UploadAsync(
                uploadParams,
                cancellationToken
            );
            return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error uploading driver license image: {ex.Message}");
        }
    }

    public async Task<string> UploadAmenityIconAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "amenity-icon",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(
            uploadParams,
            cancellationToken
        );
        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadBookingInspectionImageAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "booking-inspection",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };

        ImageUploadResult uploadResult = await cloudinary.UploadAsync(
            uploadParams,
            cancellationToken
        );

        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadTransactionProofAsync(
        string name,
        Stream image,
        CancellationToken cancellationToken = default
    )
    {
        ImageUploadParams uploadParams =
            new()
            {
                File = new FileDescription(name, image),
                Folder = "transaction-proofs",
                UseFilename = true,
                UniqueFilename = false,
                Overwrite = true,
            };

        ImageUploadResult uploadResult = await cloudinary.UploadAsync(
            uploadParams,
            cancellationToken
        );

        return uploadResult.Url.AbsoluteUri
            ?? throw new Exception("Error uploading transaction proof");
    }
}
