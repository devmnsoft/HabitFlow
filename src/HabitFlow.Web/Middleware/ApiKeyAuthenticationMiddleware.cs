using System.Security.Claims;
using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Middleware;

public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IIntegrationRepository repository)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1") && context.Request.Headers.TryGetValue("X-Api-Key", out var values))
        {
            var raw = values.ToString();
            var key = raw.Length <= 128 ? await repository.FindApiKeyAsync(IntegrationService.HashSecret(raw), context.RequestAborted) : null;
            if (key is not null)
            {
                var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, key.UserId.ToString()), new("client_id", key.ClientId.ToString()), new("api_key_id", key.Id.ToString()) };
                claims.AddRange(key.Scopes.Select(scope => new Claim("scope", scope)));
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
                await repository.TouchApiKeyAsync(key.Id, context.RequestAborted);
                logger.LogInformation("api.request.received key_id={KeyId} path={Path}", key.Id, context.Request.Path);
            }
            else logger.LogWarning("api.request.denied reason=invalid_key path={Path}", context.Request.Path);
        }
        await next(context);
    }
}
