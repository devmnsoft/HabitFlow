# Auditoria v5.5 — Pop-ups, Feedback e Engajamento

## Mensagens atuais encontradas
- `TempData` em controllers para sucesso, erro, warning, validação e erro de banco.
- Alertas Bootstrap em algumas views administrativas e billing.
- `data-confirm` em LGPD/admin/perfil.

## Alertas inline encontrados
- Validação de senha no cadastro.
- Resumos de validação em formulários Razor.

## Pop-ups criados
- Modal global `_FeedbackModal` para erro, database, confirmação e conquista.
- `showRetryError` com “Tentar novamente” e suporte.
- `showAchievement` para conquistas.

## Toasts criados
- Host premium `_ToastHost`.
- Toasts success/info/warning/error/database via `HabitFlowFeedback.showToast`.

## Fluxos alterados
- `TempData` de sucesso/info/warning vira toast.
- `TempData` de erro/database vira modal.
- Confirmações usam modal reutilizável e não `confirm()` nativo.
- Preferências de pop-up adicionadas a perfil/acessibilidade.

## Pendências
- Ligar eventos AJAX do dashboard para atualização sem reload quando os endpoints retornarem payload de progresso.
- Persistir “não mostrar novamente” de dicas no banco em versão futura.
