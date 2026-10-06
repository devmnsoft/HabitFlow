# Convites de usuários

Admins gerenciam pessoas em `/account/people` e convites em `/account/invites`; `/admin/users` e `/admin/users/invite` continuam como aliases legados. O gestor só vê nome, e-mail, perfil, status e data de vínculo — não vê hábitos ou conteúdos pessoais.

Tokens são gerados com RNG criptográfico e somente o SHA-256 é persistido em `habitflow.user_invites.token_hash`. Um link só é exibido ao criar ou rotacionar um convite; rotação invalida o token anterior. Convites expiram após sete dias e têm estados `Pending`, `Accepted`, `Expired` e `Canceled`.

Convites pendentes reservam uma vaga. A ocupação usa pessoas ativas mais convites pendentes não expirados, descontando e-mails que já estejam contabilizados como pessoas ativas. O limite deve estar explicitamente configurado no plano efetivo; apenas `-1` representa ilimitado. A criação também valida a feature `user_invitations` do plano efetivo. Convites, reativações e aceitações são serializados por conta em transação. A migration 090 cria a unicidade de convite pendente por tenant/e-mail e para com erro claro se encontrar duplicidades existentes para reconciliação manual. A migration 092 torna os entitlements existentes (`user_invitations` e `users_limit`) consultáveis pela autorização efetiva sem publicá-los no catálogo comercial.

O cadastro feito pelo link de convite cria um usuário sem tenant e não abre uma nova organização. A aceitação exige correspondência exata do e-mail convidado e não move usuários já vinculados a outro tenant. A vinculação do usuário e o consumo do convite ocorrem atomicamente; após aceitar, a pessoa entra novamente para atualizar o contexto da sessão.

Desativar uma pessoa preserva histórico e dados, revoga sessões e exige motivo. A conta mantém ao menos um administrador ativo apto; reativação exige vaga no plano e conta ativa. A migration 091 registra `client_joined_at` sem reescrever migrations anteriores.
