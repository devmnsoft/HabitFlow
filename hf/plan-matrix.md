# Matriz de Features por Plano — HabitFlow v6.28.1

## Planos (DB)

| Code | Name | Monthly | Yearly |
|---|---|---|---|
| free | Gratuito | R$ 0,00 | R$ 0,00 |
| premium_monthly | Premium Mensal | R$ 14,90 | — |
| premium_yearly | Premium Anual | — | R$ 99,00 |
| team_monthly | Team Mensal | R$ 0,00 | — |
| team_yearly | Team Anual | — | R$ 0,00 |
| enterprise | Enterprise | Sob consulta | Sob consulta |
| ritmo | Ritmo (legacy) | R$ 0,00 | — |

## Matriz de Features

| Feature (code) | Free | Premium | Team | Enterprise |
|---|:---:|:---:|:---:|:---:|
| active_habits_limit | 5 | ∞ | ∞ | ∞ |
| users_limit | 1 | 1 | 10 | ∞ |
| reports_enabled | ✅ | ✅ | ❌ | ✅ |
| advanced_reports_enabled | ❌ | ✅ | ❌ | ❌ |
| shared_routines | ❌ | ❌ | ✅ | ✅ |
| shared_goals | ❌ | ❌ | ✅ | ✅ |
| challenges_enabled | ✅ | ✅ | ✅ | ✅ |
| reminders_per_habit | 1 | ∞ | 3 | ∞ |
| history_days_limit | 30 | 365 | 365 | 730 |
| weekly_goals | ❌ | ✅ | ❌ | ❌ |
| ai_assistant | ❌ | ✅ | ✅ | ✅ |
| teams | ❌ | ❌ | ✅ | ✅ |
| user_invitations | ❌ | ❌ | ✅ | ✅ |
| enterprise_sso | ❌ | ❌ | ❌ | ✅ |
| mandatory_mfa | ❌ | ❌ | ❌ | ✅ |
| custom_domain | ❌ | ❌ | ❌ | ✅ |
| white_label | ❌ | ❌ | ❌ | ✅ |
| ai_governance | ❌ | ❌ | ❌ | ✅ |
| data_portability | ❌ | ✅ | ✅ | ✅ |
| habit_program_personalization | ❌ | ✅ | ✅ | ✅ |

## Explicação

- **Free**: hábitos limitados (5), 1 usuário, relatórios básicos, 30 dias de histórico, desafios e lembretes (1 por hábito).
- **Premium**: hábitos ilimitados, relatórios avançados, IA, semanais, portabilidade, personalização de programa. Ainda 1 usuário.
- **Enterprise**: tudo do Premium + SSO, MFA obrigatório, domínio customizado, white-label, governança de IA.
- **Team**: foco em equipe — usuários ilimitados, rotinas/objetivos compartilhados, convites, sem SSO/white-label.
- **ritmo**: plano legado, não mais vendável.

## Observações

- `advanced_reports_enabled` é Premium-only (não Enterprise) — pode ser intencional ou bug.
- `team_monthly` e `team_yearly` estão com preço R$ 0,00 — precisa de configuração comercial.
- `ritmo` é um plano legado que não deveria aparecer no catálogo público.
