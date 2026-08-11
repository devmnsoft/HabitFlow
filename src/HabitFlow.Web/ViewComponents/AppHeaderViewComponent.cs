using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.ViewComponents;

public sealed class AppHeaderViewComponent(
    HeaderContextResolver contextResolver,
    HeaderCompositionService composition) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var principal = UserClaimsPrincipal;
        var context = contextResolver.Resolve(HttpContext, ViewContext.RouteData, ViewData);
        var model = await composition.ComposeAsync(HttpContext, principal, context, HttpContext.RequestAborted);
        return View(model);
    }
}
