# Auditoria de experiência v6.3

Data: 27/07/2026. A auditoria foi feita antes das alterações. O SDK .NET não está disponível no ambiente, portanto clean, restore, build, test e format ficaram pendentes de execução em uma máquina com .NET 10.

| Área | Estado inicial | Evidência / ação |
|---|---|---|
| DashboardController e tela Hoje | parcial | Lista hábitos, mas métricas eram preenchidas com zero e conclusão recarregava a página. |
| HabitService / agenda / conclusões | parcial | Fluxos básicos existem; política antiga ainda precisa ser totalmente substituída por acesso parametrizado. |
| Relatórios, notificações e jornada guiada | parcial | Serviços básicos existem; análises avançadas e agendamentos reais não existiam. |
| PlanEntitlementService | funcional | Plano efetivo e catálogo parametrizado já existem; foi criada uma fachada amigável única. |
| EntitlementService legado | precisa ser corrigido | Deve permanecer apenas para compatibilidade e não orientar recursos novos. |
| Convites, cadastro e contexto da conta | funcional | Há vínculo por `client_id`; consultas novas sempre exigem conta e pessoa. |
| Planos, cobrança e SuperAdmin | parcial | Estrutura comercial existe; métricas agregadas de produto ainda precisam da tela completa. |
| Migrations 032–035 | funcional | Catálogo, preços, papéis e restrição financeira estão estruturados. |
| Objetivos, calendário, marcos e compartilhamento | não implementado | Migrations e primeiro fluxo de objetivos foram adicionados nesta versão. |
| PWA e offline seguro | apenas estrutura | Havia documentação antiga, mas não os quatro ativos instaláveis; agora foram adicionados. |
| Mobile, CSS e JavaScript | parcial | Base responsiva existe; a revisão visual completa em quatro larguras segue pendente. |
| Testes v6.3 | não implementado | A suíte existente não cobria os novos critérios; ampliar antes da publicação. |

## Riscos prioritários

1. Finalizar métricas reais do Dashboard e resposta Ajax completa.
2. Implementar scheduler com trava transacional e idempotência.
3. Cobrir compartilhamento e relatórios com testes de isolamento por conta.
4. Executar scripts em PostgreSQL limpo e banco migrado.
