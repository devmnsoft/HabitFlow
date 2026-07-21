# HabitFlow Design System v4.7

## Paleta
A paleta oficial usa `--hf-primary #10B981`, `--hf-primary-dark #059669`, `--hf-primary-soft #D1FAE5`, superfícies brancas, fundo `#F3F4F6`, texto `#111827` e estados warning/danger/info/success.

## Tipografia
Fonte system UI/Inter-like, títulos com tracking reduzido e hierarquia clara. Eyebrows usam caixa alta para contexto de seção.

## Componentes
- Topbar premium responsiva com marca, slogan, links e área do usuário.
- `hf-card`, `hf-metric-card`, `hf-admin-metric`, `hf-habit-card`.
- Partials Razor em `Views/Shared/Partials` para headers, métricas, empty states, badges, toasts, modal, notificações, hábitos, admin e suporte.

## Botões, cards e badges
Botões seguem Bootstrap 5 com radius pill. Cards usam borda suave, sombra leve e espaço em branco. Badges comunicam plano, status, categoria e contexto sem depender só de cor.

## Forms e tabelas
Inputs têm labels visíveis, raio consistente e foco acessível. Tabelas devem ficar dentro de `.table-responsive`.

## Estados vazios
Empty states devem orientar uma próxima ação pequena, com mensagem motivadora e CTA claro.

## Acessibilidade
Usar landmarks, aria-label em botões de ícone, foco visível, contraste adequado, headings sequenciais e modais com título.

## Exemplos
Use `.hf-page-header` no topo de telas, `.hf-grid` com colunas utilitárias para métricas e `.hf-card` para blocos de conteúdo.

## v4.9 — Componentes premium

Novas classes: `hf-shell`, `hf-navbar`, `hf-brand`, `hf-hero`, `hf-hero-grid`, `hf-feature-card`, `hf-step-card`, `hf-premium-card`, `hf-mockup-card`, `hf-section`, `hf-section-kicker`, `hf-empty-state`, `hf-guided-card`, `hf-objective-card`, `hf-habit-template-card`, `hf-footer`, `mnsoft-signature` e `mnsoft-wordmark`.

## v5.0 — Tokens de contraste seguro

O design system passa a centralizar cores em `site.css` com `--hf-bg`, `--hf-surface`, `--hf-text`, `--hf-text-muted`, `--hf-primary`, tokens MNSOFT e superfícies `surface-*`. Use texto escuro em fundos claros e `text-on-dark` em fundos escuros.
