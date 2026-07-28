using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            WebRuntimeDiagnostics.ErrorPageCount.Add(1);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ex.GetType().FullName + ex.Message)))[..16];
            var correlationId = context.TraceIdentifier;
            logger.LogError(ex,
                "Erro não tratado. CorrelationId={CorrelationId} Fingerprint={Fingerprint} Route={Route} Method={Method} UserId={UserId} ClientId={ClientId} ExceptionType={ExceptionType} DurationMs={DurationMs}",
                correlationId, fingerprint, context.Request.Path.Value, context.Request.Method,
                Mask(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value),
                Mask(context.User.FindFirst("client_id")?.Value), ex.GetType().Name,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            await TryAuditAsync(context, fingerprint, correlationId);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.ContentType = "application/problem+json; charset=utf-8";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    type = "about:blank",
                    title = "Não foi possível concluir a solicitação.",
                    status = StatusCodes.Status500InternalServerError,
                    correlationId,
                    supportCode = fingerprint
                }), Encoding.UTF8);
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            var details = environment.IsDevelopment() ? "<p>Consulte os logs locais para detalhes técnicos.</p>" : string.Empty;
            var retryPath = HtmlEncoder.Default.Encode(context.Request.Path.Value ?? "/");
            await context.Response.WriteAsync($$"""
                <!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><title>HabitFlow — página indisponível</title></head>
                <body><main><h1>Não foi possível abrir esta página agora.</h1>
                <p>Código de atendimento: <strong>{{fingerprint}}</strong></p>{{details}}
                <p><a href="{{retryPath}}">Tentar novamente</a> <a href="/">Voltar ao início</a> <a href="/help">Ir para ajuda</a></p>
                </main></body></html>
                """, Encoding.UTF8);
        }
    }

    private static async Task TryAuditAsync(HttpContext context, string fingerprint, string correlationId)
    {
        try
        {
            var audit = context.RequestServices.GetService<AuditService>();
            if (audit is not null)
            {
                await audit.LogAsync("global_exception", "Exceção não tratada no pipeline web.", AuditSeverity.Error, null, context.User.Identity?.Name, new { fingerprint, correlationId, path = context.Request.Path.Value }, context.RequestAborted);
            }
        }
        catch
        {
            // Não interromper a resposta de erro caso a auditoria esteja indisponível.
        }
    }

    private static string Mask(string? value) => string.IsNullOrWhiteSpace(value) ? "anonymous" : value[..Math.Min(8, value.Length)] + "…";
}
