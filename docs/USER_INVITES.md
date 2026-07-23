# Convites de usuários

Admins convidam usuários para o próprio cliente em `/admin/users/invite`. SuperAdmin pode informar outro cliente em fluxos administrativos.

Tokens são gerados com RNG criptográfico e somente o SHA-256 é persistido em `habitflow.user_invites.token_hash`. Convites expiram e, ao aceitar, o usuário é vinculado ao `client_id` do convite.
