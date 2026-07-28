using HabitFlow.Application;
using HabitFlow.Infrastructure;
using HabitFlow.Web.Configuration;
using HabitFlow.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHabitFlowPipeline();

app.Run();

public partial class Program { }
