using System.Diagnostics;
using System.Text.RegularExpressions;

namespace HabitFlow.Web.Middleware;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";
    private static readonly EventId RequestCompleted = new(617601, "request.completed");

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsValid(supplied) ? supplied! : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        var started = Stopwatch.GetTimestamp();
        using (logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["Route"] = context.Request.Path.Value,
            ["HttpMethod"] = context.Request.Method
        }))
        {
            await next(context);
            logger.LogInformation(RequestCompleted,
                "request.completed CorrelationId={CorrelationId} Route={Route} Method={Method} DurationMs={DurationMs} Result={Result}",
                correlationId, context.Request.Path.Value, context.Request.Method,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.Response.StatusCode);
        }
    }

    public static bool IsValid(string? value) => value is not null && CorrelationPattern().IsMatch(value);

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._:-]{7,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex CorrelationPattern();
}
