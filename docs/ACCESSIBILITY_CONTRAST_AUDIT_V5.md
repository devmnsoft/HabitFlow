# Auditoria de contraste v5.0

Critério usado: WCAG AA (4.5:1 para texto normal, 3:1 para texto grande/negrito e elementos de interface). O CSS foi consolidado em um único `:root` e usa tokens seguros.

| Combinação | Tokens | Resultado |
|---|---|---|
| Texto principal sobre página | `--hf-text` em `--hf-bg` | Aprovado AA |
| Texto secundário sobre página/card | `--hf-text-muted` em fundos claros | Aprovado AA |
| Cards brancos | `--hf-surface` + `--hf-text` | Aprovado AA |
| Cards verdes claros | `--hf-surface-green` + `--hf-text` | Aprovado AA |
| Cards azuis claros | `--hf-surface-blue` + `--hf-text` | Aprovado AA |
| Cards escuros/mockups | `--hf-surface-dark` + `--hf-text-on-dark` | Aprovado AA |
| Botões verdes | `--hf-primary` + `--hf-text-on-primary` | Aprovado AA |
| Botões outline | texto verde escuro + borda primária | Aprovado AA |
| Badges | fundos claros + texto escuro específico | Aprovado AA |
| Alertas | fundos suaves + texto escuro de status | Aprovado AA |
| Sidebar Admin | fundo `#0F172A` + texto claro | Aprovado AA |
| Footer escuro | `#0F172A` + `#F9FAFB`/`#DBEAFE` | Aprovado AA |
| Links e hover/focus | azul escuro MNSOFT | Aprovado AA |
| Tabelas | cabeçalho soft + texto forte | Aprovado AA |
| Inputs/placeholders | borda visível + placeholder `--hf-text-soft` | Aprovado AA |

## Decisões
- `#9CA3AF` não deve ser usado para texto importante.
- Branco não deve ser usado sobre fundos amarelos/claros.
- Gradientes foram mantidos como decoração e não como base de textos longos sem contraste.
- Alto contraste troca tokens no `body.hf-contrast-high` sem duplicar componentes.
