using System.Security.Cryptography;
using System.Text;
using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ex.GetType().FullName + ex.Message)))[..16];
            logger.LogError(ex, "Erro não tratado capturado pelo middleware global. Fingerprint {Fingerprint}", fingerprint);
            await TryAuditAsync(context, fingerprint);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                await context.Response.WriteAsJsonAsync(new { error = "Erro inesperado.", fingerprint });
                return;
            }
            await context.Response.WriteAsync(environment.IsDevelopment() ? $"Erro inesperado. Código: {fingerprint}\n{ex}" : $"Erro inesperado. Código: {fingerprint}");
        }
    }

    private static async Task TryAuditAsync(HttpContext context, string fingerprint)
    {
        try
        {
            var audit = context.RequestServices.GetService<AuditService>();
            if (audit is not null)
            {
                await audit.LogAsync("global_exception", "Exceção não tratada no pipeline web.", AuditSeverity.Error, null, context.User.Identity?.Name, new { fingerprint, path = context.Request.Path.Value }, context.RequestAborted);
            }
        }
        catch
        {
            // Não interromper a resposta de erro caso a auditoria esteja indisponível.
        }
    }
}
