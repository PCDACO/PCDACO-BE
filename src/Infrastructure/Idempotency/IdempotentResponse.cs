namespace Infrastructure.Idempotency;

public record IdempotentResponse(int StatusCode, object? Value);
