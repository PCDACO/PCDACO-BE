using Ardalis.Result;
using Domain.Entities;
using Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UseCases.Abstractions;
using UseCases.DTOs;
using UUIDNext;

namespace UseCases.UC_Manufacturer;

public sealed class CreateManufacturer
{
    public sealed record Command(string ManufacturerName) : IRequest<Result<Response>>;

    public sealed record Response(Guid Id)
    {
        public static Response FromEntity(Manufacturer manufacturer) => new(manufacturer.Id);
    };

    private sealed class Handler(
        IAppDBContext context,
        CurrentUser currentUser,
        IAesEncryptionService aesEncryptionService,
        IKeyManagementService keyManagementService,
        EncryptionSettings encryptionSettings
    ) : IRequestHandler<Command, Result<Response>>
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
                    m => m.Name == request.ManufacturerName && !m.IsDeleted,
                    cancellationToken
                );
            if (checkingManufacturer is not null)
                return Result.Error("Hãng xe đã tồn tại !");

            Manufacturer manufacturer = new() { Name = request.ManufacturerName };
            await context.Manufacturers.AddAsync(manufacturer, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Created(Response.FromEntity(manufacturer), "Tạo hãng xe thành công");
        }
    }
}
