using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Xunit;

namespace HabitFlow.Tests;

public class CoreRulesTests
{
    [Fact]
    public void Free_plan_limit_blocks_sixth_active_habit()
    {
        var user = User(UserPlan.Free);
        Assert.False(DomainPolicies.CanCreateHabit(user, 5));
    }

    [Fact]
    public void Current_streak_is_calculated()
    {
        var service = new ProgressService();
        Assert.Equal(2, service.CurrentStreak([new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)], new DateOnly(2026, 1, 2)));
    }

    [Fact]
    public void Best_streak_is_calculated()
    {
        var service = new ProgressService();
        Assert.Equal(3, service.BestStreak([new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3)]));
    }

    [Fact]
    public void Whatsapp_rejects_script()
    {
        var validator = new WhatsAppValidator();
        Assert.True(validator.Validate(new WhatsAppOptions(true, "+5511999999999", "<script>", "Chamar")).IsFailure);
    }

    [Fact]
    public void Sanitizer_masks_secrets()
    {
        Assert.DoesNotContain("abc", new LogSanitizer().Sanitize("token=abc"));
    }

    [Fact]
    public void Protocol_has_prefix()
    {
        Assert.StartsWith("HF-", new ProtocolGenerator().Generate());
    }

    [Fact]
    public void Result_carries_value()
    {
        var result = Result<int>.Success(10);
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value);
    }

    [Fact]
    public void Admin_authorization_requires_admin()
    {
        Assert.True(new AdminAuthorizationService().EnsureAdmin(User(UserPlan.Free)).IsFailure);
    }

    [Fact]
    public void Password_hash_roundtrip()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("Admin@123");
        Assert.True(hasher.Verify("Admin@123", hash));
    }

    private static User User(UserPlan plan) => new(Guid.NewGuid(), "U", "u@e.com", "hash", null, UserRole.User, AccountStatus.Active, RiskStatus.Normal, plan, PlanStatus.Active, false, false, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow);
}

public class HabitRecurrenceRulesTests
{
    private static readonly HabitScheduleService Schedule = new(null!, null!, null!);
    private static Habit Habit(HabitFrequencyType frequency) => new(Guid.NewGuid(), Guid.NewGuid(), "Teste", "#198754", "Saúde", false, null, DateTime.UtcNow, DateTime.UtcNow, frequency);

    [Fact]
    public void Daily_is_due_every_day() => Assert.True(Schedule.IsHabitDueOnDate(Habit(HabitFrequencyType.Daily), new DateOnly(2026, 7, 21), []));

    [Fact]
    public void Weekdays_are_due_from_monday_to_friday()
    {
        Assert.True(Schedule.IsHabitDueOnDate(Habit(HabitFrequencyType.Weekdays), new DateOnly(2026, 7, 21), []));
        Assert.False(Schedule.IsHabitDueOnDate(Habit(HabitFrequencyType.Weekdays), new DateOnly(2026, 7, 25), []));
    }

    [Fact]
    public void Weekends_are_due_on_saturday_and_sunday()
    {
        Assert.True(Schedule.IsHabitDueOnDate(Habit(HabitFrequencyType.Weekends), new DateOnly(2026, 7, 25), []));
        Assert.False(Schedule.IsHabitDueOnDate(Habit(HabitFrequencyType.Weekends), new DateOnly(2026, 7, 21), []));
    }

    [Fact]
    public void Custom_weekly_uses_selected_days()
    {
        var days = new[] { new HabitWeekDay(Guid.NewGuid(), Guid.NewGuid(), 2, DateTime.UtcNow) };
        Assert.True(Schedule.IsHabitDueOnDate(Habit(HabitFrequencyType.CustomWeekly), new DateOnly(2026, 7, 21), days));
    }

    [Fact]
    public void Validate_frequency_rejects_invalid_target() => Assert.True(Schedule.ValidateFrequency(HabitFrequencyType.Daily, 8, []).IsFailure);

    [Fact]
    public void Csv_export_sanitizes_formula_injection() => Assert.StartsWith("'", ReportService.SanitizeCsv("=cmd"));
}
