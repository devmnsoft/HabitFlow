using HabitFlow.Web.Middleware;

namespace HabitFlow.Web.Configuration;

public static class MiddlewareConfig
{
    public static WebApplication UseHabitFlowPipeline(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<ClientBindingMiddleware>();
        app.UseMiddleware<AccountStatusMiddleware>();
        app.UseMiddleware<RequiredPasswordChangeMiddleware>();
        app.UseMiddleware<SuperAdminMfaMiddleware>();
        app.MapControllerRoute("login", "login", new { controller = "Auth", action = "Login" });
        app.MapControllerRoute("register", "register", new { controller = "Auth", action = "Register" });
        app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
        app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
        return app;
    }
}
