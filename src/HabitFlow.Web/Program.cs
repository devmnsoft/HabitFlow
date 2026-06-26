using HabitFlow.Application; using HabitFlow.Infrastructure; using HabitFlow.Web.Middleware; using Microsoft.AspNetCore.Authentication.Cookies;
var builder=WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["App:Urls"]??"http://localhost:5097");
builder.Services.AddControllersWithViews(); builder.Services.AddHealthChecks(); builder.Services.AddInfrastructure();
builder.Services.AddScoped<AuthService>(); builder.Services.AddSingleton<IPasswordHasher,BCryptPasswordHasher>(); builder.Services.AddSingleton<ProgressService>(); builder.Services.AddSingleton<HabitPolicy>(); builder.Services.AddSingleton<ProtocolGenerator>(); builder.Services.AddSingleton<LogSanitizer>(); builder.Services.AddSingleton<WhatsAppValidator>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{o.Cookie.Name=builder.Configuration["Security:CookieName"]??"HabitFlow.Auth"; o.Cookie.HttpOnly=true; o.Cookie.SameSite=SameSiteMode.Lax; o.LoginPath="/login"; o.LogoutPath="/logout"; o.SlidingExpiration=true;});
builder.Services.AddAuthorization(o=>o.AddPolicy("AdminOnly",p=>p.RequireRole("Admin")));
var app=builder.Build(); app.UseMiddleware<GlobalExceptionMiddleware>(); if(!app.Environment.IsDevelopment()) app.UseHsts(); app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.MapHealthChecks("/health"); app.MapControllerRoute("default","{controller=Home}/{action=Index}/{id?}"); app.Run();
