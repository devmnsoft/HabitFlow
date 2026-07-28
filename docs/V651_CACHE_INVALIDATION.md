# Invalidação de cache v6.5.1

CSS, JavaScript, favicon e manifest usam `asp-append-version`. O Service Worker usa o cache `habitflow-public-v651-shell-1`, remove caches públicos HabitFlow anteriores na ativação e mantém estratégia network-first somente para a allowlist pública. Rotas privadas (`dashboard`, conta, billing, profile, admin e superadmin) não são cacheadas.

Para QA: remover o Service Worker, limpar Cache Storage/Local Storage, recarregar com cache desabilitado e confirmar a nova versão. A mensagem `SKIP_WAITING` continua realizando atualização segura; `CLEAR_HABITFLOW_CACHES` permanece disponível para suporte.
