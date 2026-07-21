using Dapper;
using HabitFlow.Application.Operations;
using HabitFlow.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HabitFlow.Web.Controllers;

public class HealthController(ILogger<HealthController> logger, DbConnectionFactory db, IConfiguration configuration, IWebHostEnvironment env) : Controller
{
    [HttpGet("health")]
    [HttpGet("health/ui")]
    public IActionResult Index() => Ok(new { status = "Healthy", app = "HabitFlow" });

    [HttpGet("health/db")]
    public async Task<IActionResult> Database(CancellationToken ct)
    {
        try
        {
            using var connection = await db.OpenAsync(ct);
            var value = await connection.ExecuteScalarAsync<int>(new CommandDefinition("select 1", cancellationToken: ct));
            return Ok(new { status = value == 1 ? "Healthy" : "Unhealthy", databaseProvider = "PostgreSQL" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro no health check do banco");
            return StatusCode(503, new { status = "Unhealthy", databaseProvider = "PostgreSQL" });
        }
    }

    [HttpGet("health/version")]
    public IActionResult Version()
    {
        var hostingMode = HostingEnvironmentDetector.Detect(configuration["App:HostingMode"], Environment.GetEnvironmentVariable("ASPNETCORE_MODULE_NAME"), Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true", Process.GetCurrentProcess().ProcessName);
        return Ok(new
        {
            appVersion = configuration["App:Version"] ?? "v4.4-WindowsIIS-Production-NoDocker",
            environment = env.EnvironmentName,
            buildTime = configuration["App:BuildTime"] ?? "não informado",
            databaseProvider = "PostgreSQL",
            hostingMode
        });
    }
}
