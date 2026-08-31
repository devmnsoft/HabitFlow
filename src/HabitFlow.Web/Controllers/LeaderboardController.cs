using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HabitFlow.Web.Controllers;
[Authorize,Route("leaderboard")]
public sealed class LeaderboardController(LeaderboardService service):Controller
{
 [HttpGet("")] public async Task<IActionResult> Index(CancellationToken ct)=>View(await service.GetAsync(this.CurrentClientId(),this.CurrentUserId(),ct));
 [HttpPost("preference"),ValidateAntiForgeryToken] public async Task<IActionResult> Preference(bool optedIn,LeaderboardScope scope,string publicName,CancellationToken ct){await service.SaveAsync(this.CurrentClientId(),this.CurrentUserId(),optedIn,scope,publicName,ct);TempData["Success"]=optedIn?"Você entrou no ranking com seu nome público.":"Você saiu do ranking. Seu histórico continua privado e preservado.";return RedirectToAction(nameof(Index));}
}
