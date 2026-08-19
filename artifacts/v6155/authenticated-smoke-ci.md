# Contrato do smoke autenticado no CI

O job cria uma senha aleatória mascarada, registra um usuário Free descartável pelo endpoint com antiforgery e valida as 14 rotas autenticadas definidas no gate, incluindo privacidade e biblioteca de hábitos. Redirecionamento para login, HTTP 500, conteúdo técnico ou exceções no log reprovam a execução.

Este documento não contém credenciais nem declara aprovação sem execução real.
