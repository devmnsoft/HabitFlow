using System.Security.Claims;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Http;

namespace HabitFlow.Application;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;
    public string Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public string Name => Principal?.Identity?.Name ?? Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public UserRole Role => Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role) ? role : UserRole.User;
    public Guid? ClientId => Guid.TryParse(Principal?.FindFirstValue("client_id"), out var id) ? id : null;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public bool IsSuperAdmin => IsAuthenticated && Role == UserRole.SuperAdmin;
    public bool IsAdmin => IsAuthenticated && Role == UserRole.Admin;
    public bool IsUser => IsAuthenticated && Role == UserRole.User;
    public bool RequiresClient => IsAdmin || IsUser;
    public bool HasClient => ClientId.HasValue;
}
