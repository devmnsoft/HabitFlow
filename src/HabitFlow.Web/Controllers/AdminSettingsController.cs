using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
[Authorize(Roles="Admin")][Route("admin/settings")]
public sealed class AdminSettingsController : Controller { [HttpGet("")] public IActionResult Index()=>View("~/Views/Admin/Settings.cshtml"); }
