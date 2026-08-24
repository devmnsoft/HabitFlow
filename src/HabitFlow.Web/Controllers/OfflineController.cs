using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
[AllowAnonymous]
public sealed class OfflineController : Controller { [HttpGet("offline")] public IActionResult Index()=>View(); }
