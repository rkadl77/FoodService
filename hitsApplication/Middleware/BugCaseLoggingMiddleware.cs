using hitsApplication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using hitsApplication.Services.Interfaces;

namespace hitsApplication.Middleware;

public class BugCaseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IBugCaseLoggingService _bugCaseLogger;
    private readonly ILogger<BugCaseLoggingMiddleware> _logger;

    public BugCaseLoggingMiddleware(
        RequestDelegate next,
        IBugCaseLoggingService bugCaseLogger,
        ILogger<BugCaseLoggingMiddleware> logger)
    {
        _next = next;
        _bugCaseLogger = bugCaseLogger;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<FeatureFlags> featureFlags)
    {
        var startTime = DateTime.UtcNow;
        string userId = null;

        try
        {
            // Пытаемся получить userId из контекста
            userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                   ?? context.User?.FindFirst("sub")?.Value
                   ?? "anonymous";

            await _next(context);

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var endpoint = context.Request.Path;

            // Логируем успешный запрос
            _bugCaseLogger.LogBackendRequest(method, endpoint.ToString(), statusCode, userId);

            // Дополнительное логирование если есть активные баги
            if (featureFlags.Value.BreakOrderCreation && endpoint.ToString().Contains("create-order"))
            {
                _logger.LogWarning("BUG-CASE: BreakOrderCreation flag is active for order creation");
            }
        }
        catch (Exception ex)
        {
            // Логируем ошибку
            _bugCaseLogger.LogBackendRequest(
                context.Request.Method,
                context.Request.Path.ToString(),
                500,
                userId);

            // Логируем информацию об ошибке
            _logger.LogError(ex, "BUG-CASE: Error in {Method} {Endpoint}",
                context.Request.Method, context.Request.Path);

            throw;
        }
    }
}