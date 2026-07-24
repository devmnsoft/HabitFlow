using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class ClientRepository(SqlExecutor db) : IClientRepository
{
    private const string Columns = "id, name, legal_name, document, email, phone, contact_name, plan, status, notes, is_active, created_at, updated_at, person_type, document_type, document_raw, document_normalized, trade_name, state_registration, municipal_registration, billing_email, billing_phone, billing_responsible_name, address_zipcode, address_street, address_number, address_complement, address_district, address_city, address_state, subscription_status, benefits_status, payment_status, last_payment_at, next_due_date, overdue_since, grace_period_until, blocked_paid_benefits_at, blocked_paid_benefits_reason";

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<Client>($"select {Columns} from habitflow.clients where id = @id", new { id }, ct);

    public async Task<IReadOnlyList<Client>> SearchAsync(string? search, ClientStatus? status, ClientPlan? plan, int offset, int pageSize, CancellationToken ct = default) =>
        (await db.QueryAsync<Client>($"""
            select {Columns}
            from habitflow.clients
            where (@search is null or name ilike @like or email ilike @like or document ilike @like or document_normalized ilike @like)
              and (@status is null or status = @status)
              and (@plan is null or plan = @plan)
            order by created_at desc
            offset @offset limit @pageSize
            """, new { search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(), like = $"%{search?.Trim()}%", status = status?.ToString(), plan = plan?.ToString(), offset, pageSize }, ct)).ToList();

    public Task CreateAsync(Client c, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.clients(id,name,legal_name,document,email,phone,contact_name,plan,status,notes,is_active,created_at,updated_at,person_type,document_type,document_raw,document_normalized,trade_name,state_registration,municipal_registration,billing_email,billing_phone,billing_responsible_name,address_zipcode,address_street,address_number,address_complement,address_district,address_city,address_state,subscription_status,benefits_status,payment_status,last_payment_at,next_due_date,overdue_since,grace_period_until,blocked_paid_benefits_at,blocked_paid_benefits_reason)
        values(@Id,@Name,@LegalName,@Document,@Email,@Phone,@ContactName,@Plan,@Status,@Notes,@IsActive,@CreatedAt,@UpdatedAt,@PersonType,@DocumentType,@DocumentRaw,@DocumentNormalized,@TradeName,@StateRegistration,@MunicipalRegistration,@BillingEmail,@BillingPhone,@BillingResponsibleName,@AddressZipcode,@AddressStreet,@AddressNumber,@AddressComplement,@AddressDistrict,@AddressCity,@AddressState,@SubscriptionStatus,@BenefitsStatus,@PaymentStatus,@LastPaymentAt,@NextDueDate,@OverdueSince,@GracePeriodUntil,@BlockedPaidBenefitsAt,@BlockedPaidBenefitsReason)
        """, new { c.Id, c.Name, c.LegalName, c.Document, c.Email, c.Phone, c.ContactName, Plan = DbEnum.Text(c.Plan), Status = DbEnum.Text(c.Status), c.Notes, c.IsActive, c.CreatedAt, c.UpdatedAt, PersonType = DbEnum.Text(c.PersonType), DocumentType = DbEnum.Text(c.DocumentType), c.DocumentRaw, c.DocumentNormalized, c.TradeName, c.StateRegistration, c.MunicipalRegistration, c.BillingEmail, c.BillingPhone, c.BillingResponsibleName, c.AddressZipcode, c.AddressStreet, c.AddressNumber, c.AddressComplement, c.AddressDistrict, c.AddressCity, c.AddressState, SubscriptionStatus = DbEnum.Text(c.SubscriptionStatus), BenefitsStatus = DbEnum.Text(c.BenefitsStatus), PaymentStatus = DbEnum.Text(c.PaymentStatus), c.LastPaymentAt, c.NextDueDate, c.OverdueSince, c.GracePeriodUntil, c.BlockedPaidBenefitsAt, c.BlockedPaidBenefitsReason }, ct);

    public Task UpdateAsync(Client c, CancellationToken ct = default) => db.ExecuteAsync("""
        update habitflow.clients
        set name=@Name, legal_name=@LegalName, document=@Document, email=@Email, phone=@Phone, contact_name=@ContactName,
            plan=@Plan, status=@Status, notes=@Notes, is_active=@IsActive, updated_at=@UpdatedAt, person_type=@PersonType, document_type=@DocumentType, document_raw=@DocumentRaw, document_normalized=@DocumentNormalized, trade_name=@TradeName, billing_email=@BillingEmail, billing_phone=@BillingPhone, billing_responsible_name=@BillingResponsibleName, address_zipcode=@AddressZipcode, address_street=@AddressStreet, address_number=@AddressNumber, address_complement=@AddressComplement, address_district=@AddressDistrict, address_city=@AddressCity, address_state=@AddressState, subscription_status=@SubscriptionStatus, benefits_status=@BenefitsStatus, payment_status=@PaymentStatus, last_payment_at=@LastPaymentAt, next_due_date=@NextDueDate, overdue_since=@OverdueSince, grace_period_until=@GracePeriodUntil, blocked_paid_benefits_at=@BlockedPaidBenefitsAt, blocked_paid_benefits_reason=@BlockedPaidBenefitsReason
        where id=@Id
        """, new { c.Id, c.Name, c.LegalName, c.Document, c.Email, c.Phone, c.ContactName, Plan = DbEnum.Text(c.Plan), Status = DbEnum.Text(c.Status), c.Notes, c.IsActive, c.CreatedAt, c.UpdatedAt, PersonType = DbEnum.Text(c.PersonType), DocumentType = DbEnum.Text(c.DocumentType), c.DocumentRaw, c.DocumentNormalized, c.TradeName, c.StateRegistration, c.MunicipalRegistration, c.BillingEmail, c.BillingPhone, c.BillingResponsibleName, c.AddressZipcode, c.AddressStreet, c.AddressNumber, c.AddressComplement, c.AddressDistrict, c.AddressCity, c.AddressState, SubscriptionStatus = DbEnum.Text(c.SubscriptionStatus), BenefitsStatus = DbEnum.Text(c.BenefitsStatus), PaymentStatus = DbEnum.Text(c.PaymentStatus), c.LastPaymentAt, c.NextDueDate, c.OverdueSince, c.GracePeriodUntil, c.BlockedPaidBenefitsAt, c.BlockedPaidBenefitsReason }, ct);

    public Task<bool> DocumentExistsAsync(string documentNormalized, Guid? ignoreClientId = null, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<bool>("select exists(select 1 from habitflow.clients where document_normalized=@documentNormalized and (@ignoreClientId is null or id <> @ignoreClientId))", new { documentNormalized, ignoreClientId }, ct);

    public Task<Client?> GetByDocumentAsync(string documentNormalized, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<Client>($"select {Columns} from habitflow.clients where document_normalized = @documentNormalized", new { documentNormalized }, ct);

    public async Task<IReadOnlyList<ClientUserSummary>> GetUsersAsync(Guid clientId, CancellationToken ct = default) =>
        (await db.QueryAsync<ClientUserSummary>("select id, name, email, role, account_status, created_at from habitflow.users where client_id = @clientId order by created_at desc", new { clientId }, ct)).ToList();

    public async Task<ClientMetrics> GetMetricsAsync(Guid clientId, CancellationToken ct = default)
    {
        var linked = await db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.users where client_id = @clientId", new { clientId }, ct);
        return new ClientMetrics(linked, 0, 0);
    }
}
