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
        ViewData["Filter"] = filter;
        if (result.IsFailure)
        {
            feedback.SetDatabaseError(this, "Não foi possível acessar os dados", "Tente novamente em instantes ou verifique a configuração do banco.");
            return View("~/Views/Admin/Clients/Index.cshtml", Array.Empty<ClientListItemDto>());
        }

        return View("~/Views/Admin/Clients/Index.cshtml", result.Value);
    }

    [HttpGet("create")]
    public IActionResult Create() => View("~/Views/Admin/Clients/Create.cshtml", new CreateClientRequest());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClientRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            feedback.SetWarning(this, "Revise os dados", "Alguns campos precisam ser corrigidos.");
            return View("~/Views/Admin/Clients/Create.cshtml", request);
        }

        var result = await clients.CreateAsync(request, this.CurrentUserSnapshot(), ct);
        if (result.IsFailure)
        {
            AddFeedback(result);
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View("~/Views/Admin/Clients/Create.cshtml", request);
        }

        feedback.SetSuccess(this, "Cliente cadastrado", "O cliente foi cadastrado com sucesso.");
        return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var result = await clients.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            AddFeedback(result);
            return RedirectToAction(nameof(Index));
        }

        return View("~/Views/Admin/Clients/Details.cshtml", result.Value);
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var result = await clients.GetByIdAsync(id, ct);
        if (result.IsFailure)
        {
            AddFeedback(result);
            return RedirectToAction(nameof(Index));
        }

        var client = result.Value!.Client;
        return View("~/Views/Admin/Clients/Edit.cshtml", new UpdateClientRequest
        {
            Name = client.Name,
            LegalName = client.LegalName,
            Document = client.Document,
            Email = client.Email,
            Phone = client.Phone,
            ContactName = client.ContactName,
            Plan = client.Plan,
            Status = client.Status,
            Notes = client.Notes
        });
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateClientRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            feedback.SetWarning(this, "Revise os dados", "Alguns campos precisam ser corrigidos.");
            return View("~/Views/Admin/Clients/Edit.cshtml", request);
        }

        var result = await clients.UpdateAsync(id, request, this.CurrentUserSnapshot(), ct);
        if (result.IsFailure)
        {
            AddFeedback(result);
            return View("~/Views/Admin/Clients/Edit.cshtml", request);
        }

        feedback.SetSuccess(this, "Cliente atualizado", "As informações do cliente foram salvas.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var result = await clients.ActivateAsync(id, this.CurrentUserSnapshot(), ct);
        if (result.IsFailure) AddFeedback(result);
        else feedback.SetSuccess(this, "Cliente ativado", "O cliente voltou a ficar ativo.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, string reason, CancellationToken ct)
    {
        var result = await clients.DeactivateAsync(id, this.CurrentUserSnapshot(), reason, ct);
        if (result.IsFailure) AddFeedback(result);
        else feedback.SetWarning(this, "Cliente desativado", "O cliente foi desativado, mas o histórico foi mantido.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/block")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(Guid id, string reason, CancellationToken ct)
    {
        var result = await clients.BlockAsync(id, this.CurrentUserSnapshot(), reason, ct);
        if (result.IsFailure) AddFeedback(result);
        else feedback.SetModal(this, "warning", "Cliente bloqueado", "O cliente foi bloqueado com segurança.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private void AddFeedback(HabitFlow.Shared.Result result)
    {
        if (result.Error.Code == "database")
        {
            feedback.SetDatabaseError(this, "Não foi possível acessar os dados", "Tente novamente em instantes ou verifique a configuração do banco.");
            return;
        }

        feedback.SetError(this, "Não foi possível concluir", result.Error.Message);
    }
}
