using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

[Authorize, Route("support")]
public sealed class SupportController(SupportCenterService service, ILogger<SupportController> logger) : Controller
{
    private bool Admin => User.IsInRole("SuperAdmin") || User.IsInRole("TenantAdmin") || User.IsInRole("TenantOwner");

    [AllowAnonymous, HttpGet("")]
    [HttpGet("tickets")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var contact = await service.ContactAsync(ct);
        if (User.Identity?.IsAuthenticated != true)
            return View(new SupportIndexViewModel(false, [], contact));

        var tickets = await service.ListAsync(this.CurrentClientId(), this.CurrentUserId(), Admin, ct);
        return View(new SupportIndexViewModel(true, tickets, contact));
    }

    [HttpGet("tickets/new")]
    public async Task<IActionResult> New(CancellationToken ct)
    {
        ViewBag.Contact = await service.ContactAsync(ct);
        return View();
    }

    [HttpPost("tickets/new"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string category, string priority, string subject, string description,
        string? currentRoute, string? viewport, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(description))
        {
            ModelState.AddModelError("", "Assunto e descrição são obrigatórios para abrir um chamado.");
            return await New(ct);
        }

        var plan = this.CurrentUserSnapshot().Plan.ToString();
        var id = await service.CreateAsync(
            this.CurrentClientId(), this.CurrentUserId(),
            category, priority, subject, description,
            currentRoute ?? Request.Path, Request.Headers.UserAgent.ToString(),
            viewport ?? "não informado", plan,
            HttpContext.TraceIdentifier, ct);

        TempData["Success"] = "Chamado de suporte aberto com sucesso. Nossa equipe já foi notificada.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpGet("tickets/{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var ticket = await service.GetAsync(this.CurrentClientId(), this.CurrentUserId(), id, Admin, ct);
        if (ticket is null) return NotFound();

        var messages = await service.MessagesAsync(this.CurrentClientId(), id, Admin, ct);
        var contact = await service.ContactAsync(ct);
        return View(new TicketDetailViewModel(ticket, messages, contact));
    }

    [HttpPost("tickets/{id:guid}/reply"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(Guid id, string message, CancellationToken ct)
    {
        if (!await service.ReplyAsync(this.CurrentClientId(), this.CurrentUserId(), id, Admin, message, false, ct))
        {
            TempData["Error"] = "Não foi possível enviar a resposta. Verifique a mensagem e o status atual do chamado.";
        }
        else
        {
            TempData["Success"] = "Mensagem adicionada com sucesso.";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("tickets/{id:guid}/close"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, string? reason, CancellationToken ct)
    {
        var result = await service.CloseAsync(this.CurrentClientId(), this.CurrentUserId(), id, reason, Admin, ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.Error.Message;
        }
        else
        {
            TempData["Success"] = "Chamado encerrado com sucesso.";
        }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("tickets/{id:guid}/reopen"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid id, string reason, CancellationToken ct)
    {
        var plan = this.CurrentUserSnapshot().Plan.ToString();
        var result = await service.ReopenAsync(this.CurrentClientId(), this.CurrentUserId(), id, reason, plan, ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.Error.Message;
        }
        else
        {
            TempData["Success"] = "Chamado reaberto com sucesso. Nova contagem de SLA iniciada.";
        }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("tickets/{id:guid}/satisfaction"), ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitSatisfaction(Guid id, int rating, string? feedback, CancellationToken ct)
    {
        var result = await service.SubmitSatisfactionAsync(this.CurrentClientId(), this.CurrentUserId(), id, rating, feedback, ct);
        if (result.IsFailure)
        {
            TempData["Error"] = result.Error.Message;
        }
        else
        {
            TempData["Success"] = "Agradecemos sua avaliação de atendimento!";
        }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [AllowAnonymous, HttpGet("/support/whatsapp")]
    public async Task<IActionResult> WhatsApp(CancellationToken ct)
    {
        var contact = await service.ContactAsync(ct);
        if (contact.WhatsAppUrl is null) return Redirect($"mailto:{contact.Email}");
        logger.LogInformation("support.whatsapp.opened UserId={UserId}", User.Identity?.IsAuthenticated == true ? (Guid?)this.CurrentUserId() : null);
        return Redirect(contact.WhatsAppUrl);
    }
}
