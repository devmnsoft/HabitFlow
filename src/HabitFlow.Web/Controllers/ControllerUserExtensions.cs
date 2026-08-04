using System.Security.Claims;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

internal static class ControllerUserExtensions
{
    public static Guid CurrentUserId(this Controller controller) =>
        Guid.TryParse(controller.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    public static Guid CurrentClientId(this Controller controller) =>
        Guid.TryParse(controller.User.FindFirstValue("client_id"), out var id) ? id : Guid.Empty;

    public static Guid? CurrentClientIdOrNull(this Controller controller) =>
        Guid.TryParse(controller.User.FindFirstValue("client_id"), out var id) ? id : null;

    public static User CurrentUserSnapshot(this Controller controller) => new(
        controller.CurrentUserId(),
        controller.User.Identity?.Name ?? "Usuário",
        controller.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
        string.Empty,
        null,
        Enum.TryParse<UserRole>(controller.User.FindFirstValue(ClaimTypes.Role), out var role) ? role : UserRole.User,
        Enum.TryParse<AccountStatus>(controller.User.FindFirstValue("account_status"), out var status) ? status : AccountStatus.Active,
        RiskStatus.Normal,
        UserPlan.Free,
        PlanStatus.Active,
        false,
        true,
        null,
        null,
        null,
        null,
        DateTime.UtcNow,
        DateTime.UtcNow,
        Guid.TryParse(controller.User.FindFirstValue("client_id"), out var clientId) ? clientId : null,
        int.TryParse(controller.User.FindFirstValue("session_version"), out var version) ? version : 0);
}
