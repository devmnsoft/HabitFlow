using Dapper;
using HabitFlow.Application.Operations;
using HabitFlow.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace HabitFlow.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminEnvironmentController(DbConnectionFactory db, IConfiguration configuration, IWebHostEnvironment env, ILogger<AdminEnvironmentController> logger) : Controller
{
    [HttpGet("admin/environment")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dbStatus = "Unhealthy";
        IReadOnlyList<object> events = [];
        try
        {
            using var connection = await db.OpenAsync(ct);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition("select 1", cancellationToken: ct));
            dbStatus = "Healthy";
            events = (await connection.QueryAsync<object>(new CommandDefinition("select version, environment, hosting_mode, action, status, notes, created_at from habitflow.deployment_events order by created_at desc limit 5", cancellationToken: ct))).ToList();
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar diagnóstico de ambiente"); }

        var model = new AdminEnvironmentViewModel(
            HostingEnvironmentDetector.Detect(configuration["App:HostingMode"], Environment.GetEnvironmentVariable("ASPNETCORE_MODULE_NAME"), Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true", Process.GetCurrentProcess().ProcessName),
            env.EnvironmentName,
            configuration["App:Version"] ?? "v4.4-WindowsIIS-Production-NoDocker",
            dbStatus,
            configuration.GetValue<bool>("Telegram:Enabled"),
            configuration.GetValue<bool>("WhatsApp:Enabled"),
            ConnectionStringMasker.Mask(configuration.GetConnectionString("DefaultConnection")),
            configuration["App:PublishPath"] ?? "não configurado",
            events);
        return View(model);
    }
}

public sealed record AdminEnvironmentViewModel(string HostingMode, string Environment, string Version, string DatabaseStatus, bool TelegramEnabled, bool WhatsAppEnabled, string MaskedConnectionString, string PublishPath, IReadOnlyList<object> RecentDeploymentEvents);
