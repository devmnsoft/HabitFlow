# Auditoria de regras de negócio — v6.12.3

## Bugs confirmados e corrigidos

1. **Data inicial ausente:** a criação manual construía `Habit` sem `StartDate`; agora usa a data local do serviço `UserTimeZoneService`. A edição preserva a data persistida e só usa o input/fallback para registros legados.
2. **Gravação parcial:** criação/edição do hábito, substituição da agenda semanal e auditoria agora compartilham a `IUnitOfWork`; falhas provocam rollback.
3. **Objetivo de outro tenant:** a validação do editor e os comandos de link/unlink exigem simultaneamente cliente e usuário.
4. **Limite de plano na edição:** permanece aplicado apenas quando não há hábito atual; editar não consome nova cota.
5. **Exclusão segura:** hábito continua usando arquivamento, objetivo cancelamento, lembrete exclusão escopada e notificação arquivamento.

## Contratos revisados

- Hábitos arquivados não entram em `CountActiveAsync`; pausados continuam contando conforme a regra vigente.
- POSTs de lifecycle e conclusão usam antiforgery e identidade tenant/user.
- `Guid.Empty` em objetivo é rejeitado como input inválido.
- O detalhe de objetivo recebe somente a projeção que corresponde ao record `Habit`.

## Limites desta auditoria

Não houve alteração funcional grande. Fluxos que dependem de autenticação externa, PostgreSQL e browser são cobertos por especificações executáveis, mas somente podem ser declarados validados quando o ambiente E2E estiver configurado.
