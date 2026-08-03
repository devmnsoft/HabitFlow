using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class PremiumFeedbackV55Tests
{
    [Fact]
    public void FeedbackMapper_maps_invalid_password_to_database_modal_without_json()
    {
        var feedback = new FeedbackMapper().Map("postgres.invalid_password");
        Assert.Equal(FeedbackType.Modal, feedback.FeedbackType);
        Assert.Equal(FeedbackSeverity.Database, feedback.Severity);
        Assert.DoesNotContain("{", feedback.UserMessage);
        Assert.DoesNotContain("password", feedback.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SiteJs_exposes_feedback_service_and_avoids_native_dialogs()
    {
        var js = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/wwwroot/js/site.js"));
        Assert.Contains("window.HabitFlowFeedback", js);
        Assert.Contains("showToast", js);
        Assert.Contains("showModal", js);
        Assert.Contains("showConfirm", js);
        Assert.Contains("textContent", js);
        Assert.DoesNotContain("alert(", js);
        Assert.DoesNotContain("confirm(", js);
        Assert.DoesNotContain("innerHTML", js);
    }

    [Fact]
    public void Feedback_modal_and_toast_host_are_premium_accessible_partials()
    {
        var toast = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/Partials/_ToastHost.cshtml"));
        var modal = File.ReadAllText(RepositoryRootLocator.PathTo("src/HabitFlow.Web/Views/Shared/Partials/_FeedbackModal.cshtml"));
        Assert.Contains("hf-toast-host", toast);
        Assert.Contains("aria-live=\"polite\"", toast);
        Assert.Contains("aria-labelledby", modal);
        Assert.Contains("aria-describedby", modal);
    }
}
