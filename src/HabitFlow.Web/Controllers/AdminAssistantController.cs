using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/assistant")]
public sealed class AdminAssistantController(IOptions<AssistantOptions> options) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(options.Value);

    [HttpPost("configuration"), ValidateAntiForgeryToken]
    public IActionResult Configure(bool enabled, string provider, string model, int maxInputChars, int maxOutputChars, string defaultMessage)
    {
        if (provider is not ("Disabled" or "Knowledge")) ModelState.AddModelError(nameof(provider), "Provider não permitido.");
        if (maxInputChars is < 100 or > 10000 || maxOutputChars is < 100 or > 10000) ModelState.AddModelError("limits", "Os limites devem ficar entre 100 e 10.000.");
        if (string.IsNullOrWhiteSpace(defaultMessage) || defaultMessage.Length > 500) ModelState.AddModelError(nameof(defaultMessage), "Informe uma mensagem de até 500 caracteres.");
        if (!ModelState.IsValid) return View("Index", options.Value);
        var value = options.Value;
        value.Enabled = enabled;
        value.Provider = provider;
        value.Model = (model ?? "").Trim()[..Math.Min((model ?? "").Trim().Length, 100)];
        value.MaxInputChars = maxInputChars;
        value.MaxOutputChars = maxOutputChars;
        value.DefaultMessage = defaultMessage.Trim();
        TempData["Success"] = "Configuração aplicada nesta instância. Para persistir após reinício, atualize a configuração segura do ambiente.";
        return RedirectToAction(nameof(Index));
    }
}
