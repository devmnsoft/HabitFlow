using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class UserTimeZoneService(IConfiguration configuration, TimeProvider timeProvider, ILogger<UserTimeZoneService> logger)
{
    public TimeZoneInfo Resolve()
    {
        var configured = configuration["Progress:DefaultTimeZone"] ?? "America/Sao_Paulo";
        foreach (var id in new[] { configured, "America/Sao_Paulo", "E. South America Standard Time" }.Distinct())
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        logger.LogWarning("No configured progress timezone could be resolved; UTC fallback is active.");
        return TimeZoneInfo.Utc;
    }
    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Resolve()).DateTime);
    public DateTimeOffset LocalNow() => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Resolve());
}
