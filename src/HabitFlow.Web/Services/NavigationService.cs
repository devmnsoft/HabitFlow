using System.Security.Claims;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class NavigationService
{
    private static readonly IReadOnlyList<NavigationItem> Items =
    [
        new("home", "Início", "Conheça o HabitFlow", "home", NavigationContext.Public, null, null, 10, true),
        new("demo", "Demonstração", "Veja como funciona", "demo", NavigationContext.Public, null, null, 20, true),
        new("library", "Biblioteca", "Encontre um primeiro passo", "library", NavigationContext.Public, null, null, 30, true),
        new("plans", "Planos", "Escolha seu ritmo", "premium", NavigationContext.Public, null, null, 40, true),
        new("help", "Ajuda", "Encontre respostas", "help", NavigationContext.Public, null, null, 50, true),
        new("today", "Hoje", "Seu próximo passo", "dashboard", NavigationContext.Personal, null, null, 10, true),
        new("habits", "Hábitos", "Cuide da sua rotina", "habit", NavigationContext.Personal, null, null, 20, true),
        new("goals", "Objetivos", "Veja onde quer chegar", "target", NavigationContext.Personal, null, null, 30, true),
        new("progress", "Progresso", "Acompanhe seu ritmo", "progress", NavigationContext.Personal, null, null, 40, true),
        new("reports", "Relatórios", "Entenda sua evolução", "report", NavigationContext.Personal, null, "reports", 50, true),
        new("account", "Sua conta", "Acessos e preferências", "profile", NavigationContext.Account, null, null, 10, true),
        new("people", "Pessoas", "Pessoas da sua conta", "users", NavigationContext.Account, null, "people", 20, true),
        new("invites", "Convites", "Convide com segurança", "invite", NavigationContext.Account, null, "people", 30, true),
        new("my-plan", "Meu plano", "Acesso, limites e opções", "premium", NavigationContext.Account, null, null, 40, true),
        new("payments", "Pagamentos", "Faturas e confirmações", "billing", NavigationContext.Account, null, null, 50, true),
        new("privacy", "Privacidade", "Controle seus dados", "privacy", NavigationContext.Account, null, null, 60, true),
        new("support", "Suporte", "Fale com a gente", "help", NavigationContext.Account, null, null, 70, true),
        new("platform", "Visão geral", "Saúde da plataforma", "dashboard", NavigationContext.Platform, "platform.view", null, 10, true),
        new("clients", "Clientes", "Contas e acessos", "organization", NavigationContext.Platform, "platform.clients", null, 20, true),
        new("users", "Usuários", "Pessoas e perfis", "users", NavigationContext.Platform, "platform.users", null, 30, true),
        new("platform-plans", "Planos", "Catálogo publicado", "premium", NavigationContext.Platform, "platform.plans", null, 40, true),
        new("subscriptions", "Assinaturas", "Ciclos em andamento", "calendar", NavigationContext.Platform, "platform.billing", null, 50, true),
        new("platform-payments", "Pagamentos", "Confirmações financeiras", "billing", NavigationContext.Platform, "platform.billing", null, 60, true),
        new("overdue", "Inadimplência", "Acessos reduzidos", "warning", NavigationContext.Platform, "platform.billing", null, 70, true),
        new("platform-support", "Suporte", "Atendimentos", "help", NavigationContext.Platform, "platform.support", null, 80, true),
        new("audit", "Auditoria", "Ações administrativas", "report", NavigationContext.Platform, "platform.audit", null, 90, true),
        new("system", "Sistema", "Operação e jobs", "settings", NavigationContext.Platform, "platform.system", null, 100, true)
    ];

    public IReadOnlyList<NavigationItem> Get(NavigationContext context, ClaimsPrincipal user, string path)
    {
        var permissions = user.FindAll("permission").Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var isSuperAdmin = user.IsInRole("SuperAdmin");
        return Items.Where(x => x.Context == context && x.IsActive)
            .Where(x => x.RequiredPermission is null || isSuperAdmin || permissions.Contains(x.RequiredPermission))
            .Select(x => x with { IsActive = IsCurrent(x.Url, path) }).OrderBy(x => x.SortOrder).ToArray();
    }

    public bool HasPlatformAccess(ClaimsPrincipal user) => user.IsInRole("SuperAdmin") || user.HasClaim("permission", "platform.view");

    private static bool IsCurrent(string url, string path) => url == "/" ? path == "/" : path.StartsWith(url, StringComparison.OrdinalIgnoreCase);

    public static string ResolveUrl(string code) => code switch
    {
        "home" => "/", "demo" => "/demo", "library" => "/habit-library", "plans" => "/plans", "help" => "/help",
        "today" => "/dashboard", "habits" => "/habits", "goals" => "/goals", "progress" => "/progress/calendar", "reports" => "/reports",
        "account" => "/profile", "people" => "/account/people", "invites" => "/account/invites", "my-plan" => "/account/plan",
        "payments" => "/billing", "privacy" => "/profile/privacy", "support" => "/support",
        "platform" => "/superadmin", "clients" => "/superadmin/clients", "users" => "/superadmin/users", "platform-plans" => "/superadmin/plans",
        "subscriptions" => "/superadmin/subscriptions", "platform-payments" => "/superadmin/payments", "overdue" => "/superadmin/overdue",
        "platform-support" => "/superadmin/support", "audit" => "/superadmin/audit", "system" => "/superadmin/system-health", _ => "#"
    };
}
