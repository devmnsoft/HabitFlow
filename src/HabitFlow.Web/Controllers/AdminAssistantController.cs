using System.Text.RegularExpressions;
using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/assistant")]
public sealed class AdminAssistantController(IOptions<AssistantOptions> options, AiAdminService admin) : Controller
{
    private static readonly Regex ModelPattern = new("^[A-Za-z0-9._:-]{1,100}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    [HttpGet("")]
    public IActionResult Index() => View(options.Value);

    [HttpPost("configuration"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(bool enabled, string? provider, string? model, int? maxInputChars, int? maxOutputChars, string? defaultMessage)
    {
        var posted = Posted(enabled, provider, model, maxInputChars, maxOutputChars, defaultMessage);
        if (string.IsNullOrWhiteSpace(provider)) ModelState.AddModelError(nameof(provider), "Preencha este campo obrigatório.");
        else if (provider is not ("Disabled" or "Knowledge" or "Groq" or "Gemini" or "DeepSeek")) ModelState.AddModelError(nameof(provider), "O formato informado não é válido.");
        if (!string.IsNullOrWhiteSpace(model) && !ModelPattern.IsMatch(model.Trim())) ModelState.AddModelError(nameof(model), "O formato informado não é válido.");
        if (maxInputChars is null || maxOutputChars is null) ModelState.AddModelError("limits", "O formato informado não é válido.");
        else if (maxInputChars is < 100 or > 10000 || maxOutputChars is < 100 or > 10000) ModelState.AddModelError("limits", "O formato informado não é válido.");
        if (string.IsNullOrWhiteSpace(defaultMessage)) ModelState.AddModelError(nameof(defaultMessage), "Preencha este campo obrigatório.");
        else if (defaultMessage.Trim().Length > 500) ModelState.AddModelError(nameof(defaultMessage), "O formato informado não é válido.");
        if (!ModelState.IsValid)
        {
            await admin.RecordAsync(this.CurrentClientId(), this.CurrentUserId(), provider ?? "", model ?? "", "Invalid", "ui.form.validation.failed", HttpContext.TraceIdentifier, 0, HttpContext.RequestAborted);
            return View("Index", posted);
        }
        var value = options.Value;
        value.Enabled = enabled;
        value.Provider = provider!;
        value.Model = (model ?? "").Trim();
        value.MaxInputChars = maxInputChars!.Value;
        value.MaxOutputChars = maxOutputChars!.Value;
        value.DefaultMessage = defaultMessage!.Trim();
        TempData["Success"] = "Configuração aplicada nesta instância. Para persistir após reinício, atualize a configuração segura do ambiente.";
        return RedirectToAction(nameof(Index));
    }

    private AssistantOptions Posted(bool enabled, string? provider, string? model, int? maxInputChars, int? maxOutputChars, string? defaultMessage) => new()
    {
        Enabled = enabled,
        Provider = provider ?? "",
        Model = model ?? "",
        MaxInputChars = maxInputChars ?? options.Value.MaxInputChars,
        MaxOutputChars = maxOutputChars ?? options.Value.MaxOutputChars,
        DefaultMessage = defaultMessage ?? "",
        TimeoutSeconds = options.Value.TimeoutSeconds,
        StoreConversationHistory = options.Value.StoreConversationHistory,
        AllowHabitContext = options.Value.AllowHabitContext,
        AllowBillingContext = options.Value.AllowBillingContext
    };
}
