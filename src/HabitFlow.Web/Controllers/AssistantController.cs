using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitFlow.Web.Controllers;
[Authorize,Route("assistant")]
public sealed class AssistantController(AssistantChatService service,SupportCenterService support):Controller
{
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct){ViewBag.Contact=await support.ContactAsync(ct);ViewBag.AssistantEnabled=service.IsEnabled;ViewBag.MaxInputChars=service.Configuration.MaxInputChars;return View();}
    [HttpPost("ask"),ValidateAntiForgeryToken,EnableRateLimiting("assistant")]
    public async Task<IActionResult> Ask(string message,CancellationToken ct){var max=Math.Clamp(service.Configuration.MaxInputChars,100,10000);if(string.IsNullOrWhiteSpace(message)||message.Length>max)return BadRequest(new{message=$"Escreva uma pergunta de até {max:N0} caracteres."});var correlation=HttpContext.TraceIdentifier;return Json(await service.AskAsync(this.CurrentClientId(),this.CurrentUserId(),message,correlation,ct));}
    [HttpPost("history/delete"),ValidateAntiForgeryToken] public async Task<IActionResult> Delete(CancellationToken ct){await service.DeleteAsync(this.CurrentClientId(),this.CurrentUserId(),ct);TempData["Success"]="Histórico apagado com segurança.";return RedirectToAction(nameof(Index));}
}
