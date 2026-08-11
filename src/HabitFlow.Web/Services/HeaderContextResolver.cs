using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace HabitFlow.Web.Services;

public sealed class HeaderContextResolver(LayoutContextResolver layout)
{
    public NavigationContext Resolve(HttpContext httpContext, RouteData routeData, ViewDataDictionary viewData)
        => layout.Resolve(httpContext, routeData, viewData);
}
