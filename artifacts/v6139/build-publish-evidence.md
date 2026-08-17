# Evidência de build/publish — v6.13.9

- Comandos requeridos: não executados porque `dotnet` não existe no container (`dotnet: command not found`).
- O workflow instala .NET 10, restaura, compila em Release e publica sem rebuild em `artifacts/v6139-publish`.
- O upload `v6139-publish` usa `if-no-files-found: error`; o diretório gerado permanece ignorado pelo Git.
- Resultado: **P0 pendente de runner real**.
