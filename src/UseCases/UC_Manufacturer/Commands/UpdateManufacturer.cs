using Ardalis.Result;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_Manufacturer.Commands;

public sealed class UpdateManufacturer
{
    public sealed record Command(Guid Id, string ManufacturerName) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id, string Name)
    {
        public static Response FromEntity(Manufacturer manufacturer) =>
            new(manufacturer.Id, manufacturer.Name);
    };

    public class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Error("Bạn không có quyền thực hiện thao tác này");

            // Check if manufacturer is exist
            Manufacturer? updatingManufacturer = await context.Manufacturers.FirstOrDefaultAsync(
                m => m.Id == request.Id,
                cancellationToken
            );
            if (updatingManufacturer is null)
                return Result.NotFound("Không tìm thấy hãng xe");

            // Update the manufacturer
            updatingManufacturer.Name = request.ManufacturerName;
            updatingManufacturer.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(
                Response.FromEntity(updatingManufacturer),
                "Cập nhật hãng xe thành công"
            );
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống");
            RuleFor(x => x.ManufacturerName)
                .NotEmpty()
                .WithMessage("Tên hãng xe không được để trống")
                .MaximumLength(100)
                .WithMessage("Tên hãng xe không được vượt quá 100 ký tự");
        }
    }
}
