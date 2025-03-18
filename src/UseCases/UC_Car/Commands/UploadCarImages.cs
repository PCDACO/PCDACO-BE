
using Ardalis.Result;

using Domain.Constants;
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
        ImageFile[] CarImages
    ) : IRequest<Result<Response>>;

    public record ImageFile
    {
        public required Stream Content { get; set; }
        public required string FileName { get; set; }
    }

    public record Response(ImageDetail[] Images)
    {
        public static Response FromEntity(Guid id, ImageCar[] files)
        {
            return new Response(
                [.. files.Select(f => new ImageDetail(id, f.Url, f.Name))]
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
            ImageType? carImageType = await context.ImageTypes
                .AsNoTracking()
                .Where(it => EF.Functions.ILike(it.Name, "%car%"))
                .Where(it => !it.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
            if (carImageType is null) return Result.Error("Không tìm thấy loại hình ảnh đang cần");
            if (car.ImageCars.Where(ic => ic.Type.Name.Equals("car", StringComparison.CurrentCultureIgnoreCase)).Any())
            {
                await context.ImageCars
                    .Where(c => c.CarId == request.CarId)
                    .Where(it => EF.Functions.ILike(it.Type.Name, "%car%"))
                    .ExecuteDeleteAsync(cancellationToken);
            }
            List<Task<string>> carTasks = [];
            int carIndex = 0;
            foreach (var image in request.CarImages)
            {
                carTasks.Add(cloudinaryServices.UploadCarImageAsync($"Car-{car.Id}-Image-{++carIndex}", image.Content, cancellationToken));
            }
            string[] carImageUrls = await Task.WhenAll(carTasks);
            ImageCar[] images =
            [
                .. Enumerable.Range(0, carImageUrls.Length)
                    .Select(i => new ImageCar
                    {
                        CarId = car.Id,
                        Url = carImageUrls[i],
                        TypeId = carImageType.Id,
                        Name = request.CarImages[i].FileName,
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
            RuleFor(x => x.CarImages)
                .NotEmpty().WithMessage("Phải chọn ảnh !");
        }
    }
}