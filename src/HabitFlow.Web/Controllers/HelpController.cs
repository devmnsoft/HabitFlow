using HabitFlow.Application;
using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;
[Route("help")]
public sealed class HelpController(AssistantKnowledgeService knowledge,SupportCenterService support,ILogger<HelpController> logger):Controller
{
    [HttpGet("")] public async Task<IActionResult> Index(string? category,CancellationToken ct){var articles=knowledge.List(category);return View(new HelpIndexViewModel(articles,knowledge.List().Select(x=>x.Category).Distinct().ToArray(),null,category,await support.ContactAsync(ct)));}
    [HttpGet("search")] public async Task<IActionResult> Search(string? q,CancellationToken ct)=>View("Index",new HelpIndexViewModel(knowledge.Search(q),knowledge.List().Select(x=>x.Category).Distinct().ToArray(),q,null,await support.ContactAsync(ct)));
    [HttpGet("getting-started")] public IActionResult GettingStarted()=>Redirect("/help/primeiros-passos");
    [HttpGet("habits")] public IActionResult Habits()=>Redirect("/help/criar-habito");
    [HttpGet("progress")] public IActionResult Progress()=>Redirect("/help/conclusao-streak");
    [HttpGet("reports")] public IActionResult Reports()=>Redirect("/help/relatorios-exportacoes");
    [HttpGet("premium")] public IActionResult Premium()=>Redirect("/help/planos");
    [HttpGet("privacy")] public IActionResult Privacy()=>Redirect("/help/privacidade-suporte");
    [HttpGet("support")] public IActionResult Support()=>Redirect("/support/tickets/new");
    [HttpGet("login")] public IActionResult Login()=>View("Login");
    [HttpGet("database-setup")] public IActionResult DatabaseSetup()=>View("DatabaseSetup");
    [HttpGet("{slug}")] public async Task<IActionResult> Article(string slug,CancellationToken ct){var article=knowledge.Get(slug);if(article is null)return NotFound();logger.LogInformation("help.article.viewed Slug={Slug}",slug);var related=knowledge.List(article.Category).Where(x=>x.Slug!=slug).Take(3).ToArray();return View("Article",new HelpArticleViewModel(article,related,await support.ContactAsync(ct)));}
}
