using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;
[Authorize,Route("support/tickets")]
public sealed class SupportController(SupportCenterService service,ILogger<SupportController> logger):Controller
{
    private bool Admin=>User.IsInRole("Admin");
    [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>View(await service.ListAsync(this.CurrentClientId(),this.CurrentUserId(),Admin,ct));
    [HttpGet("new")] public async Task<IActionResult> New(CancellationToken ct){ViewBag.Contact=await service.ContactAsync(ct);return View();}
    [HttpPost("new"),ValidateAntiForgeryToken] public async Task<IActionResult> Create(string category,string subject,string description,string? currentRoute,string? viewport,CancellationToken ct){if(string.IsNullOrWhiteSpace(subject)||string.IsNullOrWhiteSpace(description)){ModelState.AddModelError("","Assunto e descrição são obrigatórios.");return await New(ct);}var id=await service.CreateAsync(this.CurrentClientId(),this.CurrentUserId(),category,subject,description,currentRoute??Request.Path,Request.Headers.UserAgent.ToString(),viewport??"não informado",this.CurrentUserSnapshot().Plan.ToString(),HttpContext.TraceIdentifier,ct);return RedirectToAction(nameof(Detail),new{id});}
    [HttpGet("{id:guid}")] public async Task<IActionResult> Detail(Guid id,CancellationToken ct){var ticket=await service.GetAsync(this.CurrentClientId(),this.CurrentUserId(),id,Admin,ct);if(ticket is null)return NotFound();return View(new TicketDetailViewModel(ticket,await service.MessagesAsync(this.CurrentClientId(),id,ct),await service.ContactAsync(ct)));}
    [HttpPost("{id:guid}/reply"),ValidateAntiForgeryToken] public async Task<IActionResult> Reply(Guid id,string message,CancellationToken ct){if(!string.IsNullOrWhiteSpace(message))await service.ReplyAsync(this.CurrentClientId(),this.CurrentUserId(),id,Admin,message,ct);return RedirectToAction(nameof(Detail),new{id});}
    [HttpPost("{id:guid}/close"),ValidateAntiForgeryToken] public async Task<IActionResult> Close(Guid id,CancellationToken ct){await service.CloseAsync(this.CurrentClientId(),this.CurrentUserId(),id,Admin,ct);return RedirectToAction(nameof(Detail),new{id});}
    [AllowAnonymous,HttpGet("/support/whatsapp")] public async Task<IActionResult> WhatsApp(CancellationToken ct){var contact=await service.ContactAsync(ct);if(contact.WhatsAppUrl is null)return Redirect($"mailto:{contact.Email}");logger.LogInformation("support.whatsapp.opened UserId={UserId}",User.Identity?.IsAuthenticated==true?(Guid?)this.CurrentUserId():null);return Redirect(contact.WhatsAppUrl);}
}
