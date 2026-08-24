using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;
[Authorize(Roles="Admin"),Route("admin/support-settings")]
public sealed class AdminSupportSettingsController(IAssistanceRepository repository,WhatsAppValidator validator,ILogger<AdminSupportSettingsController> logger):Controller
{
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>View("~/Views/Admin/SupportSettings.cshtml",await repository.GetSupportSettingsAsync(ct));
    [HttpPost(""),ValidateAntiForgeryToken] public async Task<IActionResult> Save(SupportSettings model,CancellationToken ct){var validation=validator.Validate(new(model.IsActive&&!string.IsNullOrWhiteSpace(model.WhatsAppPhone),model.WhatsAppPhone,model.DefaultMessage,model.ButtonText));if(validation.IsFailure){ModelState.AddModelError("WhatsAppPhone",validation.Error.Message);return View("~/Views/Admin/SupportSettings.cshtml",model);}var saved=model with{Id=model.Id==Guid.Empty?Guid.Parse("61690000-0000-0000-0000-000000000001"):model.Id,UpdatedAt=DateTime.UtcNow};await repository.UpdateSupportSettingsAsync(saved,ct);logger.LogInformation("support.settings.updated AdminUserId={AdminUserId}",this.CurrentUserId());TempData["Success"]="Configurações de suporte atualizadas.";return RedirectToAction(nameof(Index));}
}
