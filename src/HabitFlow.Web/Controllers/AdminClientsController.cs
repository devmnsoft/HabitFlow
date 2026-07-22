using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/clients")]
public sealed class AdminClientsController(ClientService clients, ApplicationFeedbackService feedback) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ClientFilter filter, CancellationToken ct)
    {
        var result = await clients.SearchAsync(filter, ct);
        if (result.IsFailure) { feedback.SetDatabaseError(this, "Dados indisponíveis", result.Error.Message); return View("~/Views/Admin/Clients/Index.cshtml", Array.Empty<ClientListItemDto>()); }
        ViewData["Filter"] = filter; return View("~/Views/Admin/Clients/Index.cshtml", result.Value);
    }
    [HttpGet("create")] public IActionResult Create() => View("~/Views/Admin/Clients/Create.cshtml", new CreateClientRequest());
    [HttpPost("create")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClientRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("~/Views/Admin/Clients/Create.cshtml", request);
        var result = await clients.CreateAsync(request, this.CurrentUserSnapshot(), ct);
        if (result.IsFailure) { AddFeedback(result); ModelState.AddModelError(string.Empty, result.Error.Message); return View("~/Views/Admin/Clients/Create.cshtml", request); }
        feedback.SetSuccess(this, "Cliente cadastrado", "Cliente cadastrado com sucesso."); return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct) { var r = await clients.GetByIdAsync(id, ct); if (r.IsFailure) { AddFeedback(r); return RedirectToAction(nameof(Index)); } return View("~/Views/Admin/Clients/Details.cshtml", r.Value); }
    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct) { var r = await clients.GetByIdAsync(id, ct); if (r.IsFailure) { AddFeedback(r); return RedirectToAction(nameof(Index)); } var c=r.Value!.Client; return View("~/Views/Admin/Clients/Edit.cshtml", new UpdateClientRequest { Name=c.Name, LegalName=c.LegalName, Document=c.Document, Email=c.Email, Phone=c.Phone, ContactName=c.ContactName, Plan=c.Plan, Status=c.Status, Notes=c.Notes }); }
    [HttpPost("{id:guid}/edit")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateClientRequest request, CancellationToken ct) { if (!ModelState.IsValid) return View("~/Views/Admin/Clients/Edit.cshtml", request); var r=await clients.UpdateAsync(id,request,this.CurrentUserSnapshot(),ct); if(r.IsFailure){AddFeedback(r);return View("~/Views/Admin/Clients/Edit.cshtml",request);} feedback.SetSuccess(this,"Cliente atualizado","Cliente atualizado com sucesso."); return RedirectToAction(nameof(Details),new{id}); }
    [HttpPost("{id:guid}/activate")][ValidateAntiForgeryToken] public async Task<IActionResult> Activate(Guid id, CancellationToken ct){var r=await clients.ActivateAsync(id,this.CurrentUserSnapshot(),ct); if(r.IsFailure)AddFeedback(r); else feedback.SetSuccess(this,"Cliente ativado","Cliente ativado."); return RedirectToAction(nameof(Details),new{id});}
    [HttpPost("{id:guid}/deactivate")][ValidateAntiForgeryToken] public async Task<IActionResult> Deactivate(Guid id,string reason,CancellationToken ct){var r=await clients.DeactivateAsync(id,this.CurrentUserSnapshot(),reason,ct); if(r.IsFailure)AddFeedback(r); else feedback.SetWarning(this,"Cliente desativado","Cliente desativado."); return RedirectToAction(nameof(Details),new{id});}
    [HttpPost("{id:guid}/block")][ValidateAntiForgeryToken] public async Task<IActionResult> Block(Guid id,string reason,CancellationToken ct){var r=await clients.BlockAsync(id,this.CurrentUserSnapshot(),reason,ct); if(r.IsFailure)AddFeedback(r); else feedback.SetWarning(this,"Cliente bloqueado","Cliente bloqueado."); return RedirectToAction(nameof(Details),new{id});}
    private void AddFeedback(HabitFlow.Shared.Result r){ if(r.Error.Code=="database") feedback.SetDatabaseError(this,"Dados indisponíveis",r.Error.Message); else feedback.SetError(this,"Não foi possível concluir",r.Error.Message); }
}
