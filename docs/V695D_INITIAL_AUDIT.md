# Auditoria inicial v6.9.5D

Data: 2026-08-01 (UTC)

## Origem e histórico

- HEAD inicial: `09e3db62f18b6d4cfb5c3360cf2412d5a1a63184` (merge do PR #87).
- A cópia de trabalho iniciou limpa na branch local `work`.
- A branch `feature/v695d-activation-journey-production` foi criada a partir desse HEAD.
- Os merges dos PRs #80, #81, #82, #83, #84, #85, #86 e #87 estão presentes no histórico local.
- O repositório fornecido não possui remoto configurado. Assim, `git fetch --all --prune` não encontrou referências para atualizar; não é possível afirmar que o HEAD local equivale à `main` remota além do histórico entregue.

## Ferramentas disponíveis

- Node.js: `v24.15.0`.
- npm: `11.4.2` (emite aviso de depreciação futura para a configuração `http-proxy`).
- `dotnet`: indisponível (`command not found`).
- `psql`: indisponível (`command not found`).

## Estado inicial observado

O contrato tipado de ativação já existia, mas a implementação ainda validava a data de início contra a data UTC, retornava `GoalUpdates` vazio mesmo ao criar ou vincular um objetivo, mantinha `OnboardingUpdated` falso e `NewMilestones` vazio, e usava a API legada de notificação sem isolamento por cliente e sem chave idempotente.

## Gates

Os comandos .NET (`clean`, `restore`, builds por camada, build da solução, testes, auditoria NuGet e `publish`) e os cenários PostgreSQL não podem ser executados neste contêiner porque os respectivos executáveis não estão instalados. Nenhum desses gates é declarado aprovado neste documento. Os comandos Node, a verificação de whitespace e a inspeção de artefatos versionados são executados e registrados no Pull Request.
