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
    public const string RemindersPerHabit = "reminders_per_habit";
    public const string ActiveGoalsLimit = "active_goals_limit";
    public const string CustomCategories = "custom_categories";
    public const string BasicReports = "basic_reports";
    public const string ReportPrint = "report_print";
    public const string FullHistory = "full_history";
    public const string HistoryDaysLimit = "history_days_limit";
    public const string SharedGoals = "shared_goals";
    public const string ClientAdminDashboard = "client_admin_dashboard";
    public const string ConsolidatedReports = "consolidated_reports";
    public const string UserInvitations = "user_invitations";
    public const string PrioritySupport = "priority_support";
    public const string InternalCommunications = "internal_communications";
}

public sealed record PlanFeatureValue(string Code, string Name, string ValueType, bool? BoolValue, int? IntValue, string? StringValue);
public enum PlanFeatureImplementationStatus { Implemented, Partial, Planned, Internal, Deprecated }
public sealed record PlanFeatureImplementation(string Code, PlanFeatureImplementationStatus Status, string PublicName,
    string PublicDescription, string Evidence, bool IsMarketable, string? ImplementedSince,
    IReadOnlyList<string> RequiredRoutes, IReadOnlyList<string> RequiredServices);
public sealed record PlanPublicBenefit(string PlanCode, string FeatureCode, string Title, string Description,
    string IconCode, int SortOrder, string ComparisonGroup, bool IsHighlighted,
    PlanFeatureImplementationStatus ImplementationStatus);
public sealed record PlanPrice(Guid Id, string BillingCycle, decimal Amount, string Currency);
public sealed record PublicPlan(Guid Id, string Code, string PublicName, string? Headline, string? Description, string? AudienceText, string? BadgeText, bool IsFeatured, int SortOrder, IReadOnlyList<PlanPrice> Prices, IReadOnlyList<PlanFeatureValue> Features);
public sealed record ClientPlanAccess(Guid ClientId, string ContractedPlanCode, string EffectivePlanCode, string BenefitsStatus, DateOnly? GracePeriodUntil);
