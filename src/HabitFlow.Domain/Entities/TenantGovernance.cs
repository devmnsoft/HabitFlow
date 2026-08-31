namespace HabitFlow.Domain;

public enum TenantStatus { Active, Suspended, CommerciallyBlocked, Disabled }

public static class TenantModules
{
    public const string Habits = "habits";
    public const string Goals = "goals";
    public const string Routines = "routines";
    public const string Calendar = "calendar";
    public const string Notifications = "notifications";
    public const string Analytics = "analytics";
    public const string Gamification = "gamification";
    public const string Assistant = "assistant";
    public const string Teams = "teams";
    public const string Integrations = "integrations";
    public const string Billing = "billing";
    public const string Support = "support";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { Habits, Goals, Routines, Calendar, Notifications, Analytics, Gamification, Assistant, Teams, Integrations, Billing, Support };
}

public sealed record TenantAccessContext(Guid UserId, Guid? TenantId, UserRole Role, TenantStatus Status, IReadOnlySet<string> EnabledModules);
public sealed record ManualChargeRequest(Guid TenantId, decimal Amount, DateOnly DueDate, string Description, string Reason);

public static class TenantLoginSelection
{
    public static bool RequiresSelection(int matchingActiveTenants) => matchingActiveTenants > 1;
    public static bool CanComplete(int matchingActiveTenants, Guid? selectedTenantId) =>
        matchingActiveTenants == 1 || matchingActiveTenants > 1 && selectedTenantId.HasValue;
}

/// <summary>Authoritative, server-side rules shared by controllers and middleware.</summary>
public static class TenantAccessPolicy
{
    public static bool CanAccessTenant(TenantAccessContext actor, Guid tenantId) =>
        actor.Role == UserRole.SuperAdmin || (actor.TenantId == tenantId && actor.Status == TenantStatus.Active);

    public static bool CanManageUsers(UserRole role) => role is UserRole.SuperAdmin or UserRole.TenantOwner or UserRole.TenantAdmin or UserRole.Admin;

    public static bool CanGrant(UserRole actor, UserRole requested) => actor == UserRole.SuperAdmin ||
        Rank(actor) >= Rank(requested) && requested != UserRole.SuperAdmin;

    public static bool CanUseModule(TenantAccessContext actor, string module) => actor.Role == UserRole.SuperAdmin ||
        actor.Status == TenantStatus.Active && TenantModules.All.Contains(module) && actor.EnabledModules.Contains(module);

    public static bool IsValidManualCharge(ManualChargeRequest request) => request.Amount > 0 &&
        !string.IsNullOrWhiteSpace(request.Description) && !string.IsNullOrWhiteSpace(request.Reason);

    private static int Rank(UserRole role) => role switch
    {
        UserRole.TenantOwner => 50, UserRole.Admin or UserRole.TenantAdmin => 40,
        UserRole.BillingAdmin or UserRole.Manager => 30, UserRole.User => 20,
        UserRole.ReadOnly => 10, _ => 0
    };
}
