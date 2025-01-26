using Domain.Entities;

namespace Persistance.Bogus;

public class ImageTypeGenerator
{
    private static readonly string[] _imageTypes = ["Car", "Paper"];
    public static ImageType[] Execute()
    {
        return [.. _imageTypes.Select(status => {
            return new ImageType()
            {
                Name = status,
            };
        })];
    }
}