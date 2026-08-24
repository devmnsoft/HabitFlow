using HabitFlow.Domain.Security;

namespace HabitFlow.Application;

/// <summary>Backend authorization rules. A tenant assignment never grants access outside its tenant.</summary>
public sealed class PermissionService
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Grants =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [AdminRoles.Owner] = AdminPermissions.All,
            [AdminRoles.Admin] = Set(AdminPermissions.DashboardRead, AdminPermissions.UsersRead, AdminPermissions.UsersInvite,
                AdminPermissions.UsersUpdateRole, AdminPermissions.UsersDisable, AdminPermissions.BillingRead,
                AdminPermissions.SupportRead, AdminPermissions.SupportReply, AdminPermissions.AuditRead,
                AdminPermissions.FeatureFlagsManage, AdminPermissions.PrivacyManage, AdminPermissions.SystemHealthRead),
            [AdminRoles.Member] = Set(),
            [AdminRoles.Support] = Set(AdminPermissions.DashboardRead, AdminPermissions.UsersRead, AdminPermissions.SupportRead, AdminPermissions.SupportReply),
            [AdminRoles.BillingAdmin] = Set(AdminPermissions.DashboardRead, AdminPermissions.BillingRead, AdminPermissions.BillingManage),
            [AdminRoles.ReadOnly] = Set(AdminPermissions.DashboardRead, AdminPermissions.UsersRead, AdminPermissions.BillingRead,
                AdminPermissions.SupportRead, AdminPermissions.AuditRead, AdminPermissions.SystemHealthRead)
        };

    public bool HasPermission(Guid currentClientId, Guid assignmentClientId, string role, string permission) =>
        currentClientId != Guid.Empty && currentClientId == assignmentClientId &&
        AdminPermissions.All.Contains(permission) && Grants.TryGetValue(role, out var permissions) && permissions.Contains(permission);

    private static IReadOnlySet<string> Set(params string[] values) => new HashSet<string>(values, StringComparer.Ordinal);
}
