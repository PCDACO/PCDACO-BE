
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
        Stream[] Images
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
            List<Task<string>> tasks = [];
            int index = 0;
            foreach (var image in request.Images)
            {
                tasks.Add(cloudinaryServices.UploadCarImageAsync($"Car-{car.Id}-Image-{++index}", image, cancellationToken));
            }
            string[] urls = await Task.WhenAll(tasks);
            ImageCar[] images = [.. urls.Select(url => new ImageCar
            {
                CarId = car.Id,
                Url = url
            })];
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
            RuleFor(x => x.Images)
                .NotEmpty().WithMessage("Phải chọn ảnh !");
        }
    }
}