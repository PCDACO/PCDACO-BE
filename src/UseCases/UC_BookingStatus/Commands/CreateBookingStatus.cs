
using Ardalis.Result;

using Domain.Entities;

using FluentValidation;

using MediatR;

using UseCases.Abstractions;
using UseCases.DTOs;

namespace UseCases.UC_BookingStatus.Commands;

public class CreateBookingStatus
{
    public record Command(string Name) : IRequest<Result<Response>>;

    public record Response(
        Guid Id
    );

    public class Handler(
        IAppDBContext context,
        CurrentUser currentUser
    ) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            if (!currentUser.User!.IsAdmin())
                return Result.Forbidden("Bạn không có quyền thực hiện thao tác này");
            BookingStatus addingBookingStatus = new()
            {
                Name = request.Name
            };
            await context.BookingStatuses.AddAsync(addingBookingStatus, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(addingBookingStatus.Id), "Thêm trạng thái đặt xe thành công");
        }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên trạng thái không được để trống");
        }
    }
}