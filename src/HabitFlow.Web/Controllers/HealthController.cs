using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.Controllers;

public class HealthController(ILogger<HealthController> logger) : Controller
{
    [HttpGet("health/ui")]
    public IActionResult Index()
    {
        try { return Ok(new { status = "Healthy", app = "HabitFlow" }); }
        catch (Exception ex) { logger.LogError(ex, "Erro no health check"); return StatusCode(500, new { status = "Unhealthy" }); }
    }
}
