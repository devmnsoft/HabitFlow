namespace HabitFlow.Domain;

public static class PlanCodes
{
    public const string Free = "free";
    public const string Ritmo = "ritmo";
    public const string Evolucao = "evolucao";
}

public static class PlanFeatureCodes
{
    public const string ActiveHabitsLimit = "active_habits_limit";
    public const string UsersLimit = "users_limit";
    public const string FullHabitLibrary = "full_habit_library";
    public const string AdvancedReports = "advanced_reports";
    public const string ReportExportCsv = "report_export_csv";
    public const string SharedRoutines = "shared_routines";
}

public sealed record PlanFeatureValue(string Code, string Name, string ValueType, bool? BoolValue, int? IntValue, string? StringValue);
public sealed record PlanPrice(Guid Id, string BillingCycle, decimal Amount, string Currency);
public sealed record PublicPlan(Guid Id, string Code, string PublicName, string? Headline, string? Description, string? AudienceText, string? BadgeText, bool IsFeatured, int SortOrder, IReadOnlyList<PlanPrice> Prices, IReadOnlyList<PlanFeatureValue> Features);
public sealed record ClientPlanAccess(Guid ClientId, string ContractedPlanCode, string EffectivePlanCode, string BenefitsStatus, DateTime? GracePeriodUntil);

