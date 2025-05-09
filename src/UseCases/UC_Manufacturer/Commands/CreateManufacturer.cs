using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Manufacturer.Commands;

public sealed class CreateManufacturer
{
    public sealed record Command(string ManufacturerName) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(Manufacturer manufacturer) => new(manufacturer.Id);
    };

    public sealed class Handler(IAppDBContext context, CurrentUser currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(
            Command request,
            CancellationToken cancellationToken
        )
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Error("Bạn không có quyền thực hiện chức năng này !");

            // Check if manufacturer is exist
            Manufacturer? checkingManufacturer = await context
                .Manufacturers.AsNoTracking()
                .FirstOrDefaultAsync(
                    m => m.Name.Trim().ToLower() == request.ManufacturerName.Trim().ToLower(),
                    cancellationToken
                );
            if (checkingManufacturer is not null)
                return Result.Error("Hãng xe đã tồn tại !");

            Manufacturer manufacturer = new() { Name = request.ManufacturerName };
            await context.Manufacturers.AddAsync(manufacturer, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(Response.FromEntity(manufacturer), "Tạo hãng xe thành công");
        }
    }

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ManufacturerName)
                .NotEmpty()
                .WithMessage("Tên hãng xe không được để trống")
                .MaximumLength(100)
                .WithMessage("Tên hãng xe không được vượt quá 100 ký tự");
        }
    }
}
