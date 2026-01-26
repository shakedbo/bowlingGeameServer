using System.Diagnostics;
using System.Text;
using BowlingGame.API.Models;
using BowlingGame.API.Repositories.Interfaces;

namespace BowlingGame.API.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiLogRepository apiLogRepository)
    {
        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTime.UtcNow;
        string? requestBody = null;
        string? responseBody = null;
        string? exceptionMessage = null;

        // Capture request body
        context.Request.EnableBuffering();
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // Capture response body by replacing the response stream
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exceptionMessage = ex.Message;
            throw; // Re-throw to let ExceptionMiddleware handle it
        }
        finally
        {
            stopwatch.Stop();

            // Read response body
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            // Copy response back to original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // Create and persist log entry
            var log = new ApiLog
            {
                Timestamp = timestamp,
                HttpMethod = context.Request.Method,
                Path = context.Request.Path + context.Request.QueryString,
                StatusCode = context.Response.StatusCode,
                RequestBody = TruncateIfNeeded(requestBody),
                ResponseBody = TruncateIfNeeded(responseBody),
                DurationMs = stopwatch.ElapsedMilliseconds,
                ExceptionMessage = exceptionMessage
            };

            try
            {
                await apiLogRepository.InsertAsync(log);
            }
            catch (Exception ex)
            {
                // Log failure but don't break the response
                _logger.LogError(ex, "Failed to persist API log entry");
            }
        }
    }

    private static string? TruncateIfNeeded(string? value, int maxLength = 4000)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "...[truncated]";
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
