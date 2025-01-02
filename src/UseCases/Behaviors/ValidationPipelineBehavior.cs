using Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace UseCases.Behaviors;

public class ValidationPipelineBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validtors)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validtors;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();
        var context = new ValidationContext<TRequest>(request);

        // var validationFailures = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var errors = _validators
                    .Select(x => x.Validate(context))
                    .SelectMany(validationFailure => validationFailure.Errors)
                    .Where(x => x is not null)
                    .GroupBy(
                        x => x.PropertyName.Substring(x.PropertyName.IndexOf('.') + 1),
                        x => x.ErrorMessage, (properyName, errorMessages) => new
                        {
                            Key = properyName,
                            Values = errorMessages.Distinct().ToArray()
                        }
                    )
                    .ToDictionary(x => x.Key, x => x.Values);
        if (errors.Any()) throw new ValidationAppException(errors);
        return await next().ConfigureAwait(false);
    }
}