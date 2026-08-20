# Auditoria DOM — hábitos e overlays v6.16.2

## Causa raiz reproduzida

O bloco branco era o `div.dropdown-menu.header-v4__notification-menu.show` (acionado pelo botão `[data-notification-trigger]`). Em viewport móvel, o CSS aplicava `position: fixed !important` e `inset: 58px .4rem auto !important`; ao mesmo tempo, `_Layout` carregava apenas `header-v4.js`, portanto a rotina de `/notifications/preview` existente em `app-header-v2.js` nunca executava. O ancestral visível era `.header-v4__actions .dropdown`; `document.elementFromPoint()` no centro retornava o próprio menu/área vazia de preview.

Na reprodução anterior: `.dropdown-menu.show` continha esse elemento; não havia `dialog[open]`, `.modal.show`, `.offcanvas.show` ou `.hf-search:not([hidden])`; o `body` não tinha classe de modal. O estilo computado era `position: fixed`, `inset: 58px 6.4px auto`, largura próxima ao viewport, altura dependente do conteúdo, `z-index: 1000`, `opacity: 1`, `visibility: visible`, `display: block`.

## Correção e auditoria de colisões

A lista deixou de usar `.hf-habit-card`, preservando dashboard e Meu Dia. O `dialog` e o backdrop passaram a ser limitados a `.hf-plan-limit-dialog`; `.card`, `.btn`, `.hf-panel`, `.modal`, `.offcanvas` e `.dropdown-menu` não receberam novos overrides globais. Somente os menus nomeados de notificação e usuário têm comportamento de bottom sheet móvel. `header-v4.js` agora é o controlador único ativo: usa eventos Bootstrap, fecha menus concorrentes, oferece loading/vazio/erro e devolve foco pelo botão de fechar.

Foram inspecionados busca global, Mais, Novo, notificações, usuário, drawer, limite de plano, feedback, confirmação e dicas. Busca, drawer e dialogs mantêm seus controladores próprios e nenhum começa aberto.
