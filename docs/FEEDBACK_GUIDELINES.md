# HabitFlow v5.5 — Feedback Guidelines

## Toast
Use para cadastro salvo, login realizado, hábito criado/concluído/arquivado, preferências salvas, notificação lida e ações simples. Toasts devem ser curtos, não invasivos e acionados por `window.HabitFlowFeedback.showToast(type, title, message, options)`.

## Modal/pop-up
Use para erro de banco, falha crítica, confirmação de exclusão/arquivamento, LGPD, cancelamento de assinatura, ação admin sensível, perda de dados e sessão expirada. Usuário final nunca deve ver JSON, stack trace ou segredo técnico.

## Inline
Use apenas para validação de campo: obrigatório, senha curta, senhas diferentes, e-mail inválido e erro específico de formulário.

## Central de notificações
Use para conquistas, lembretes, avisos persistentes, suporte, pagamento, relatórios prontos e alertas administrativos.
