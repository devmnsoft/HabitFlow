using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class ClientRepository(SqlExecutor db) : IClientRepository
{
    private const string Columns = "id, name, legal_name, document, email, phone, contact_name, plan, status, notes, is_active, created_at, updated_at";

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<Client>($"select {Columns} from habitflow.clients where id = @id", new { id }, ct);

    public async Task<IReadOnlyList<Client>> SearchAsync(string? search, ClientStatus? status, ClientPlan? plan, int offset, int pageSize, CancellationToken ct = default) =>
        (await db.QueryAsync<Client>($"""
            select {Columns}
            from habitflow.clients
            where (@search is null or name ilike @like or email ilike @like or document ilike @like)
              and (@status is null or status = @status)
              and (@plan is null or plan = @plan)
            order by created_at desc
            offset @offset limit @pageSize
            """, new { search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(), like = $"%{search?.Trim()}%", status = status?.ToString(), plan = plan?.ToString(), offset, pageSize }, ct)).ToList();

    public Task CreateAsync(Client c, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.clients(id,name,legal_name,document,email,phone,contact_name,plan,status,notes,is_active,created_at,updated_at)
        values(@Id,@Name,@LegalName,@Document,@Email,@Phone,@ContactName,@Plan,@Status,@Notes,@IsActive,@CreatedAt,@UpdatedAt)
        """, new { c.Id, c.Name, c.LegalName, c.Document, c.Email, c.Phone, c.ContactName, Plan = DbEnum.Text(c.Plan), Status = DbEnum.Text(c.Status), c.Notes, c.IsActive, c.CreatedAt, c.UpdatedAt }, ct);

    public Task UpdateAsync(Client c, CancellationToken ct = default) => db.ExecuteAsync("""
        update habitflow.clients
        set name=@Name, legal_name=@LegalName, document=@Document, email=@Email, phone=@Phone, contact_name=@ContactName,
            plan=@Plan, status=@Status, notes=@Notes, is_active=@IsActive, updated_at=@UpdatedAt
        where id=@Id
        """, new { c.Id, c.Name, c.LegalName, c.Document, c.Email, c.Phone, c.ContactName, Plan = DbEnum.Text(c.Plan), Status = DbEnum.Text(c.Status), c.Notes, c.IsActive, c.CreatedAt, c.UpdatedAt }, ct);

    public Task<bool> DocumentExistsAsync(string document, Guid? exceptId = null, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<bool>("select exists(select 1 from habitflow.clients where document=@document and (@exceptId is null or id <> @exceptId))", new { document, exceptId }, ct);

    public async Task<IReadOnlyList<ClientUserSummary>> GetUsersAsync(Guid clientId, CancellationToken ct = default) =>
        (await db.QueryAsync<ClientUserSummary>("select id, name, email, role, account_status, created_at from habitflow.users where client_id = @clientId order by created_at desc", new { clientId }, ct)).ToList();

    public async Task<ClientMetrics> GetMetricsAsync(Guid clientId, CancellationToken ct = default)
    {
        var linked = await db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.users where client_id = @clientId", new { clientId }, ct);
        return new ClientMetrics(linked, 0, 0);
    }
}
