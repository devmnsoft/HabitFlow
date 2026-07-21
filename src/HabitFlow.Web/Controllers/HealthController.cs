using HabitFlow.Application;
using HabitFlow.Application.Operations;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HabitFlow.Web.Controllers;

public class HealthController(IConfiguration configuration, IWebHostEnvironment env, DatabaseDiagnosticsService diagnostics) : Controller
{
    [HttpGet("health")]
    [HttpGet("health/ui")]
    public IActionResult Index() => Ok(new { status = "Healthy", app = "HabitFlow" });

    [HttpGet("health/db")]
    public async Task<IActionResult> Database(CancellationToken ct)
    {
        if (!env.IsDevelopment() && User?.IsInRole("Admin") != true)
        {
            return Ok(new { status = "Unavailable", message = "Estamos enfrentando uma indisponibilidade temporária. Tente novamente em instantes." });
        }

        var result = await diagnostics.GetAsync(ct);
        var d = result.Value;
        var payload = new
        {
            status = d?.Status ?? "unhealthy",
            database = d?.Database,
            schema = "habitflow",
            code = d?.Status == "unhealthy" ? d?.ErrorCode ?? "postgres.unhealthy" : "postgres.ok",
            message = d?.ErrorMessage ?? "Conexão com PostgreSQL validada.",
            schemaExists = d?.SchemaExists ?? false,
            requiredTablesOk = d?.RequiredTablesOk ?? false,
            publicConflicts = d?.PublicConflicts ?? 0,
            postgresVersion = d?.PostgresVersion,
            checkedAt = d?.CheckedAt ?? DateTime.UtcNow,
            error = d?.ErrorMessage
        };
        return payload.status == "unhealthy" ? StatusCode(503, payload) : Ok(payload);
    }

    [HttpGet("diagnostics/database")]
    public async Task<IActionResult> DatabaseDiagnostics(CancellationToken ct)
    {
        if (!env.IsDevelopment() && User?.IsInRole("Admin") != true) return NotFound();
        var result = await diagnostics.GetAsync(ct);
        return View("DatabaseDiagnostics", result.Value);
    }

    [HttpGet("health/version")]
    public IActionResult Version()
    {
        var hostingMode = HostingEnvironmentDetector.Detect(configuration["App:HostingMode"], Environment.GetEnvironmentVariable("ASPNETCORE_MODULE_NAME"), Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true", Process.GetCurrentProcess().ProcessName);
        return Ok(new
        {
            appVersion = configuration["App:Version"] ?? "v4.5-DatabaseSchemaHardening-ProductionEvolution",
            environment = env.EnvironmentName,
            buildTime = configuration["App:BuildTime"] ?? "não informado",
            databaseProvider = "PostgreSQL",
            hostingMode
        });
    }
}
