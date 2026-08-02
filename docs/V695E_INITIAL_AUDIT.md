# Auditoria inicial da v6.9.5E

Data da coleta: 2026-08-02 (UTC).

## Repositório

- HEAD inicial: `a1678be1997090a4353ba49f230e1f1e20272b69`.
- Branch encontrada: `work`, com árvore limpa.
- Último merge: `a1678be Merge pull request #88 from devmnsoft/codex/concluir-jornada-de-ativacao-do-habitflow`.
- O histórico local confirma os merges dos PRs #80, #81, #82, #83, #84,
  #85, #86, #87 e #88.
- O clone fornecido não possui remoto configurado. Por isso, `git fetch --all
  --prune` não encontrou refs remotas e a branch de trabalho foi criada a partir
  do HEAD que contém o merge do PR #88.

## Banco de dados

- Foram encontradas 52 migrations numeradas, de `001` até `052`.
- A migration mais recente no início do trabalho era
  `052_library_v2_onboarding.sql`.
- O primeiro defeito verificável no workflow era o cenário de upgrade aplicar
  migrations `001` a `031` diretamente e depois inserir as mesmas versões à mão
  em `habitflow.schema_migrations`. Isso mantinha dois responsáveis concorrentes
  pelo registro e ignorava a reconciliação, checksums e locks do runner canônico.

## Ambiente e build

- Node.js: `v24.15.0`.
- npm: `11.4.2`.
- O SDK .NET e o cliente `psql` não estão instalados no ambiente fornecido.
  Consequentemente, não foi possível executar localmente `dotnet clean`,
  `restore`, builds, testes .NET nem os gates reais do PostgreSQL.
- Nenhuma conclusão de CI verde é registrada por esta auditoria: não há remoto
  configurado nem acesso a um workflow run associado ao commit local.
