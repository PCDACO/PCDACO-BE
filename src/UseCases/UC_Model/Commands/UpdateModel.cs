using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Model.Commands;

public sealed class UpdateModel
{
    public sealed record Command(
        Guid ModelId,
        string Name,
        DateTimeOffset ReleaseDate,
        Guid ManufacturerId
    ) : IRequest<Result<Response>>;

    public sealed record Response(
        Guid ModelId,
        string Name,
        DateTimeOffset ReleaseDate,
        Guid ManufacturerId
    )
    {
        public static Response FromEntity(Model model) =>
            new(model.Id, model.Name, model.ReleaseDate, model.ManufacturerId);
    };

    public sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            // check if the user is not admin
            if (!currentUser.User!.IsAdmin())
                return Result.Error("Bạn không có quyền thực hiện chức năng này !");

            // check if the model exists
            var updatingModel = await context.Models.FirstOrDefaultAsync(
                m => m.Id == request.ModelId && !m.IsDeleted,
                cancellationToken
            );
            if (updatingModel is null)
                return Result.Error("Mô hình xe không tồn tại");

            // check if the manufacturer exists
            var checkingManufacturer = await context
                .Manufacturers.AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.Id == request.ManufacturerId && !m.IsDeleted,
                    cancellationToken
                );
            if (checkingManufacturer is null)
                return Result.Error("Hãng xe không tồn tại");

            // Check if another model with the same name already exists for this manufacturer
            bool duplicateNameExists = await context
                .Models.AsNoTracking()
                .AnyAsync(
                    m =>
                        m.Id != request.ModelId
                        && m.ManufacturerId == request.ManufacturerId
                        && m.Name.Trim().ToLower() == request.Name.Trim().ToLower(),
                    cancellationToken
                );

            if (duplicateNameExists)
                return Result.Error("Tên mô hình xe đã tồn tại trong hãng xe này");

            updatingModel.ManufacturerId = request.ManufacturerId;
            updatingModel.Name = request.Name;
            updatingModel.ReleaseDate = request.ReleaseDate;
            updatingModel.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(
                Response.FromEntity(updatingModel),
                "Cập nhật mô hình xe thành công"
            );
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
                .WithMessage("Ngày phát hành không được để trống");

            RuleFor(x => x.ManufacturerId).NotEmpty().WithMessage("hãng xe không được để trống");
        }
    }
}
