namespace HabitFlow.Web.Models;

public sealed record PlanUsageLimitViewModel(string Label, int Current, int? Limit)
{
    public int? Percentage => Limit is > 0 ? Math.Min(100, (int)Math.Round(Current * 100d / Limit.Value)) : null;
    public int? Available => Limit is >= 0 ? Math.Max(0, Limit.Value - Current) : null;
    public bool IsAtLimit => Limit is >= 0 && Current >= Limit;
    public bool IsNearLimit => Percentage is >= 80 and < 100;
}

public sealed record PlanFeatureUsageViewModel(string Name, string Description, bool Available);
public sealed record PlanUpgradeSuggestionViewModel(string Title, string Description, string Url);

public sealed record PlanUsageViewModel(
    string PlanName,
    bool HasPaidSubscription,
    PlanUsageLimitViewModel ActiveHabits,
    PlanUsageLimitViewModel ActiveGoals,
    IReadOnlyList<PlanFeatureUsageViewModel> Features,
    IReadOnlyList<PlanFeatureUsageViewModel> BlockedFeatures,
    string HistoryDescription,
    string? PaymentStatus,
    DateTime? NextBillingAt,
    PlanUpgradeSuggestionViewModel? Upgrade);

public sealed record PlanChangeImpactViewModel(string PlanCode, string PlanName, bool IsCurrent,
    IReadOnlyList<string> Changes, string ConfirmationMessage);
