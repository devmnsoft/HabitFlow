using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class ClientAccountRegistrationService(IClientRepository clients, IUserRepository users, IPasswordHasher hasher, DocumentValidator documents, ClientOnboardingService onboarding, ClientCommunicationService communications, AuditService audit, ILogger<ClientAccountRegistrationService> logger, IUnitOfWork unitOfWork)
{
    public async Task<Result<Client>> RegisterAsync(RegisterClientAccountDto dto, CancellationToken ct = default)
    {
        try
        {
            var personTypeText = dto.ClientPersonType?.Trim();
            if (!Enum.TryParse<ClientPersonType>(personTypeText, true, out var personType)) return Result<Client>.Failure("validation.person_type_required", "Informe se o cliente é Pessoa Física ou Pessoa Jurídica.");
            var expectedDocumentType = documents.GetDocumentTypeByPersonType(personType.ToString());
            var normalized = documents.Normalize(string.IsNullOrWhiteSpace(dto.DocumentNormalized) ? dto.DocumentRaw : dto.DocumentNormalized);
            var isCpf = personType == ClientPersonType.NaturalPerson;
            if (isCpf && !documents.ValidateCpf(normalized)) return Result<Client>.Failure("validation.cpf_invalid", "Informe um CPF válido.");
            if (!isCpf && !documents.ValidateCnpj(normalized)) return Result<Client>.Failure("validation.cnpj_invalid", "Informe um CNPJ válido.");
            if (string.IsNullOrWhiteSpace(dto.Email)) return Result<Client>.Failure("validation.email_required", "Informe seu e-mail.");
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8) return Result<Client>.Failure("validation.password_short", "A senha deve ter pelo menos 8 caracteres.");
            if (!dto.AcceptedTerms) return Result<Client>.Failure("validation.terms_required", "Aceite os Termos de Uso para continuar.");
            if (!dto.AcceptedPrivacy) return Result<Client>.Failure("validation.privacy_required", "Aceite a Política de Privacidade para continuar.");
            if (await clients.DocumentExistsAsync(normalized, null, ct)) return Result<Client>.Failure("validation.document_duplicate", "Já existe uma conta cadastrada com este CPF/CNPJ.");
            var email = dto.Email.Trim().ToLowerInvariant();
            if (await users.GetByEmailAsync(email, ct) is not null) return Result<Client>.Failure("email.exists", "E-mail já cadastrado.");

            await unitOfWork.BeginTransactionAsync(ct);
            var now = DateTime.UtcNow;
            var formatted = isCpf ? documents.FormatCpf(normalized) : documents.FormatCnpj(normalized);
            var clientName = isCpf ? dto.ClientName.Trim() : (dto.LegalName ?? dto.ClientName).Trim();
            if (string.IsNullOrWhiteSpace(clientName)) return Result<Client>.Failure("validation.name_required", isCpf ? "Informe seu nome completo." : "Informe a razão social.");
            if (!isCpf && string.IsNullOrWhiteSpace(dto.ResponsibleName)) return Result<Client>.Failure("validation.responsible_required", "Informe o nome do responsável.");
            var client = new Client(Guid.NewGuid(), clientName, isCpf ? null : dto.LegalName?.Trim(), formatted, email, dto.Phone?.Trim(), isCpf ? dto.ClientName.Trim() : dto.ResponsibleName?.Trim(), ClientPlan.Free, ClientStatus.Active, "Cadastro público SaaS Free", true, now, now, personType, Enum.Parse<ClientDocumentType>(expectedDocumentType), formatted, normalized, dto.TradeName?.Trim(), null, null, email, dto.Phone?.Trim(), isCpf ? dto.ClientName.Trim() : dto.ResponsibleName?.Trim(), null, null, null, null, null, null, null, ClientSubscriptionStatus.Free, ClientBenefitsStatus.Free, ClientPaymentStatus.None);
            await clients.CreateAsync(client, ct);
            var userName = isCpf ? dto.ClientName.Trim() : dto.ResponsibleName!.Trim();
            var user = new User(Guid.NewGuid(), userName, email, hasher.Hash(dto.Password), null, UserRole.Admin, AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Active, false, false, now, now, null, null, now, now, client.Id);
            await users.CreateAsync(user, ct);
            await onboarding.GetOrCreateAsync(client.Id, ct);
            await communications.CreateInternalMessageAsync(client.Id, user.Id, "Welcome", "Conta criada", "Sua conta gratuita foi criada com sucesso.", null, ct);
            await audit.LogAsync("client_registered", "Cliente criado no cadastro público", AuditSeverity.Info, user.Id, email, new { client.Id, personType = personType.ToString(), documentType = expectedDocumentType, document = Mask(normalized) }, ct);
            await unitOfWork.CommitAsync(ct);
            return Result<Client>.Success(client);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(ct);
            logger.LogError(ex, "Erro ao criar conta SaaS pública para {Email}", dto.Email);
            return Result<Client>.Failure("client_registration.error", "Não foi possível criar a conta. Tente novamente em instantes.");
        }
    }

    private static string Mask(string value) => value.Length <= 4 ? "****" : new string('*', value.Length - 4) + value[^4..];
}
