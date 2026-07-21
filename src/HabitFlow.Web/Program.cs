using HabitFlow.Application;
using HabitFlow.Infrastructure;
using HabitFlow.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5097");

builder.Services
    .AddHabitFlowApplication()
    .AddHabitFlowInfrastructure(builder.Configuration)
    .AddHabitFlowWeb(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseHabitFlowPipeline();

app.Run();
