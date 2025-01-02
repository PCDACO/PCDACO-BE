using Ardalis.Result;
using MediatR;

namespace UseCases.Tests.Query;

public class Test
{
    public record Query() : IRequest<Result<string>>;

    public class Handler : IRequestHandler<Query, Result<string>>
    {
        public async Task<Result<string>> Handle(Query request, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);
            return Result.Success("Hello PCDACO!", "SUCCESS");
        }
    }
}