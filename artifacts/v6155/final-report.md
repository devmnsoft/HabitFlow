# Relatório final — v6.15.5 CIReleaseGateConsolidation

- SHA inicial: `3c49eb1ea1d5cb607e7f621ef901541b50544aaf`.
- SHA final: registrado pelo commit e pelo `GITHUB_SHA` no relatório da execução.
- Workflows antes: oito; depois: sete.
- Removido: `.github/workflows/v6138-release-gate.yml`.
- Canônico: `.github/workflows/habitflow-dotnet-release-gate.yml`.
- Jobs: `dotnet-build-publish`, `postgres-migrations`, `runtime-smoke-public`, `runtime-smoke-authenticated` e `artifact-summary`.
- Cobertura: restore/build/publish, migrations dinâmicas e idempotentes, objetos LGPD, startup, rotas públicas, autenticação efêmera, rotas privadas e navegação mínima.
- Pendente manual: criação e conclusão visual de hábito, navegador real, mobile, UX e fluxo mutacional completo.
- Segurança: nenhuma senha de usuário, connection string de produção, publish ou binário foi adicionado. A credencial PostgreSQL é explicitamente descartável e restrita ao service container.
- Comandos: inventário Git/filesystem, parser YAML, verificações de diff, build e buscas de consistência (resultados finais registrados na entrega).
- Decisão documental: **CI Release Gate consolidado**. A aprovação de runtime somente pode ser emitida pelo job `artifact-summary` após todos os jobs obrigatórios passarem.
