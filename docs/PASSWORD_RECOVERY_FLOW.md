# Fluxo de recuperação de senha

O fluxo público usa `/forgot-password`, confirmação genérica em `/forgot-password/sent`, formulário de uso único em `/reset-password?token=...` e conclusão em `/reset-password/success`. A aplicação nunca informa se a conta existe e não autentica automaticamente após a troca.

Quando uma conta ativa existe, o serviço revoga tokens anteriores, persiste somente SHA-256 do novo token e enfileira o e-mail. A redefinição bloqueia o token, atualiza o hash, incrementa `session_version`, consome o token, revoga os demais e enfileira a confirmação na mesma transação.
