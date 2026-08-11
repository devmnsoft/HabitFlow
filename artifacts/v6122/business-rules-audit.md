# v6.12.2 — auditoria de regras de negócio

## Escopo e método
Revisão estática dos fluxos de hábitos, objetivos, progresso, relatórios, biblioteca, lembretes, notificações, planos, privacidade e busca global. Foram procurados binder nulo, enums/IDs inválidos, ausência de escopo por tenant/usuário, limites comerciais, antiforgery, DOM inseguro e erros de domínio promovidos a HTTP 500.

| Bug encontrado | Arquivo / regra afetada | Risco | Correção aplicada | Teste criado | Pendência |
|---|---|---|---|---|---|
| `SelectedDays` ausente chegava nulo e era enumerado por `Any` | `HabitScheduleService`; frequência de hábito | `ArgumentNullException` no POST `/habits/create` | Contrato anulável e normalizador defensivo; coleções persistidas nunca são nulas | `HabitScheduleNormalizerTests` | Reexecutar fluxo real quando o SDK .NET 10 e a infraestrutura autenticada estiverem disponíveis |
| Frequências fixas aceitavam metas incompatíveis e preservavam dias irrelevantes | `HabitEditorService`; agenda semanal | Agenda e indicadores divergentes | Defaults/capacidades por frequência e descarte explícito de dias não customizados | Casos Daily/Weekdays/Weekends/CustomWeekly | — |
| Enum de frequência e dificuldade aceitava valor forjado | Editor de hábitos | Estado de domínio inválido | `Enum.IsDefined` antes da persistência | Frequência desconhecida coberta | Adicionar teste integrado para dificuldade quando houver fixture do editor |
| `ObjectiveId` era persistido sem prova de ownership | Editor de hábitos/objetivos | Referência cruzada entre tenants/usuários | Consulta por `id + client_id + user_id`; vazio também é rejeitado | Cobertura deve ser complementada na fixture de repositórios | Teste integrado pendente |
| Erros de input eram adicionados apenas ao summary | Controller/editor | Feedback pouco acionável | Mapeamento de códigos de domínio para campos e mensagens inline | Cenários Playwright especificados | Execução autenticada pendente |
| Exceção inesperada do save vazava como 500 | Create/Edit | Indisponibilidade e possível exposição em ambiente mal configurado | Log estruturado com correlationId e mensagem pública estável | Smoke Playwright verifica ausência de 500 | Execução pendente |
| Estado do botão e agenda não refletiam validação/frequência | Formulário de hábito | Duplo envio, botão preso e configuração errada | Preview acessível, metas sugeridas, validação custom e bloqueio somente após formulário válido | Specs de templates/regras | — |

## Planos comerciais
A revisão preservou os filtros e contratos introduzidos em v6.12.1: Gratuito não abre checkout pago; apenas Ritmo está disponível para contratação mensal/anual; Evolução permanece grandfathered e fora da oferta pública; benefícios públicos continuam condicionados a `Implemented` e `is_marketable`. Nenhuma alteração comercial foi necessária nesta versão. O limite de criação continua aplicado apenas quando `current is null`; `CountActiveAsync` é a fonte explícita e a edição não consome uma nova unidade.

## Busca global
Os seletores `[data-global-search-open]` e `[data-search-open]`, atalhos Ctrl/Meta+K, Escape/restauração de foco e construção segura de resultados permanecem cobertos pelos testes existentes de v6.12.1. Nenhuma regressão estática foi encontrada.

## Decisões explícitas
- Hábitos pausados contam no limite enquanto não forem arquivados, conforme a semântica da consulta `CountActiveAsync`; arquivados não contam.
- Valores de limite ilimitado permanecem centralizados nas políticas/entitlements existentes; este patch não adiciona uma segunda interpretação de `-1` ou `null`.
- Nenhum recurso `Partial`, `Planned` ou `Internal` foi promovido ou exibido como benefício comercial.
