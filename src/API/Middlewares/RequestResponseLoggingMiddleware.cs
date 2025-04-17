using Microsoft.IO;

namespace API.Middlewares;

public class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger
)
{
    private readonly RequestDelegate _next = next;
    private readonly RecyclableMemoryStreamManager _streamManager = new();
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;

        try
        {
            // Skip logging for multipart/form-data requests (file uploads)
            if (context.Request.ContentType?.Contains("multipart/form-data") == true)
            {
                await _next(context);
                return;
            }

            // Enable buffering for the request
            context.Request.EnableBuffering();

            // Read the request body
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                var requestBody = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogInformation("Request: {RequestBody}", requestBody);
                }

                // Reset the request body stream position
                context.Request.Body.Position = 0;
            }

            // Prepare for reading response
            await using var responseStream = _streamManager.GetStream();
            context.Response.Body = responseStream;

            // Continue with the pipeline
            await _next(context);

            // Read response body
            responseStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseStream).ReadToEndAsync();
            if (!string.IsNullOrEmpty(responseBody))
            {
                _logger.LogInformation("Response: {ResponseBody}", responseBody);
            }

            // Copy to the original stream
            responseStream.Seek(0, SeekOrigin.Begin);
            await responseStream.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }
}
