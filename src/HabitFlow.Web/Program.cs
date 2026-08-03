using HabitFlow.Application;
using HabitFlow.Infrastructure;
using HabitFlow.Web.Configuration;
using HabitFlow.Web.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });

if (!builder.Environment.IsProduction())
    builder.Host.UseDefaultServiceProvider(options => { options.ValidateScopes = true; options.ValidateOnBuild = true; });

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
}

builder.WebHost.UseUrls("http://0.0.0.0:5097");

builder.Services
    .AddHabitFlowApplication()
    .AddHabitFlowInfrastructure(builder.Configuration)
    .AddHabitFlowWeb(builder.Configuration, builder.Environment);

builder.Services.AddHostedService<BillingStatusHostedService>();

var app = builder.Build();

if (AdminCli.IsCommand(args))
{
    Environment.ExitCode = await AdminCli.RunAsync(args, app.Services);
    return;
}

app.UseHabitFlowPipeline();

app.Run();

public partial class Program { }
