using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.ViewComponents;

public sealed class NavigationViewComponent(NavigationService navigation) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(NavigationContext context)
        => View(await navigation.GetAsync(
            context,
            HttpContext.User,
            HttpContext.Request.Path,
            HttpContext.RequestAborted));
}
