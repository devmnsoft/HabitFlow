using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HabitFlow.Web.Controllers;
[Authorize,Route("assistant")]
public sealed class AssistantController(AssistantConversationService service,SupportCenterService support):Controller
{
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct){ViewBag.Contact=await support.ContactAsync(ct);return View();}
    [HttpPost("ask"),ValidateAntiForgeryToken,EnableRateLimiting("assistant")]
    public async Task<IActionResult> Ask(string message,CancellationToken ct){if(string.IsNullOrWhiteSpace(message)||message.Length>1000)return BadRequest(new{message="Escreva uma pergunta de até 1.000 caracteres."});var correlation=HttpContext.TraceIdentifier;return Json(await service.AskAsync(this.CurrentClientId(),this.CurrentUserId(),message,correlation,ct));}
    [HttpPost("history/delete"),ValidateAntiForgeryToken] public async Task<IActionResult> Delete(CancellationToken ct){await service.DeleteAsync(this.CurrentClientId(),this.CurrentUserId(),ct);TempData["Success"]="Histórico apagado com segurança.";return RedirectToAction(nameof(Index));}
}
