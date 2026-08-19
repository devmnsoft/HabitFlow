# Smoke público — v6.13.9

- Rotas alvo: `/`, `/login`, `/register`, `/plans`, `/service-worker.js`, `/favicon.ico`.
- Resultado: **P0 pendente**; a aplicação publicada não pôde iniciar sem .NET/PostgreSQL.
- O workflow valida HTTP menor que 500 e reprova logs contendo `Unhandled exception`, `InvalidOperationException` ou `NpgsqlException`.
