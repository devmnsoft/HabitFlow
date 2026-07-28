using Microsoft.Extensions.Configuration;

namespace HabitFlow.Application;

public sealed class UserTimeZoneService(IConfiguration configuration, TimeProvider timeProvider)
{
    public TimeZoneInfo Resolve() => TimeZoneInfo.FindSystemTimeZoneById(configuration["Progress:DefaultTimeZone"] ?? "America/Sao_Paulo");
    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Resolve()).DateTime);
}
