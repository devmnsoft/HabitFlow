# Relatório final v6.12.8

## Funcionalidades implementadas

- Dashboard executivo com seis KPIs, resumo de sequência, recomendação determinística, gráfico dos últimos sete dias e onboarding com cinco templates rápidos.
- Objetivos com busca, cinco estados de filtro, seis ordenações, prazo, valor atual/alvo e próxima ação nos cards; atalho acessível para vincular hábito.
- Lembretes agrupados em Hoje, Próximos, Ativos e Pausados, com próximo disparo, pausar/reativar, adiar 15/60 minutos, excluir e abrir hábito.
- Notificações agora oferecem arquivamento em lote das lidas, restauração pela inbox arquivada, feedback para ações indisponíveis, datas relativas e distinção visual por tipo, sempre sob escopo de tenant/usuário.
- Relatórios básicos passaram a expor planejados, conclusões, dias ativos, consistência e recomendação honesta; CSV inclui a métrica planejada e mantém neutralização de fórmula.

## Design e telas

Foram alteradas `/dashboard`, `/goals`, `/goals/{id}`, `/reminders`, `/notifications` e `/reports`. Cards, chips, estados vazios e breakpoints móveis foram refinados nos CSS já existentes, sem introduzir uma camada concorrente de estilos. `/my-day`, hábitos, biblioteca, planos e privacidade foram preservados.

## Regras preservadas

Não houve alteração em entitlements, checkout, catálogo comercial ou limites. O dashboard apenas lê o limite efetivo já calculado; vínculos e lembretes continuam usando escopo de tenant/usuário e POST com antiforgery. Nenhum recurso parcial foi adicionado à copy comercial.

## Validação

- Inventário e revisão estática concluídos.
- `npm install`, security scan, testes de segurança existentes, audit e seis verificações de sintaxe JavaScript concluídos.
- `dotnet clean`, `restore`, `build` e `publish` foram tentados, mas o executável `dotnet` não existe no ambiente (exit 127). Impacto: compilação Razor/C# e runtime autenticado não puderam ser confirmados.
- Não foram criados ou alterados testes.
- Rotas e nove viewports não puderam ser abertos visualmente sem SDK, PostgreSQL, autenticação e navegador; detalhes estão na matriz manual.

## Pendências reais

1. Executar build/publish com .NET 10 SDK.
2. Subir PostgreSQL e validar as rotas com conta Free e conta paga, dados vazios e preenchidos.
3. Inspecionar visualmente os nove viewports e registrar screenshots em ambiente executável.
4. Confirmar cálculo de timezone do próximo disparo de lembrete com dados reais.
