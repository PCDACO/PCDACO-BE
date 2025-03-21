
using Ardalis.Result;

using Domain.Constants;
using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Commands;

public sealed class UploadPaperImages
{
    public record Command(
        Guid CarId,
        ImageFile[] PaperImages
    ) : IRequest<Result<Response>>;

    public class ImageFile
    {
        public required Stream Content { get; set; }
        public required string FileName { get; set; }
    }

    public record Response(ImageDetail[] Images)
    {
        public static Response FromEntity(Guid carId, ImageCar[] files)
        {
            return new Response(
                [.. files.Select(f => new ImageDetail(carId, f.Url, f.Name))]
            );
        }
    };

    public record ImageDetail(
        Guid Id,
        string Url,
        string Name
    );
    public class Handler(
        IAppDBContext context,
        ICloudinaryServices cloudinaryServices,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var car = await context.Cars
                .AsNoTracking()
                .Include(c => c.ImageCars).ThenInclude(ic => ic.Type)
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car is null)
                return Result.NotFound("Không tìm thấy xe");
            if (currentUser.User!.Id != car.OwnerId)
                return Result.Forbidden("Không có quyền cập nhật ảnh của xe này");
            // Check if type images is exist
            ImageType? paperImageType = await context.ImageTypes
                .AsNoTracking()
                .Where(it => EF.Functions.ILike(it.Name, "%paper%"))
                .Where(it => !it.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
            if (paperImageType is null) return Result.Error("Không tìm thấy loại hình ảnh đang cần");
            if (car.ImageCars.Where(ic => ic.Type.Name.Equals("paper", StringComparison.CurrentCultureIgnoreCase)).Any())
            {
                await context.ImageCars
                    .Where(c => c.CarId == request.CarId)
                    .Where(it => EF.Functions.ILike(it.Type.Name, "%paper%"))
                    .ExecuteDeleteAsync(cancellationToken);
            }
            List<Task<string>> carTasks = [];
            int carIndex = 0;
            foreach (var image in request.PaperImages)
            {
                carTasks.Add(cloudinaryServices.UploadCarImageAsync($"Car-{car.Id}-PaperImage-{++carIndex}", image.Content, cancellationToken));
            }
            string[] paperImageUrls = await Task.WhenAll(carTasks);
            ImageCar[] images =
            [
                .. Enumerable.Range(0, paperImageUrls.Length)
                    .Select(i => new ImageCar
                    {
                        CarId = car.Id,
                        Url = paperImageUrls[i],
                        TypeId = paperImageType.Id,
                        Name = request.PaperImages[i].FileName
                    }),
            ];
            // Save new images
            await context.ImageCars.AddRangeAsync(images, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(Response.FromEntity(car.Id, images), ResponseMessages.Updated);
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CarId)
                .NotEmpty().WithMessage("Phải chọn xe cần cập nhật !");
            RuleFor(x => x.PaperImages)
                .NotEmpty().WithMessage("Phải chọn ảnh !");
        }
    }
}