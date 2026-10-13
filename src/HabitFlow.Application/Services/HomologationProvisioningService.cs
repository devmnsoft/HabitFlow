using System.Security.Cryptography;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

/// <summary>
/// Provisionamento idempotente de contas de homologação, uma por plano (§6).
/// Somente Development (o controller bloqueia qualquer outro ambiente com 404).
/// - E-mails fixos por plano: a segunda execução encontra a conta existente e NÃO
///   altera senha, onboarding ou assinatura (rerun-safe).
/// - Senha gerada com RNG criptográfico apenas na criação (ou em reset explícito),
///   persistida somente como hash BCrypt e exibida UMA vez ao operador na tela de
///   resultado — nunca em logs, PR ou documentos públicos.
/// - Cada conta pertence a um cliente (isolamento de tenant via client_id):
///   Free usa o cliente Demo, Enterprise usa o MNSOFT e Premium usa um cliente de
///   teste dedicado, claramente identificado como ambiente de teste.
/// - A assinatura de teste é identificável (contracted_plan_name "Homologação ...").
/// </summary>
public sealed class HomologationProvisioningService(
    IClientRepository clients,
    IUserRepository users,
    ISubscriptionRepository subscriptions,
    IPasswordHasher hasher,
    ClientOnboardingService onboarding,
    AuditService audit,
    IUnitOfWork unitOfWork,
    ILogger<HomologationProvisioningService> logger)
{
    private static readonly Guid PlatformTenantId = Guid.Parse("61950000-0000-4000-8000-000000000001");
    private const string TestCpf = "11144477735"; // CPF de teste válido (dígitos verificadores OK).

    public sealed record HomologationAccountSpec(
        ClientPlan Plan,
        string Email,
        string UserName,
        string SubscriptionPlanCode,
        bool UsesTrial);

    public static readonly IReadOnlyList<HomologationAccountSpec> Specs =
    [
        new(ClientPlan.Free, "homolog.free@habitflow.test", "Homologação Free", "free_monthly", false),
        new(ClientPlan.Premium, "homolog.premium@habitflow.test", "Homologação Premium", "premium_monthly", true),
        new(ClientPlan.Enterprise, "homolog.enterprise@habitflow.test", "Homologação Enterprise", "enterprise_yearly", true),
    ];

    public sealed record ProvisionedAccount(
        string Email,
        ClientPlan Plan,
        Guid ClientId,
        string ClientName,
        Guid UserId,
        bool Created,
        string? OneTimePassword,
        string SubscriptionStatus,
        DateTime ProvisionedAt);

    public async Task<IReadOnlyList<ProvisionedAccount>> ProvisionAllAsync(Guid actorUserId, string actorEmail, CancellationToken ct = default)
    {
        var results = new List<ProvisionedAccount>();
        foreach (var spec in Specs)
            results.Add(await ProvisionOneAsync(spec, actorUserId, actorEmail, resetPassword: false, ct));
        return results;
    }

    public async Task<ProvisionedAccount> ResetPasswordAsync(string email, Guid actorUserId, string actorEmail, CancellationToken ct = default)
    {
        var spec = Specs.SingleOrDefault(s => string.Equals(s.Email, email, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Conta de homologação desconhecida.");
        return await ProvisionOneAsync(spec, actorUserId, actorEmail, resetPassword: true, ct);
    }

    public async Task<IReadOnlyList<HomologationAccountStatus>> ListStatusAsync(CancellationToken ct = default)
    {
        var list = new List<HomologationAccountStatus>();
        foreach (var spec in Specs)
        {
            var user = await users.GetByEmailAsync(spec.Email, ct);
            var sub = user is null ? null : await subscriptions.GetActiveOrLatestByUserIdAsync(user.Id, ct);
            list.Add(new HomologationAccountStatus(
                spec.Email, spec.Plan,
                user?.ClientId, user?.Id, user is not null,
                user?.OnboardingCompleted ?? false,
                sub?.Status.ToString() ?? "—",
                sub?.TrialEndsAt));
        }
        return list;
    }

    public sealed record HomologationAccountStatus(
        string Email, ClientPlan Plan, Guid? ClientId, Guid? UserId, bool Exists,
        bool OnboardingCompleted, string SubscriptionStatus, DateTime? TrialEndsAt);

    private async Task<ProvisionedAccount> ProvisionOneAsync(HomologationAccountSpec spec, Guid actorUserId, string actorEmail, bool resetPassword, CancellationToken ct)
    {
        var client = await ResolveClientAsync(spec, ct);
        var existing = await users.GetByEmailAsync(spec.Email, ct);
        string? oneTimePassword = null;

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            User user;
            bool created;
            if (existing is null)
            {
                oneTimePassword = GeneratePassword();
                var now = DateTime.UtcNow;
                user = new User(Guid.NewGuid(), spec.UserName, spec.Email, hasher.Hash(oneTimePassword),
                    null, UserRole.Admin, AccountStatus.Active, RiskStatus.Normal,
                    spec.Plan == ClientPlan.Free ? UserPlan.Free : UserPlan.Premium,
                    spec.UsesTrial ? PlanStatus.Trial : PlanStatus.Active,
                    false, false, now, now, null, null, now, now, client.Id);
                await users.CreateAsync(user, ct);
                created = true;
            }
            else
            {
                user = existing;
                created = false;
                if (resetPassword)
                {
                    oneTimePassword = GeneratePassword();
                    await users.UpdatePasswordAndSessionVersionAsync(user.Id, hasher.Hash(oneTimePassword), ct);
                }
            }

            var subscription = await subscriptions.GetActiveOrLatestByUserIdAsync(user.Id, ct);
            if (subscription is null)
            {
                var now = DateTime.UtcNow;
                var trialEnd = spec.UsesTrial ? now.AddDays(15) : (DateTime?)null;
                await subscriptions.CreateAsync(new Subscription(Guid.NewGuid(), user.Id, spec.SubscriptionPlanCode,
                    spec.UsesTrial ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
                    BillingCycle.Monthly, PaymentProvider.Manual, null, null, null, null,
                    now, spec.UsesTrial ? trialEnd : now.AddYears(1), trialEnd, null, now, now), ct);
                subscription = await subscriptions.GetActiveOrLatestByUserIdAsync(user.Id, ct);
            }

            await onboarding.GetOrCreateAsync(client.Id, ct);
            await unitOfWork.CommitAsync(ct);

            // Auditoria sem nenhum segredo: apenas e-mail mascarado, plano e resultado.
            await audit.LogAsync("homologation.account_provisioned",
                $"Conta de homologação {spec.Plan} provisionada (created={created}, reset={resetPassword})",
                AuditSeverity.Info, actorUserId, actorEmail,
                new { email = MaskEmail(spec.Email), plan = spec.Plan.ToString(), clientId = client.Id, created, passwordReset = resetPassword }, ct);
            logger.LogInformation("homologation.provisioned Plan={Plan} ClientId={ClientId} Created={Created}", spec.Plan, client.Id, created);

            return new ProvisionedAccount(spec.Email, spec.Plan, client.Id, client.Name, user.Id, created,
                oneTimePassword, subscription?.Status.ToString() ?? "—", DateTime.UtcNow);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<Client> ResolveClientAsync(HomologationAccountSpec spec, CancellationToken ct)
    {
        if (spec.Plan == ClientPlan.Enterprise)
        {
            var platform = await clients.GetByIdAsync(PlatformTenantId, ct);
            if (platform is not null) return platform;
        }

        var byPlan = await clients.SearchAsync(null, ClientStatus.Active, spec.Plan, 0, 5, ct: ct);
        var match = byPlan.FirstOrDefault(c => !c.Name.Contains("(TESTE)", StringComparison.OrdinalIgnoreCase))
            ?? byPlan.FirstOrDefault();
        if (match is not null && spec.Plan != ClientPlan.Premium) return match;
        if (match is not null && match.Name.Contains("(TESTE)", StringComparison.OrdinalIgnoreCase)) return match;

        if (spec.Plan == ClientPlan.Premium)
        {
            // Cliente de teste dedicado, claramente identificado, com trial de 15 dias.
            var now = DateTime.UtcNow;
            var testClient = new Client(Guid.NewGuid(), "Homologação Premium (TESTE)", "Homologação Premium (TESTE)",
                "11.444.777-35", "homolog.premium@habitflow.test", null, "Operador Homologação",
                ClientPlan.Premium, ClientStatus.Active, "Cliente de teste criado pelo provisionador de homologação. Não usar em produção.",
                true, now, now, ClientPersonType.NaturalPerson, ClientDocumentType.CPF,
                "111.444.777-35", TestCpf, null, null, null,
                BillingEmail: "homolog.premium@habitflow.test", BillingPhone: null, BillingResponsibleName: "Operador Homologação",
                AddressZipcode: null, AddressStreet: null, AddressNumber: null, AddressComplement: null,
                AddressDistrict: null, AddressCity: null, AddressState: null,
                SubscriptionStatus: ClientSubscriptionStatus.Trial, BenefitsStatus: ClientBenefitsStatus.PremiumActive, PaymentStatus: ClientPaymentStatus.None,
                LastPaymentAt: null, NextDueDate: null, OverdueSince: null, GracePeriodUntil: null,
                BlockedPaidBenefitsAt: null, BlockedPaidBenefitsReason: null);
            await clients.CreateAsync(testClient, ct);
            return testClient;
        }

        match ??= (await clients.SearchAsync(null, ClientStatus.Active, spec.Plan, 0, 5, ct: ct)).FirstOrDefault();
        return match ?? throw new InvalidOperationException($"Nenhum cliente {spec.Plan} encontrado para homologação.");
    }

    private static string GeneratePassword()
    {
        const string alphabet = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789!@#";
        return string.Create(18, alphabet, static (span, chars) =>
        {
            var bytes = RandomNumberGenerator.GetBytes(span.Length);
            for (var i = 0; i < span.Length; i++) span[i] = chars[bytes[i] % chars.Length];
        });
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
