using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

using UseCases.Abstractions;

namespace Infrastructure.Medias;

public class CloudinaryServices(Cloudinary cloudinary) : ICloudinaryServices
{
    public async Task<string> UploadCarImageAsync(string name, Stream image, CancellationToken cancellationToken = default)

    {
        ImageUploadParams uploadParams = new()
        {
            File = new FileDescription(name, image),
            Folder = "car",
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = true
        };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(uploadParams, cancellationToken);
        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadReportImageAsync(string name, Stream image, CancellationToken cancellationToken = default)
    {
        ImageUploadParams uploadParams = new()
        {
            File = new FileDescription(name, image),
            Folder = "report",
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = true
        };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(uploadParams, cancellationToken);
        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadUserImageAsync(string name, Stream image, CancellationToken cancellationToken = default)
    {
        ImageUploadParams uploadParams = new()
        {
            File = new FileDescription(name, image),
            Folder = "user",
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = true
        };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(uploadParams, cancellationToken);
        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }

    public async Task<string> UploadFeedbackImageAsync(string name, Stream image, CancellationToken cancellationToken = default)
    {
        ImageUploadParams uploadParams = new()
        {
            File = new FileDescription(name, image),
            Folder = "feedback",
            UseFilename = true,
            UniqueFilename = false,
            Overwrite = true
        };
        ImageUploadResult uploadResult = await cloudinary.UploadAsync(uploadParams, cancellationToken);
        return uploadResult.Url.AbsoluteUri ?? throw new Exception("Error uploading image");
    }
}