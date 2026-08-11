# Inventário técnico v6.12.0

## Baseline

- HEAD inicial: `d389d07569d71baec35c891cda0436667754cf25`, merge do PR #121 (`validar-release-candidate-v6.11.9`).
- Target framework: `net10.0` nos projetos Domain, Application, Infrastructure, Shared e Web.
- Aplicação: `http://localhost:5097`; Development usa PostgreSQL local na porta 5432 e `ConnectionStrings:DefaultConnection` em `appsettings.Development.json` (credencial de desenvolvimento; produção deve sobrescrever externamente).
- Migration canônica mais recente: `063_account_privacy_center.sql`; runner oficial: `scripts/database/run-migrations.sh`. Aggregates `database/script_completo.sql` e `_dev.sql` são alternativas para bancos isolados, nunca para combinar com runner no mesmo banco.

## Header e assets ativos

O header é composto por `AppHeaderViewComponent`, partials em `Views/Shared/Partials` e `_Layout.cshtml`. O layout ativo carrega `header-v4.css/js` e `navigation-v4.css`, não carrega `app-header-v2` nem `navigation-v2`.

CSS globais ativos: `site`, `design-system`, `forms`, `feedback`, `feedback-v5`, `layout-premium`, `layout-stabilization`, `navigation-premium`, `app-shell-premium`, `global-search`, `header-v4`, `navigation-v4`, `product-tips-v4`, stylesheet contextual, `accessibility`, `responsive` e `print`.

JS globais ativos: `site`, `design-system`, `feedback-system`, `feedback-v5`, `modal-manager`, `form-validation`, `guided-tour`, `pwa`, `navigation-premium`, `app-shell-premium`, `global-search`, `header-v4` e `guided-tour-v4`.

## Playwright

Specs preexistentes: feedback real/release, busca global, headers contextual/release/responsivo, privacidade responsiva e páginas públicas. A v6.12.0 acrescenta smoke de rotas, páginas de conta, matriz responsiva, gerador de evidência visual e setup manual seguro de auth state. `HABITFLOW_AUTH_STORAGE` habilita os casos autenticados.

## Scripts e workflows

Já havia scripts de banco, publisher, QA, segurança e operação Windows. A v6.12.0 acrescenta setup, execução local, runner de migrations, orquestração integral, Playwright seletivo, geração de auth state e limpeza, e direciona o publish IIS para `artifacts/v6120/iis-publish`.

Workflows anteriores: `dotnet-ci.yml` e `security-ci.yml`. A v6.12.0 os torna focados/reutilizáveis e adiciona `database-migrations-ci.yml`, `playwright-ci.yml` e `release-candidate.yml`.

## Lacunas encontradas no baseline

- SDK .NET 10, `psql`, servidor PostgreSQL e browsers não estavam disponíveis no container.
- O CI anterior misturava build, banco e browser num único workflow e o Playwright iniciava a aplicação sem preparar autenticação verificável.
- Não havia gate composto, artifact web independente, matriz real de screenshots v6.12.0, relatório visual uniforme, scripts Windows obrigatórios nem checklist operacional único.
- Specs autenticados podiam ser ignorados sem impedir o job. O novo CI exige `HABITFLOW_AUTH_STORAGE_B64`, restaura-o em arquivo efêmero e falha quando o segredo não está configurado.
