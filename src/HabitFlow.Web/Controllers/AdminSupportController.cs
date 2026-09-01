using HabitFlow.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
[Authorize(Roles="SuperAdmin")][Route("admin/support")]
public sealed class AdminSupportController(AdminSupportService service) : Controller
{ [HttpGet("")] public async Task<IActionResult> Index([FromQuery]SupportTicketFilter filter,CancellationToken ct)=>View("~/Views/Admin/Support.cshtml",await service.SearchTicketsAsync(filter,ct)); [HttpGet("{id:guid}")] public async Task<IActionResult> Detail(Guid id,CancellationToken ct)=>View("~/Views/Admin/SupportDetail.cshtml",await service.GetTicketDetailAsync(id,ct)); [HttpPost("{id:guid}/status")][ValidateAntiForgeryToken] public async Task<IActionResult> Status(Guid id,string status,string message,CancellationToken ct){await service.UpdateTicketStatusAsync(this.CurrentUserSnapshot(),id,status,message,ct);return RedirectToAction(nameof(Detail),new{id});}}
