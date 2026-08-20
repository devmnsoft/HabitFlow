using Xunit;

namespace HabitFlow.Tests;

public sealed class HabitsVisualV6162Tests
{
    private static string Read(string relative) => File.ReadAllText(Path.Combine(RepositoryRootLocator.Root, relative.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void Habit_list_card_is_isolated_from_legacy_horizontal_card()
    {
        var card = Read("src/HabitFlow.Web/Views/Habits/Partials/_HabitCard.cshtml");
        var css = Read("src/HabitFlow.Web/wwwroot/css/habits-v4.css");
        Assert.Contains("hf-habits-list-card__header", card);
        Assert.Contains("hf-habits-list-card__metrics", card);
        Assert.DoesNotContain("class=\"hf-habit-card\"", card);
        Assert.Contains(".hf-habits-list-card{display:flex;flex-direction:column", css);
        Assert.Contains("grid-template-columns:repeat(3,minmax(320px,1fr))", css);
    }

    [Fact]
    public void Habit_css_does_not_style_every_dialog_or_reduced_motion_globally()
    {
        var css = Read("src/HabitFlow.Web/wwwroot/css/habits-v4.css");
        Assert.DoesNotContain("}dialog{", css);
        Assert.DoesNotContain("}dialog::backdrop", css);
        Assert.DoesNotContain("reduce){*{", css);
        Assert.Contains(".hf-plan-limit-dialog::backdrop", css);
    }

    [Fact]
    public void Active_header_owns_notification_loading_and_non_empty_states()
    {
        var script = Read("src/HabitFlow.Web/wwwroot/js/header-v4.js");
        Assert.Contains("/notifications/unread-count", script);
        Assert.Contains("/notifications/preview", script);
        Assert.Contains("Você não tem novas notificações", script);
        Assert.Contains("Não foi possível carregar as notificações agora", script);
    }
}
