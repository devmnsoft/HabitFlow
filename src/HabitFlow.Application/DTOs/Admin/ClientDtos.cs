using HabitFlow.Domain;
using System.ComponentModel.DataAnnotations;

namespace HabitFlow.Application;

public abstract class ClientRequestBase
{
    [Required(ErrorMessage = "Informe o nome do cliente.")]
    [StringLength(180, ErrorMessage = "O nome deve ter no máximo 180 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(220)]
    public string? LegalName { get; set; }

    [StringLength(30, MinimumLength = 3, ErrorMessage = "Informe um documento válido.")]
    public string? Document { get; set; }

    [Required]
    public ClientPersonType PersonType { get; set; } = ClientPersonType.LegalPerson;

    [Required]
    public ClientDocumentType DocumentType { get; set; } = ClientDocumentType.CNPJ;

    [StringLength(180)]
    public string? TradeName { get; set; }

    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(200)]
    public string? BillingEmail { get; set; }

    [StringLength(40)]
    public string? BillingPhone { get; set; }

    [StringLength(160)]
    public string? BillingResponsibleName { get; set; }

    [StringLength(20)] public string? AddressZipcode { get; set; }
    [StringLength(200)] public string? AddressStreet { get; set; }
    [StringLength(40)] public string? AddressNumber { get; set; }
    [StringLength(120)] public string? AddressComplement { get; set; }
    [StringLength(120)] public string? AddressDistrict { get; set; }
    [StringLength(120)] public string? AddressCity { get; set; }
    [StringLength(2)] public string? AddressState { get; set; }

    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(200)]
    public string? Email { get; set; }

    [StringLength(40, ErrorMessage = "O telefone deve ter no máximo 40 caracteres.")]
    public string? Phone { get; set; }

    [StringLength(160)]
    public string? ContactName { get; set; }

    [Required]
    public ClientPlan Plan { get; set; } = ClientPlan.Free;

    [Required]
    public ClientStatus Status { get; set; } = ClientStatus.Active;

    public string? Notes { get; set; }
}

public sealed class CreateClientRequest : ClientRequestBase
{
}

public sealed class UpdateClientRequest : ClientRequestBase
{
}

public sealed record ClientListItemDto(Guid Id, string Name, string? Document, string? Email, ClientPlan Plan, ClientStatus Status, bool IsActive, DateTime CreatedAt);

public sealed record ClientEntitlementsDto(Guid ClientId, ClientPlan Plan, ClientBenefitsStatus BenefitsStatus, ClientSubscriptionStatus SubscriptionStatus, ClientPaymentStatus PaymentStatus, int FreeHabitLimit, bool AdvancedReportsEnabled, bool PremiumLibraryEnabled);

public sealed record ClientDetailDto(Client Client, IReadOnlyList<ClientUserSummary> Users, ClientMetrics Metrics);

public sealed class ClientFilter
{
    public string? Search { get; set; }
    public ClientStatus? Status { get; set; }
    public ClientPlan? Plan { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
