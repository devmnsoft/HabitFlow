namespace HabitFlow.Domain.Security;

/// <summary>Canonical permission names used by the SaaS administration boundary.</summary>
public static class AdminPermissions
{
    public const string DashboardRead = "admin.dashboard.read";
    public const string UsersRead = "users.read";
    public const string UsersInvite = "users.invite";
    public const string UsersUpdateRole = "users.update_role";
    public const string UsersDisable = "users.disable";
    public const string BillingRead = "billing.read";
    public const string BillingManage = "billing.manage";
    public const string SupportRead = "support.read";
    public const string SupportReply = "support.reply";
    public const string AuditRead = "audit.read";
    public const string FeatureFlagsManage = "feature_flags.manage";
    public const string PrivacyManage = "privacy.manage";
    public const string SystemHealthRead = "system_health.read";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        DashboardRead, UsersRead, UsersInvite, UsersUpdateRole, UsersDisable,
        BillingRead, BillingManage, SupportRead, SupportReply, AuditRead,
        FeatureFlagsManage, PrivacyManage, SystemHealthRead
    };
}

public static class AdminRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Member = "Member";
    public const string Support = "Support";
    public const string BillingAdmin = "BillingAdmin";
    public const string ReadOnly = "ReadOnly";
}
