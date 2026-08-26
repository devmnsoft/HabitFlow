using System.Security.Claims;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class NavigationService(INavigationAccessEvaluator? accessEvaluator = null)
{
    private static readonly IReadOnlyList<NavigationItem> Items =
    [
        // Navegação pública
        new(
            Code: "home",
            Label: "Início",
            Description: "Conheça o HabitFlow",
            Icon: "home",
            Url: "/",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 10,
            IsEnabled: true),

        new(
            Code: "demo",
            Label: "Como funciona",
            Description: "Veja o HabitFlow em ação",
            Icon: "demo",
            Url: "/demo",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 20,
            IsEnabled: true),

        new(
            Code: "library",
            Label: "Biblioteca",
            Description: "Ideias prontas para começar",
            Icon: "library",
            Url: "/habit-library",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 30,
            IsEnabled: true),

        new(
            Code: "plans",
            Label: "Planos",
            Description: "Comece grátis ou avance no Ritmo",
            Icon: "premium",
            Url: "/plans",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 40,
            IsEnabled: true),

        new(
            Code: "help",
            Label: "Ajuda",
            Description: "Encontre respostas",
            Icon: "help",
            Url: "/help",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 60,
            IsEnabled: true),

        new("public-support", "Suporte", "Fale com a nossa equipe", "help", "/support",
            NavigationContext.Public, null, null, 70, true),

        new("public-privacy", "Privacidade", "Seus dados, suas escolhas", "privacy", "/privacy",
            NavigationContext.Public, null, null, 50, true),

        // Navegação pessoal
        new(
            Code: "today",
            Label: "Hoje",
            Description: "Seu próximo passo",
            Icon: "dashboard",
            Url: "/dashboard",
            Context: NavigationContext.Personal,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 10,
            IsEnabled: true),

        new(
            Code: "my-day",
            Label: "Meu dia",
            Description: "Organize seus próximos passos",
            Icon: "dashboard",
            Url: "/my-day",
            Context: NavigationContext.Personal,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 15,
            IsEnabled: true),

        new(
            Code: "weekly-review",
            Label: "Revisão",
            Description: "Aprenda com sua semana",
            Icon: "progress",
            Url: "/weekly-review",
            Context: NavigationContext.Personal,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 18,
            IsEnabled: true),

        new(
            Code: "habits",
            Label: "Hábitos",
            Description: "Cuide da sua rotina",
            Icon: "habit",
            Url: "/habits",
            Context: NavigationContext.Personal,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 20,
            IsEnabled: true),

        new(
            Code: "goals",
            Label: "Objetivos",
            Description: "Veja onde quer chegar",
            Icon: "target",
            Url: "/goals",
            Context: NavigationContext.Personal,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 30,
            IsEnabled: true),

        new(
            Code: "progress",
            Label: "Progresso",
            Description: "Acompanhe seu ritmo",
            Icon: "progress",
            Url: "/progress/calendar",
            Context: NavigationContext.Personal,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 40,
            IsEnabled: true),

        new(
            Code: "reports",
            Label: "Relatórios",
            Description: "Entenda sua evolução",
            Icon: "report",
            Url: "/reports",
            Context: NavigationContext.Personal,
            RequiredPermission: null,
            RequiredFeature: "basic_reports",
            SortOrder: 50,
            IsEnabled: true),

        new(
            Code: "analytics",
            Label: "Minha evolução",
            Description: "Visualize consistência e tendências",
            Icon: "progress",
            Url: "/analytics",
            Context: NavigationContext.Personal,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 45,
            IsEnabled: true),

        new(
            Code: "personal-library", Label: "Biblioteca", Description: "Ideias prontas para começar", Icon: "library", Url: "/habit-library",
            Context: NavigationContext.Personal, RequiredPermission: null, RequiredFeature: null, SortOrder: 60, IsEnabled: true),

        new(
            Code: "challenges", Label: "Desafios", Description: "Um passo por dia", Icon: "progress", Url: "/challenges",
            Context: NavigationContext.Personal, RequiredPermission: null, RequiredFeature: null, SortOrder: 65, IsEnabled: true),

        new(
            Code: "reminders", Label: "Lembretes", Description: "Organize os avisos da sua rotina", Icon: IconCodes.Reminders, Url: "/reminders",
            Context: NavigationContext.Personal, RequiredPermission: null, RequiredFeature: null, SortOrder: 70, IsEnabled: true),

        new(
            Code: "notifications", Label: "Notificações", Description: "Acompanhe novidades e próximos passos", Icon: IconCodes.Notifications, Url: "/notifications",
            Context: NavigationContext.Personal, RequiredPermission: null, RequiredFeature: null, SortOrder: 80, IsEnabled: true),

        // Navegação da conta
        new(
            Code: "account",
            Label: "Minha conta",
            Description: "Acessos e preferências",
            Icon: "profile",
            Url: "/profile",
            Context: NavigationContext.Account,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 10,
            IsEnabled: true),

        new(
            Code: "security", Label: "Segurança", Description: "Senha, sessões e acessos", Icon: "privacy", Url: "/account/security",
            Context: NavigationContext.Account, RequiredPermission: null, RequiredFeature: null, SortOrder: 15, IsEnabled: true),

        new(
            Code: "preferences", Label: "Preferências", Description: "Acessibilidade e experiência", Icon: "settings", Url: "/profile/accessibility",
            Context: NavigationContext.Account, RequiredPermission: null, RequiredFeature: null, SortOrder: 65, IsEnabled: true),

        new(
            Code: "people",
            Label: "Pessoas",
            Description: "Pessoas da sua conta",
            Icon: "users",
            Url: "/account/people",
            Context: NavigationContext.Account,
            RequiredPermission: "Client.Users.Manage",
            RequiredFeature: "user_invitations",
            SortOrder: 20,
            IsEnabled: true),

        new(
            Code: "invites",
            Label: "Convites",
            Description: "Convide com segurança",
            Icon: "invite",
            Url: "/account/invites",
            Context: NavigationContext.Account,
            RequiredPermission: "Client.Users.Manage",
            RequiredFeature: "user_invitations",
            SortOrder: 30,
            IsEnabled: true),

        new(
            Code: "my-plan",
            Label: "Meu plano",
            Description: "Acesso, limites e opções",
            Icon: "premium",
            Url: "/account/plan",
            Context: NavigationContext.Account,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 40,
            IsEnabled: true),

        new(
            Code: "payments",
            Label: "Pagamentos",
            Description: "Faturas e confirmações",
            Icon: "billing",
            Url: "/billing",
            Context: NavigationContext.Account,
            RequiredPermission: "Client.Billing.View",
            RequiredFeature: null,
            SortOrder: 50,
            IsEnabled: true),

        new(
            Code: "privacy",
            Label: "Privacidade",
            Description: "Controle seus dados",
            Icon: "privacy",
            Url: "/account/privacy",
            Context: NavigationContext.Account,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 60,
            IsEnabled: true),

        new(
            Code: "support",
            Label: "Suporte",
            Description: "Fale com a gente",
            Icon: "help",
            Url: "/support",
            Context: NavigationContext.Account,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 70,
            IsEnabled: true),

        // Navegação da plataforma
        new(
            Code: "platform",
            Label: "Visão geral",
            Description: "Saúde da plataforma",
            Icon: "dashboard",
            Url: "/superadmin",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Clients.View",
            RequiredFeature: null,
            SortOrder: 10,
            IsEnabled: true),

        new(
            Code: "clients",
            Label: "Clientes",
            Description: "Contas e acessos",
            Icon: "organization",
            Url: "/superadmin/clients",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Clients.View",
            RequiredFeature: null,
            SortOrder: 20,
            IsEnabled: true),

        new(
            Code: "users",
            Label: "Usuários",
            Description: "Pessoas e perfis",
            Icon: "users",
            Url: "/superadmin/users",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Users.View",
            RequiredFeature: null,
            SortOrder: 30,
            IsEnabled: true),

        new(
            Code: "platform-plans",
            Label: "Planos",
            Description: "Catálogo publicado",
            Icon: "premium",
            Url: "/superadmin/plans",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Plans.View",
            RequiredFeature: null,
            SortOrder: 40,
            IsEnabled: true),

        new(
            Code: "subscriptions",
            Label: "Assinaturas",
            Description: "Ciclos em andamento",
            Icon: "calendar",
            Url: "/superadmin/subscriptions",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Billing.View",
            RequiredFeature: null,
            SortOrder: 50,
            IsEnabled: true),

        new(
            Code: "platform-payments",
            Label: "Pagamentos",
            Description: "Confirmações financeiras",
            Icon: "billing",
            Url: "/superadmin/payments",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Billing.View",
            RequiredFeature: null,
            SortOrder: 60,
            IsEnabled: true),

        new(
            Code: "overdue",
            Label: "Inadimplência",
            Description: "Acessos reduzidos",
            Icon: "warning",
            Url: "/superadmin/overdue",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Billing.View",
            RequiredFeature: null,
            SortOrder: 70,
            IsEnabled: true),

        new(
            Code: "platform-support",
            Label: "Suporte",
            Description: "Atendimentos",
            Icon: "help",
            Url: "/superadmin/support",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Support.Manage",
            RequiredFeature: null,
            SortOrder: 80,
            IsEnabled: true),

        new(
            Code: "audit",
            Label: "Auditoria",
            Description: "Ações administrativas",
            Icon: "report",
            Url: "/superadmin/audit",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Audit.View",
            RequiredFeature: null,
            SortOrder: 90,
            IsEnabled: true),

        new(
            Code: "system",
            Label: "Sistema",
            Description: "Operação e jobs",
            Icon: "settings",
            Url: "/superadmin/system-health",
            Context: NavigationContext.Platform,
            RequiredPermission: "Platform.Settings.Manage",
            RequiredFeature: null,
            SortOrder: 100,
            IsEnabled: true)
    ];

    public static IReadOnlyList<NavigationItem> Definitions => Items;

    public IReadOnlyList<NavigationItem> Get(
        NavigationContext context,
        ClaimsPrincipal user,
        string path)
    {
        ArgumentNullException.ThrowIfNull(user);

        var permissions = user
            .FindAll("permission")
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isSuperAdmin = user.IsInRole("SuperAdmin");

        return Items
            .Where(item => item.Context == context && item.IsEnabled)
            .Where(item =>
                item.RequiredPermission is null ||
                isSuperAdmin ||
                permissions.Contains(item.RequiredPermission))
            .Select(item => item with
            {
                IsCurrent = IsCurrent(item.Url, path)
            })
            .OrderBy(item => item.SortOrder)
            .ToArray();
    }

    /// <summary>Builds navigation after evaluating permissions and effective-plan features.</summary>
    public async Task<IReadOnlyList<NavigationItem>> GetAsync(
        NavigationContext context,
        ClaimsPrincipal user,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (context is NavigationContext.Personal or NavigationContext.Account &&
            user.Identity?.IsAuthenticated != true)
        {
            return [];
        }

        var candidates = Items.Where(item => item.Context == context && item.IsEnabled);
        var visible = new List<NavigationItem>();
        foreach (var item in candidates)
        {
            var allowed = accessEvaluator is null
                ? HasClaimAccess(user, item.RequiredPermission) && item.RequiredFeature is null
                : await accessEvaluator.CanAccessAsync(user, item.RequiredPermission, item.RequiredFeature, cancellationToken);

            // Account plan is the safe, authenticated escape hatch even when paid access changes.
            if ((allowed || item.Code == "my-plan") &&
                (context == NavigationContext.Public || user.Identity?.IsAuthenticated == true))
            {
                visible.Add(item with { IsCurrent = IsCurrent(item.Url, path) });
            }
        }

        return visible.OrderBy(item => item.SortOrder).ToArray();
    }

    public bool HasPlatformAccess(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.IsInRole("SuperAdmin") ||
               user.HasClaim("permission", "Platform.FullAccess") ||
               user.HasClaim("permission", "Platform.Clients.View") ||
               user.HasClaim("permission", "Platform.Billing.View") ||
               user.HasClaim("permission", "Platform.Support.Manage") ||
               user.HasClaim("permission", "Platform.Audit.View") ||
               user.HasClaim("permission", "Platform.Settings.Manage");
    }

    private static bool HasClaimAccess(ClaimsPrincipal user, string? permission) =>
        permission is null || user.IsInRole("SuperAdmin") ||
        user.Claims.Any(claim => claim.Type == "permission" &&
            (claim.Value.Equals(permission, StringComparison.OrdinalIgnoreCase) ||
             claim.Value.Equals("Platform.FullAccess", StringComparison.OrdinalIgnoreCase) ||
             claim.Value.Equals("platform.view", StringComparison.OrdinalIgnoreCase)));

    private static bool IsCurrent(string url, string path)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (url == "/")
        {
            return path == "/";
        }

        return path.Equals(url, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(
                   url.TrimEnd('/') + "/",
                   StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveUrl(string code)
    {
        var item = Items.FirstOrDefault(
            navigationItem =>
                navigationItem.Code.Equals(
                    code,
                    StringComparison.OrdinalIgnoreCase));

        return item?.Url ?? "/";
    }
}
