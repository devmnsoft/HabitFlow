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
    public const string WhiteLabelManage = "white_label.manage";
    public const string SsoMfaManage = "sso_mfa.manage";
    public const string DomainsManage = "domains.manage";
    public const string IntegrationsManage = "integrations.manage";
    public const string ApiKeysManage = "api_keys.manage";
    public const string WebhooksManage = "webhooks.manage";
    public const string AiManage = "ai.manage";
    public const string LegalManage = "legal.manage";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        DashboardRead, UsersRead, UsersInvite, UsersUpdateRole, UsersDisable,
        BillingRead, BillingManage, SupportRead, SupportReply, AuditRead,
        FeatureFlagsManage, PrivacyManage, SystemHealthRead, WhiteLabelManage,
        SsoMfaManage, DomainsManage, IntegrationsManage, ApiKeysManage,
        WebhooksManage, AiManage, LegalManage
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
