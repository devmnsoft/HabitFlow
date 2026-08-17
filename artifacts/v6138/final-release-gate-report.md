# Relatório do release gate — v6.13.8

- SHA inicial: `41c89b3d6e5e66cffc0a5b3dd125086c5bc31f73`.
- SHA final: commit que contém este relatório (consultar o PR).
- Ambiente preparado: GitHub Actions Ubuntu, .NET 10, PostgreSQL 17, PowerShell e Node 22.
- URL do run: pendente após publicação do PR (checkout sem remote e `gh` sem autenticação).
- Build/publish/migrations/startup/smoke público: configurados, execução externa pendente.
- Smoke autenticado, jornada, regras de plano e mobile: pendentes e explicitamente não aprovados.
- Bugs corrigidos: parametrização do destino do relatório PostgreSQL; workflow dedicado e fail-fast.
- Validações locais executadas: security scan, testes de segurança existentes, audit npm e sintaxe dos nove arquivos JavaScript.
- Decisão: **release não aprovada** até todos os P0s pendentes terem evidência real verde.
