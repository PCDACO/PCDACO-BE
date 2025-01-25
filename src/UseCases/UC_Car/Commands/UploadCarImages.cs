
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Car.Commands;

public sealed class UploadCarImages
{
    public record Command(
        Guid CarId,
        Stream[] CarImages,
        Stream[] PaperImages
    ) : IRequest<Result<Response>>;

    public record Response(ImageDetail[] Images)
    {
        public static Response FromEntity(Car car)
        {
            return new Response(
                [.. car.ImageCars.Select(i => new ImageDetail(i.Id, i.Url))]
            );
        }
    };

    public record ImageDetail(
        Guid Id,
        string Url
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
                .Include(c => c.ImageCars)
                .Include(c => c.Owner)
                .FirstOrDefaultAsync(c => c.Id == request.CarId, cancellationToken);

            if (car is null)
                return Result.NotFound("Không tìm thấy xe");

            if (currentUser.User!.Id != car.OwnerId)
                return Result.Forbidden("Không có quyền cập nhật ảnh của xe này");
            // Delete old images
            await context.ImageCars
                .Where(i => i.CarId == car.Id)
                .ExecuteDeleteAsync(cancellationToken);
            // Upload new images
            // Check if type images is exist
            ImageType? carImageType = await context.ImageTypes
                .AsNoTracking()
                .Where(it => it.Name.Contains("car", StringComparison.OrdinalIgnoreCase))
                .Where(it => !it.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
            if (carImageType is null) return Result.Error("Không tìm thấy loại hình ảnh đang cần");
            ImageType? paperImageType = await context.ImageTypes
                .AsNoTracking()
                .Where(it => it.Name.Contains("paper", StringComparison.OrdinalIgnoreCase))
                .Where(it => !it.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
            if (carImageType is null) return Result.Error("Không tìm thấy loại hình ảnh đang cần");
            List<Task<string>> carTasks = [];
            List<Task<string>> paperTasks = [];
            int carIndex = 0;
            int paperIndex = 0;
            foreach (var image in request.CarImages)
            {
                carTasks.Add(cloudinaryServices.UploadCarImageAsync($"Car-{car.Id}-Image-{++carIndex}", image, cancellationToken));
            }
            foreach (var image in request.CarImages)
            {
                paperTasks.Add(cloudinaryServices.UploadCarImageAsync($"Car-{car.Id}-Image-{++paperIndex}-Paper", image, cancellationToken));
            }
            string[] carImageUrls = await Task.WhenAll(carTasks);
            string[] paperImageUrls = await Task.WhenAll(paperTasks);
            ImageCar[] images =
            [
                .. carImageUrls
                    .Select(url => new ImageCar
                    {
                        CarId = car.Id,
                        Url = url,
                        TypeId = carImageType.Id
                    }),
                .. paperImageUrls.Select(url => new ImageCar
                {
                CarId = car.Id,
                Url = url,
                TypeId = paperImageType!.Id
                }),
            ];

            car.ImageCars = images;
            // Save new images
            await context.ImageCars.AddRangeAsync(images, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result<Response>.Success(Response.FromEntity(car));
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CarId)
                .NotEmpty().WithMessage("Phải chọn xe cần cập nhật !");
            RuleFor(x => x.CarImages)
                .NotEmpty().WithMessage("Phải chọn ảnh !");
            RuleFor(x => x.PaperImages)
            .NotEmpty().WithMessage("Phải chọn ảnh !");
        }
    }
}