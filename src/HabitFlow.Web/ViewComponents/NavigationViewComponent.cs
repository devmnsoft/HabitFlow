using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.ViewComponents;

public sealed class NavigationViewComponent(NavigationService navigation) : ViewComponent
{
    public IViewComponentResult Invoke(NavigationContext context)
        => View(navigation.Get(context, HttpContext.User, HttpContext.Request.Path));
}
