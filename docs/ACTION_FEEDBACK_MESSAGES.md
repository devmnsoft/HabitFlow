# Mensagens de ações

| Ação | Tipo | Título | Mensagem | Apresentação |
|---|---|---|---|---|
| Criar cliente | success | Cliente cadastrado | O cliente foi cadastrado com sucesso. | Toast |
| Editar cliente | success | Cliente atualizado | As informações do cliente foram salvas. | Toast |
| Ativar cliente | success | Cliente ativado | O cliente voltou a ficar ativo. | Toast |
| Desativar cliente | warning | Cliente desativado | O cliente foi desativado, mas o histórico foi mantido. | Toast |
| Bloquear cliente | warning | Cliente bloqueado | O cliente foi bloqueado com segurança. | Modal |
| Validação | warning | Revise os dados | Alguns campos precisam ser corrigidos. | Inline + toast |
| Banco indisponível | database | Não foi possível acessar os dados | Tente novamente em instantes ou verifique a configuração do banco. | Modal |

## Regras

- Não mostrar stack trace, SQL, `postgres.invalid_password`, JSON técnico ou detalhes internos para usuário comum.
- Usar `ApplicationFeedbackService` nos controllers.
- Usar validação inline nos campos Razor.
- Ações sensíveis devem usar modal de confirmação, nunca `confirm()` nativo.
