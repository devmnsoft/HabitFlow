using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.ViewComponents;

public sealed class NavigationViewComponent(NavigationService navigation) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        NavigationContext context,
        NavigationVariant variant)
    {
        var items = await navigation.GetAsync(
            context,
            HttpContext.User,
            HttpContext.Request.Path,
            HttpContext.RequestAborted);

        if (variant == NavigationVariant.MobileBottom)
        {
            items = items.Take(5).ToArray();
        }

        return View(new NavigationViewModel(context, variant, items));
    }
}
