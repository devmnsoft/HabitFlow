using Xunit;

namespace HabitFlow.Tests;

public sealed class DailyCenterV6183Tests
{
    private static string Root => RepositoryRootLocator.Find();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Daily_center_exposes_commercial_actions_without_technical_ids()
    {
        var view = Read("src/HabitFlow.Web/Views/MyDay/Partials/_DailyCommandCenter.cshtml");
        Assert.Contains("/habits/create", view);
        Assert.Contains("/reminders", view);
        Assert.Contains("/progress/calendar", view);
        Assert.Contains("/plans", view);
        Assert.DoesNotContain("user_id", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("client_id", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Daily_center_is_observable_and_has_a_safe_error_boundary()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/MyDayController.cs");
        Assert.Contains("daily_center.opened", controller);
        Assert.Contains("CorrelationId={CorrelationId}", controller);
        Assert.Contains("catch (Exception ex)", controller);
        Assert.DoesNotContain("this.CurrentUserId()}", controller);
    }

    [Fact]
    public void Daily_center_has_explicit_small_mobile_layout()
    {
        var css = Read("src/HabitFlow.Web/wwwroot/css/my-day-v3.css");
        Assert.Contains("@media(max-width:375px)", css);
        Assert.Contains("overflow-wrap:anywhere", css);
    }
}
