using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Model.Commands;

public sealed class CreateModel
{
    public sealed record Command(string Name, DateTimeOffset ReleaseDate, Guid ManufacturerId)
        : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(Model model) => new(model.Id);
    };

    internal sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Error("Bạn không có quyền thực hiện chức năng này !");

            var checkingManufacturer = await context
                .Manufacturers.AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.Id == request.ManufacturerId && !m.IsDeleted,
                    cancellationToken
                );
            if (checkingManufacturer is null)
                return Result.Error("Hãng xe không tồn tại");

            var checkingExistedModel = await context
                .Models.AsNoTracking()
                .FirstOrDefaultAsync(
                    m =>
                        m.Name == request.Name
                        && m.ManufacturerId == request.ManufacturerId
                        && !m.IsDeleted,
                    cancellationToken
                );
            if (checkingExistedModel is not null)
                return Result.Error(
                    "Mô hình xe đã tồn tại trong hãng xe " + checkingManufacturer.Name
                );

            Model model = new()
            {
                ManufacturerId = request.ManufacturerId,
                Name = request.Name,
                ReleaseDate = request.ReleaseDate,
            };
            await context.Models.AddAsync(model, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(Response.FromEntity(model), "Tạo mô hình xe thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Tên mô hình xe không được để trống")
                .MaximumLength(100)
                .WithMessage("Tên mô hình xe không được vượt quá 100 ký tự");

            RuleFor(x => x.ReleaseDate)
                .NotEmpty()
                .WithMessage("Ngày phát hành không được để trống")
                .GreaterThanOrEqualTo(DateTimeOffset.UtcNow)
                .WithMessage("Ngày phát hành phải lớn hơn hoặc bằng ngày hiện tại");

            RuleFor(x => x.ManufacturerId).NotEmpty().WithMessage("hãng xe không được để trống");
        }
    }
}
