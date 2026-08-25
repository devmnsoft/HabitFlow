# HabitFlow v6.17.2 — Design System 2.0

## Diagnóstico e causa

O produto já possuía shells funcionais, mas a cascata era composta por várias gerações de CSS. Tokens mínimos em `tokens.css`, estilos de feature e correções de release disputavam cores, raios, sombras e espaçamento. A inconsistência visual era consequência dessa fragmentação. O painel branco do header era um estado restaurado do Bootstrap/Popper: classes `show` e estilos de posicionamento sobreviviam ao cache de navegação.

## Solução

`design-system-v2.css` é carregado por último e consolida tokens sem alterar regras de negócio: verde profundo/esmeralda, menta, gelo, grafite, escala espacial, foco, elevação e movimento. Ele harmoniza shells público, autenticado e administrativo; botões, cards, inputs, badges, tabelas, filtros, dropdowns, modais e estados vazios. O admin usa densidade maior. Movimento é reduzido quando solicitado pelo sistema.

Os componentes reutilizáveis `ActionCard`, `FilterBar`, `DataTable`, `SectionShell` e `MobileNav` complementam `PageHeader`, `MetricCard`, `EmptyState`, `StatusBadge`, confirmações e toasts existentes. Views continuam livres para migrar progressivamente, sem abstrações de negócio.

`design-system-v2.js` normaliza overlays restaurados, remove backdrops órfãos, recupera o scroll e devolve foco após dropdown/offcanvas. O Bootstrap continua responsável por clique externo, Escape e `aria-expanded` durante interações normais.

## Matriz visual automatizada

As rotas públicas `/`, `/plans`, `/support` e `/login` são verificadas em 320, 360, 390, 430, 768, 1024, 1366 e 1440 px. A suíte falha em `console.error`, `pageerror`, overflow horizontal, overlay aberto no carregamento ou painel branco vazio no header, e captura screenshots como artefatos do Playwright. Rotas autenticadas e admin permanecem cobertas pelas suítes existentes quando os storage states seguros são fornecidos pela CI.
