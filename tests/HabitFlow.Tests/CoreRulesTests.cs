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

 
public class AdminOperationalRulesTests
{
    [Theory]
    [InlineData("=cmd")]
    [InlineData("+cmd")]
    [InlineData("-cmd")]
    [InlineData("@cmd")]
    public void Admin_csv_export_sanitizes_formula_injection(string input)
    {
        Assert.StartsWith("'", AdminExportService.SanitizeCsvCell(input));
    }

    [Fact]
    public void Blocked_user_cannot_use_dashboard()
    {
        var user = new User(Guid.NewGuid(), "U", "u@e.com", "hash", null, UserRole.User, AccountStatus.Blocked, RiskStatus.Normal, UserPlan.Free, PlanStatus.Active, false, false, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow);
        Assert.False(user.CanUseDashboard);
    }

    [Fact]
    public void Suspicious_risk_status_exists_for_admin_review()
    {
        Assert.Equal("Suspicious", RiskStatus.Suspicious.ToString());
    }
}
