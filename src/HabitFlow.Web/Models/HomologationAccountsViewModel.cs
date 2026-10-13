using HabitFlow.Application;
using static HabitFlow.Application.HomologationProvisioningService;

namespace HabitFlow.Web.Models;

/// <summary>
/// Tela de contas de homologação (§6): lista o estado das 3 contas de teste
/// (uma por plano) e, após um POST de provisionamento/reset, carrega as senhas
/// geradas UMA única vez para entrega ao operador. As senhas nunca são
/// persistidas em texto claro nem registradas em logs.
/// </summary>
public sealed class HomologationAccountsViewModel
{
    public required IReadOnlyList<HomologationAccountStatus> Accounts { get; init; }
    public required IReadOnlyList<ProvisionedAccount> JustProvisioned { get; init; }
}
