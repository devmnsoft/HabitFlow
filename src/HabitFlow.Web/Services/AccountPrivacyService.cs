using HabitFlow.Domain;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class AccountPrivacyService(ILgpdRepository repository, HabitFlow.Application.LgpdService requests)
{
    private static readonly (string Key, string Title, string Description)[] ConsentCatalog =
    [
        ("essential", "Dados essenciais", "Necessários para autenticação, segurança e funcionamento da conta."),
        ("analytics", "Métricas de produto", "Ajude a melhorar o HabitFlow com métricas de uso minimizadas."),
        ("communications", "Comunicações", "Receba novidades e orientações sobre o produto.")
    ];

    public async Task<AccountPrivacyViewModel> GetAsync(User user, CancellationToken ct)
    {
        var clientId = user.ClientId ?? throw new InvalidOperationException("Conta sem tenant vinculado.");
        var consents = await repository.ListConsentsAsync(clientId, user.Id, ct);
        var history = await repository.ListByUserAsync(clientId, user.Id, ct);
        var mappedConsents = ConsentCatalog.Select(item =>
        {
            var saved = consents.FirstOrDefault(x => x.ConsentKey == item.Key);
            return new PrivacyConsentViewModel(item.Key, item.Title, item.Description, item.Key == "essential" || saved?.Granted == true, saved?.UpdatedAt);
        }).ToArray();
        var mappedRequests = history.Select(x => new PrivacyRequestViewModel(x.Id, x.Protocol, x.Type.ToString(), x.Status.ToString(), x.CreatedAt)).ToArray();
        return new(user.Name, user.Email, user.CreatedAt, mappedConsents, mappedRequests,
            mappedRequests.Take(8).Select(x => new PrivacyActivityViewModel($"Solicitação {x.Type}", $"Protocolo {x.Protocol} · {x.Status}", x.CreatedAt)).ToArray(),
            new(history.Any(x => x.Type == LgpdRequestType.Export && x.Status is LgpdRequestStatus.Requested or LgpdRequestStatus.InReview or LgpdRequestStatus.Processing)),
            new(history.Any(x => x.Type == LgpdRequestType.Delete && x.Status is LgpdRequestStatus.Requested or LgpdRequestStatus.InReview or LgpdRequestStatus.Processing), history.Any(x => x.Type == LgpdRequestType.Anonymize && x.Status is LgpdRequestStatus.Requested or LgpdRequestStatus.InReview or LgpdRequestStatus.Processing)));
    }

    public Task SetConsentAsync(Guid userId, string key, bool granted, CancellationToken ct)
    {
        if (key is not ("analytics" or "communications")) throw new ArgumentException("Consentimento inválido.", nameof(key));
        return repository.UpsertConsentAsync(new(userId, key, granted, DateTime.UtcNow), ct);
    }

    public Task<HabitFlow.Shared.Result> RequestAsync(User user, LgpdRequestType type, CancellationToken ct) => requests.RequestAsync(user, type, ct);

    public async Task<byte[]> ExportJsonAsync(User user, CancellationToken ct)
    {
        var clientId = user.ClientId ?? throw new InvalidOperationException("Conta sem tenant vinculado.");
        await repository.RecordSecurityEventAsync(clientId, user.Id, "data_export.requested", "Info", ct);
        var json = await repository.ExportOwnedDataJsonAsync(clientId, user.Id, ct);
        await repository.RecordSecurityEventAsync(clientId, user.Id, "data_export.completed", "Info", ct);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
}
