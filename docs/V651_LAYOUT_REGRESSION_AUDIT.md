# Auditoria da regressão de layout v6.5.1

## Diagnóstico de pré-voo

A comparação somente-leitura com `25c6c1cb43d3032e01b6e98be8b8feadacad913b` confirmou a introdução do `NavigationService`, quatro layouts intermediários e folhas contextuais mínimas na v6.5. O contêiner de execução não possui o SDK `dotnet`; por isso clean, restore, build, test e format foram registrados como pendentes de CI, e não como aprovados localmente.

## Inventário encontrado

- **Layouts forçados:** Admin (`AdminLogs`, `Leads`, `Lgpd`, `Logs`, `Settings`, `Support`, `SupportDetail`, `SystemLogs`), Auth (`Login`, `Register`), Dashboard, Demo, Habits (`Index`, `Detail`), Health diagnostics, Help (`DatabaseSetup`, `Login`), Notifications, Profile (`Index`, `Accessibility`), Progress, Reports, Shared Error e Support.
- **Aninhamento:** `_PublicLayout`, `_PersonalLayout`, `_AccountLayout` e `_PlatformLayout` selecionavam `_Layout` e repassavam `Scripts`, possibilitando renderização em posição inválida.
- **CSS:** o shell carregava `tokens.css`, `base.css` e `components.css` em vez do `site.css`; esses experimentos redefinem responsabilidades já presentes no CSS legado. As quatro folhas contextuais eram selecionadas pelos layouts, mas o contexto era facilmente ignorado pelas Views explícitas.
- **Navegação/overflow:** descrições, plano, gestão, identidade, instalação e saída disputavam a barra horizontal. Conta e Plataforma não possuíam sidebar real.
- **Ícones ausentes:** `users`, `invite`, `billing`, `warning`, `privacy` e `settings` caíam no fallback de hábito.
- **Rotas:** não foram mantidos links com `#`; as URLs publicadas pelo serviço têm contrato unitário. Rotas autenticadas dependem das personas de CI.
- **Mobile/admin:** menu horizontal extenso, footer comercial e largura do conteúdo eram compartilhados com a Plataforma.

## Correções

Há agora um documento HTML único, contexto centralizado, CSS contextual único, perfil agrupado, sidebars/drawers, footer por contexto, navegação inferior pessoal e scripts ao final. O contrato PowerShell impede regressões estruturais. Nenhuma migration, regra financeira ou funcionalidade de domínio foi alterada.
