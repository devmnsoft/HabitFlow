namespace HabitFlow.Web.Services;
public sealed class ActiveRouteService(ActiveNavigationMatcher matcher)
{
    public bool IsActive(HttpContext context, string targetUrl) => matcher.Matches(context.Request.Path, targetUrl);
}
