using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;
[Authorize, Route("support")]
public sealed class SupportController(SupportCenterService service,ILogger<SupportController> logger):Controller
{
    private bool Admin=>User.IsInRole("Admin");
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
    [HttpGet("tickets/new")] public async Task<IActionResult> New(CancellationToken ct){ViewBag.Contact=await service.ContactAsync(ct);return View();}
    [HttpPost("tickets/new"),ValidateAntiForgeryToken] public async Task<IActionResult> Create(string category,string subject,string description,string? currentRoute,string? viewport,CancellationToken ct){if(string.IsNullOrWhiteSpace(subject)||string.IsNullOrWhiteSpace(description)){ModelState.AddModelError("","Assunto e descrição são obrigatórios.");return await New(ct);}var id=await service.CreateAsync(this.CurrentClientId(),this.CurrentUserId(),category,subject,description,currentRoute??Request.Path,Request.Headers.UserAgent.ToString(),viewport??"não informado",this.CurrentUserSnapshot().Plan.ToString(),HttpContext.TraceIdentifier,ct);return RedirectToAction(nameof(Detail),new{id});}
    [HttpGet("tickets/{id:guid}")] public async Task<IActionResult> Detail(Guid id,CancellationToken ct){var ticket=await service.GetAsync(this.CurrentClientId(),this.CurrentUserId(),id,Admin,ct);if(ticket is null)return NotFound();return View(new TicketDetailViewModel(ticket,await service.MessagesAsync(this.CurrentClientId(),id,ct),await service.ContactAsync(ct)));}
    [HttpPost("tickets/{id:guid}/reply"),ValidateAntiForgeryToken] public async Task<IActionResult> Reply(Guid id,string message,CancellationToken ct){if(!string.IsNullOrWhiteSpace(message))await service.ReplyAsync(this.CurrentClientId(),this.CurrentUserId(),id,Admin,message,ct);return RedirectToAction(nameof(Detail),new{id});}
    [HttpPost("tickets/{id:guid}/close"),ValidateAntiForgeryToken] public async Task<IActionResult> Close(Guid id,CancellationToken ct){await service.CloseAsync(this.CurrentClientId(),this.CurrentUserId(),id,Admin,ct);return RedirectToAction(nameof(Detail),new{id});}
    [AllowAnonymous,HttpGet("/support/whatsapp")] public async Task<IActionResult> WhatsApp(CancellationToken ct){var contact=await service.ContactAsync(ct);if(contact.WhatsAppUrl is null)return Redirect($"mailto:{contact.Email}");logger.LogInformation("support.whatsapp.opened UserId={UserId}",User.Identity?.IsAuthenticated==true?(Guid?)this.CurrentUserId():null);return Redirect(contact.WhatsAppUrl);}
}
