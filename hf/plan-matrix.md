# Matriz de Features por Plano — HabitFlow v6.28.2

Fonte: banco `habitflow` (migração `110_v6282_commercial_trial_gate_ritmo_r10.sql` aplicada).
`-1` = ilimitado; `false`/`0` = recurso indisponível no plano.

## Regra comercial v6.28.2

1. **Free = teste de 15 dias** (`clients.trial_ends_at = criado + 15 dias`). Durante o trial o plano
   efetivo é o **Ritmo** (recursos liberados).
2. **Ao fim dos 15 dias sem assinatura** → o cliente cai no plano interno **`blocked`**: toda criação
   (hábito/meta) é recusada com `plan.trial_expired`, todas as features `false`, limites `0`.
   Dados existentes continuam visíveis e os check-ins continuam permitidos (continuidade de dados).
3. **Ritmo (R$ 10,00/mês ou R$ 100,00/ano)** é o plano pago de entrada: **até 5 hábitos ativos**.
4. **SuperAdmin gerencia contas** (`/superadmin/users`): bloqueio/desbloqueio auditado
   (ação `user.block`/`user.unblock`), com motivo obrigatório (≥5 caracteres), invalidação de sessões
   (`session_version` incrementado) e guarda contra auto-bloqueio do SuperAdmin.
5. **Troca de plano pelo usuário** (`/account/plan/change/{plano}`):
   - downgrade para Free é imediato (assassinato da assinatura ativa + audit);
   - upgrade tenta checkout real (`StartCheckoutAsync`); sem MercadoPago configurado exibe a mensagem
       honesta de "pagamento online indisponível, fale com o comercial" — **nunca simula pagamento**;
   - a ativação efetiva de plano pago acontece via `ChangePlanAsync` do SuperAdmin (auditado) ou
       confirmação de pagamento (`MarkInvoicePaidAsync` define `subscription_status='Active'`).

## Planos (DB)

| Code | Nome público | Mensal | Anual | Observação |
|---|---|---|---|---|
| free | Grátis por 15 dias | R$ 0,00 | R$ 0,00 | Público (`is_public=true`); vira `blocked` após o trial sem assinatura |
| ritmo | Ritmo | R$ 10,00 | R$ 100,00 | Plano pago de entrada (5 hábitos ativos) |
| team | Team | R$ 79,90 | R$ 799,00 | Vendável |
| enterprise | Enterprise | Sob consulta | Sob consulta | Contato comercial |
| blocked | Acesso bloqueado | — | — | Interno; aplicado pelo gate de trial expirado |
| premium_monthly / premium_yearly | Premium Mensal/Anual | R$ 14,90 | R$ 99,00 | Legados, ocultos (`sales_status='Hidden'`) |
| evolucao | Evolução | R$ 49,90 | R$ 499,00 | Grandfathered, não público |

## Matriz de Features (DB real)

| Feature (code) | free | ritmo | team | enterprise | blocked |
|---|:---:|:---:|:---:|:---:|:---:|
| active_habits_limit | 5 | **5** | ∞ | ∞ | 0 |
| active_goals_limit | 1 | ∞ | ∞ | ∞ | 0 |
| history_days_limit | 90 | ∞ | ∞ | ∞ | 30 |
| users_limit | 1 | 1 | 10 | ∞ | 0 |
| reminders_per_habit | — | — | — | — | 0 |
| basic_reports | ✅ | ✅ | ✅ | ✅ | ❌ |
| advanced_reports | ❌ | ❌ | ❌ | ❌ | ❌ |
| report_export_csv / report_print | ❌ | ✅ | ✅ | ✅ | ❌ |
| ai_assistant | ❌ | ✅ | ✅ | ✅ | ❌ |
| full_habit_library | ❌ | ✅ | ✅ | ✅ | ❌ |
| full_history | ❌ | ✅ | ✅ | ✅ | ❌ |
| push_notifications | ❌ | ✅ | ✅ | ✅ | ❌ |
| streak_freeze | ❌ | ✅ | ✅ | ✅ | ❌ |
| custom_categories | ❌ | ✅ | ✅ | ✅ | ❌ |
| advanced_achievements / advanced_offline | ❌ | ✅ | ✅ | ✅ | ❌ |
| public_api / webhooks | ❌ | ✅ | ✅ | ✅ | ❌ |
| challenge_7_days | ✅ | ✅ | ✅ | ✅ | ❌ |
| challenge_30_days / challenge_90_days | ❌ | ✅ | ✅ | ✅ | ❌ |
| weekly_goals | ✅ | ✅ | ✅ | ✅ | ❌ |
| achievements / missions / habit_programs | ✅ | ✅ | ✅ | ✅ | ❌ |
| progress_dashboard / data_portability | ✅ | ✅ | ✅ | ✅ | ❌ |
| habit_program_personalization | ✅ | ✅ | ✅ | ✅ | ❌ |
| teams | — | — | ✅ | ✅ | ❌ |
| user_invitations | ❌ | ❌ | ✅ | ✅ | ❌ |
| client_admin_dashboard | ❌ | ❌ | ✅ | ✅ | ❌ |
| shared_routines / shared_goals / priority_support / consolidated_reports / advanced_reports / internal_communications | ❌ | ❌ | ❌ | ❌ | ❌ |

## Explicação

- **Free (trial de 15 dias)**: mesmos limites do Ritmo durante o trial; ao expirar sem pagamento vira
  `blocked`. Assinando, permanece em Free com 5 hábitos / 1 meta / 90 dias de histórico.
- **Ritmo (R$ 10/mês)**: hábitos ilimitados **não** — **5 hábitos ativos**, metas ilimitadas,
  histórico ilimitado, IA, biblioteca completa, lembretes push, CSV/print, API/webhooks.
- **Team**: tudo do Ritmo + times (10 usuários), convites, dashboard do cliente.
- **Enterprise**: usuários ilimitados + tudo do Team (SSO/MFA obrigatório/etc. são features
  ainda não implementadas — ver `implementation_status` no catálogo).
- **blocked**: interno do gate de trial — todos os recursos `false`/`0`, histórico 30 dias para leitura.

## Observações

- `advanced_reports`, `shared_routines`, `shared_goals`, `priority_support`,
  `consolidated_reports`, `internal_communications` estão com `false` para **todos** os planos
  (não implementadas/marketing) — aparecem como "não incluído" na comparação pública.
- Enterprise não possui preço em `plan_prices` (fluxo "Sob consulta").
- `database/script_completo.sql` (artefato derivado) ainda reflete os preços antigos do ritmo;
  a fonte de verdade é a migração `110`.
