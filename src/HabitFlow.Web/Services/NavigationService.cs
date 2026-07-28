using System.Security.Claims;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class NavigationService
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
            IsActive: true),

        new(
            Code: "demo",
            Label: "Demonstração",
            Description: "Veja como funciona",
            Icon: "demo",
            Url: "/demo",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 20,
            IsActive: true),

        new(
            Code: "library",
            Label: "Biblioteca",
            Description: "Encontre um primeiro passo",
            Icon: "library",
            Url: "/habit-library",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 30,
            IsActive: true),

        new(
            Code: "plans",
            Label: "Planos",
            Description: "Escolha seu ritmo",
            Icon: "premium",
            Url: "/plans",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 40,
            IsActive: true),

        new(
            Code: "help",
            Label: "Ajuda",
            Description: "Encontre respostas",
            Icon: "help",
            Url: "/help",
            Context: NavigationContext.Public,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 50,
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

        // Navegação da conta
        new(
            Code: "account",
            Label: "Sua conta",
            Description: "Acessos e preferências",
            Icon: "profile",
            Url: "/profile",
            Context: NavigationContext.Account,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 10,
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

        new(
            Code: "privacy",
            Label: "Privacidade",
            Description: "Controle seus dados",
            Icon: "privacy",
            Url: "/profile/privacy",
            Context: NavigationContext.Account,
            RequiredPermission: null,
            RequiredFeature: null,
            SortOrder: 60,
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true),

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
            IsActive: true)
    ];

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
            .Where(item => item.Context == context && item.IsActive)
            .Where(item =>
                item.RequiredPermission is null ||
                isSuperAdmin ||
                permissions.Contains(item.RequiredPermission))
            .Select(item => item with
            {
                IsActive = IsCurrent(item.Url, path)
            })
            .OrderBy(item => item.SortOrder)
            .ToArray();
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

        return item?.Url ?? "#";
    }
}