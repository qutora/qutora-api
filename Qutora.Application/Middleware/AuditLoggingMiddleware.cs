using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities;

namespace Qutora.Application.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;
    private readonly IAuditService _auditService;

    public AuditLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuditLoggingMiddleware> logger,
        IAuditService auditService)
    {
        _next = next;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirst("sub")?.Value ?? "anonymous";
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.ToString();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var isApiKeyRequest = false;
        var apiKeyId = "";
        var apiKeyPermission = "";

        if (context.User.Identity?.AuthenticationType == "ApiKey")
        {
            isApiKeyRequest = true;
            apiKeyId = context.User.FindFirst("ApiKeyId")?.Value ?? "";
            apiKeyPermission = context.User.FindFirst("ApiKeyPermission")?.Value ?? "";
        }

        try
        {
            string? requestBody = null;
            if (context.Request.Body != null &&
                (method == "POST" || method == "PUT" || method == "PATCH"))
            {
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true);
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            responseBody.Seek(0, SeekOrigin.Begin);
            var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            var entityType = isApiKeyRequest ? "API_KEY_Request" : "API_Request";
            var description = isApiKeyRequest
                ? $"API Key ({apiKeyId}/{apiKeyPermission}) request: {method} {path}{queryString}"
                : $"API request: {method} {path}{queryString}";

            var auditLog = new AuditLog
            {
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                EventType = $"{method}_{path}",
                EntityType = entityType,
                EntityId = isApiKeyRequest ? apiKeyId : Guid.NewGuid().ToString(),
                Description = description,
                Data = JsonSerializer.Serialize(new
                {
                    Request = new
                    {
                        Method = method,
                        Path = path,
                        QueryString = queryString,
                        Headers = context.Request.Headers.Where(h => !h.Key.StartsWith("X-QUTORA-"))
                            .ToDictionary(h => h.Key, h => h.Value.ToString()),
                        Body = SanitizeRequestBody(requestBody),
                        IPAddress = ipAddress
                    },
                    Response = new
                    {
                        StatusCode = context.Response.StatusCode,
                        Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                        Body = SanitizeResponseBody(responseBodyText)
                    },
                    ApiKey = isApiKeyRequest
                        ? new
                        {
                            Id = apiKeyId,
                            Permission = apiKeyPermission
                        }
                        : null
                })
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await _auditService.LogAsync(auditLog);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving audit log for {Path}", path);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in audit logging middleware for request {Path}", path);
            await _next(context);
        }
    }

    private string SanitizeRequestBody(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        try
        {
            return body;
        }
        catch
        {
            return body;
        }
    }

    private string SanitizeResponseBody(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        try
        {
            if (body.Length > 2000) return body.Substring(0, 2000) + "... [truncated]";
            return body;
        }
        catch
        {
            return body;
        }
    }
}
