using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize]
public sealed class ProductTipController(ProductTipService tips) : Controller
{
    [ValidateAntiForgeryToken, HttpPost("product-tips/{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken ct)
    {
        var changed = await tips.DismissAsync(this.CurrentClientId(), this.CurrentUserId(), id, ct);
        return changed ? NoContent() : NotFound();
    }

    [ValidateAntiForgeryToken, HttpPost("product-tips/reopen")]
    public async Task<IActionResult> Reopen(CancellationToken ct)
    {
        await tips.ReopenAsync(this.CurrentClientId(), this.CurrentUserId(), ct);
        TempData["Success"] = "Guia de primeiro uso reaberto.";
        return Redirect("/dashboard");
    }
}

