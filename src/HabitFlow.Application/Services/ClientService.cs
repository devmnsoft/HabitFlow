using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace HabitFlow.Application;

public sealed class ClientService(IClientRepository repo, AuditService audit, ILogger<ClientService> logger)
{
    public async Task<Result<Client>> CreateAsync(CreateClientRequest request, User adminUser, CancellationToken ct)
    {
        var validation = await ValidateAsync(request, null, ct); if (validation is not null) return Result<Client>.Failure("client.validation", validation);
        try
        {
            var now = DateTime.UtcNow;
            var client = new Client(Guid.NewGuid(), Clean(request.Name)!, Clean(request.LegalName), Clean(request.Document), Clean(request.Email), Clean(request.Phone), Clean(request.ContactName), request.Plan, request.Status, Clean(request.Notes), request.Status == ClientStatus.Active, now, now);
            await repo.CreateAsync(client, ct);
            await audit.LogAsync("client_created", $"Cliente criado por admin {adminUser.Id}.", AuditSeverity.Info, adminUser.Id, adminUser.Email, new { clientId = client.Id, client.Plan, client.Status }, ct);
            return Result<Client>.Success(client);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao criar cliente"); return Result<Client>.Failure("database", "Não foi possível acessar os dados agora. Tente novamente em instantes."); }
    }
    public async Task<Result<Client>> UpdateAsync(Guid id, UpdateClientRequest request, User adminUser, CancellationToken ct)
    {
        var existing = await repo.GetByIdAsync(id, ct); if (existing is null) return Result<Client>.Failure("client.not_found", "Cliente não encontrado.");
        var validation = await ValidateAsync(request, id, ct); if (validation is not null) return Result<Client>.Failure("client.validation", validation);
        try
        {
            var updated = existing with { Name = Clean(request.Name)!, LegalName = Clean(request.LegalName), Document = Clean(request.Document), Email = Clean(request.Email), Phone = Clean(request.Phone), ContactName = Clean(request.ContactName), Plan = request.Plan, Status = request.Status, Notes = Clean(request.Notes), IsActive = request.Status == ClientStatus.Active, UpdatedAt = DateTime.UtcNow };
            await repo.UpdateAsync(updated, ct);
            await audit.LogAsync("client_updated", $"Cliente atualizado por admin {adminUser.Id}.", AuditSeverity.Info, adminUser.Id, adminUser.Email, new { clientId = id, updated.Plan, updated.Status }, ct);
            return Result<Client>.Success(updated);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao atualizar cliente {ClientId}", id); return Result<Client>.Failure("database", "Não foi possível acessar os dados agora. Tente novamente em instantes."); }
    }
    public async Task<Result<ClientDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    { try { var c = await repo.GetByIdAsync(id, ct); if (c is null) return Result<ClientDetailDto>.Failure("client.not_found", "Cliente não encontrado."); return Result<ClientDetailDto>.Success(new(c, await repo.GetUsersAsync(id, ct), await repo.GetMetricsAsync(id, ct))); } catch (Exception ex) { logger.LogError(ex, "Erro ao carregar cliente"); return Result<ClientDetailDto>.Failure("database", "Não foi possível acessar os dados agora. Tente novamente em instantes."); } }
    public async Task<Result<IReadOnlyList<ClientListItemDto>>> SearchAsync(ClientFilter filter, CancellationToken ct)
    { try { var items = await repo.SearchAsync(filter.Search, filter.Status, filter.Plan, Math.Max(filter.Page - 1, 0) * Math.Clamp(filter.PageSize, 1, 100), Math.Clamp(filter.PageSize, 1, 100), ct); return Result<IReadOnlyList<ClientListItemDto>>.Success(items.Select(c => new ClientListItemDto(c.Id,c.Name,c.Document,c.Email,c.Plan,c.Status,c.IsActive,c.CreatedAt)).ToList()); } catch (Exception ex) { logger.LogError(ex, "Erro ao buscar clientes"); return Result<IReadOnlyList<ClientListItemDto>>.Failure("database", "Não foi possível acessar os dados agora. Tente novamente em instantes."); } }
    public Task<Result<Client>> ActivateAsync(Guid id, User adminUser, CancellationToken ct) => ChangeStatusAsync(id, ClientStatus.Active, "client_activated", adminUser, null, ct);
    public Task<Result<Client>> DeactivateAsync(Guid id, User adminUser, string reason, CancellationToken ct) => string.IsNullOrWhiteSpace(reason) ? Task.FromResult(Result<Client>.Failure("client.reason", "Informe o motivo.")) : ChangeStatusAsync(id, ClientStatus.Inactive, "client_deactivated", adminUser, reason, ct);
    public Task<Result<Client>> BlockAsync(Guid id, User adminUser, string reason, CancellationToken ct) => string.IsNullOrWhiteSpace(reason) ? Task.FromResult(Result<Client>.Failure("client.reason", "Informe o motivo.")) : ChangeStatusAsync(id, ClientStatus.Blocked, "client_blocked", adminUser, reason, ct);
    private async Task<Result<Client>> ChangeStatusAsync(Guid id, ClientStatus status, string action, User adminUser, string? reason, CancellationToken ct) { var d = await GetByIdAsync(id, ct); if (d.IsFailure || d.Value is null) return Result<Client>.Failure(d.Error.Code, d.Error.Message); var c = d.Value.Client with { Status = status, IsActive = status == ClientStatus.Active, UpdatedAt = DateTime.UtcNow }; try { await repo.UpdateAsync(c, ct); await audit.LogAsync(action, $"Status de cliente alterado por admin {adminUser.Id}.", AuditSeverity.Warning, adminUser.Id, adminUser.Email, new { clientId = id, status, reason }, ct); return Result<Client>.Success(c); } catch(Exception ex) { logger.LogError(ex,"Erro status cliente"); return Result<Client>.Failure("database", "Não foi possível acessar os dados agora. Tente novamente em instantes."); } }
    private async Task<string?> ValidateAsync(CreateClientRequest r, Guid? id, CancellationToken ct) { if (string.IsNullOrWhiteSpace(r.Name)) return "Informe o nome do cliente."; if (r.Name.Length > 180) return "O nome deve ter no máximo 180 caracteres."; if (!string.IsNullOrWhiteSpace(r.Email) && !Regex.IsMatch(r.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return "Informe um e-mail válido."; if (!string.IsNullOrWhiteSpace(r.Phone) && r.Phone.Length > 40) return "O telefone deve ter no máximo 40 caracteres."; if (!string.IsNullOrWhiteSpace(r.Document) && (r.Document.Length < 3 || r.Document.Length > 30)) return "Informe um documento válido."; if (!string.IsNullOrWhiteSpace(r.Document) && await repo.DocumentExistsAsync(r.Document.Trim(), id, ct)) return "Já existe um cliente com este documento."; return null; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
