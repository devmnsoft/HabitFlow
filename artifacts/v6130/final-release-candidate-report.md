# Relatório do release candidate — v6.13.0

## Veredito

**Release candidate ainda não aprovado por limitação objetiva do ambiente.** Esta entrega corrige regressões reais identificadas por inspeção e conclui as verificações Node/npm disponíveis, sem alegar validação .NET, PostgreSQL, runtime autenticado ou mobile.

## Ambiente, build e publish

- SHA inicial: `3014644d0fd3fe0f6f0c1486b17f709277db0057`.
- Node `v24.15.0`; npm `11.4.2`.
- `dotnet`, `psql` e Docker indisponíveis.
- Build, publish e `dotnet test`: não executados; não aprovados.
- Startup em `localhost:5097`: não executado; não aprovado.
- Publish directory não foi fabricado ou commitado.

## Correções aplicadas

1. Lembretes e Notificações passaram a integrar a navegação pessoal autenticada, dentro do agrupamento secundário responsivo, evitando telas isoladas e URLs que precisavam ser memorizadas.
2. A confirmação nativa `confirm()` da ação administrativa de bloqueio de benefícios foi substituída pelo diálogo acessível compartilhado, com texto específico e submissão via `requestSubmit()`, preservando validação HTML e antiforgery.
3. Nenhuma classe de teste, fixture, mock ou snapshot foi criada ou alterada.

## Verificações concluídas

- `npm run security:scan`: aprovado.
- `npm test`: aprovado (suíte existente; nenhum teste novo).
- `npm audit --omit=dev`: aprovado, 0 vulnerabilidades.
- `node --check` aprovado nos oito arquivos solicitados.
- `git diff --check`: aprovado.
- Busca estática por diálogos nativos: nenhum `alert()`/`confirm()` de navegador em views; ocorrências em `pwa.js` são os nomes legítimos da API de feedback e do evento `beforeinstallprompt`.

## Migrations e banco

Não executados. A inspeção identificou migrations `001`–`065`, porém criação limpa, upgrade, idempotência, schema, tabelas, constraints e quatro consultas de nulidade continuam pendentes. Consulte `database-validation.md`.

## Rotas, fluxos e persistência

Controllers e links principais foram inventariados estaticamente. Nenhuma rota foi marcada como aberta: sem SDK não foi possível subir o servidor, autenticar ou executar ações. Onboarding, biblioteca/templates, dashboard, Meu Dia, hábitos, objetivos, relatórios, notificações, lembretes, busca global e uso do plano exigem reteste real. Consulte `manual-route-validation.md`.

## Planos e integridade comercial

A inspeção não alterou regras de limites, downgrade, checkout ou catálogo. Monthly/Yearly, Free, Ritmo e ocultação de Evolução permanecem pendentes de validação runtime com banco e gateway configurados.

## Responsividade e screenshots

Os viewports 1440×900, 1366×768, 1280×720, 1024×768, 768×1024, 430×932, 390×844, 360×800 e 320×568 não foram abertos porque a aplicação não iniciou. Nenhum screenshot foi produzido. O diretório reservado é `artifacts/v6130/screenshots/`.

## Pendências reais para aprovação

1. Disponibilizar .NET SDK compatível e executar clean, restore, build Release, publish e testes existentes.
2. Disponibilizar PostgreSQL/psql, executar migrations em banco novo/existente/rerun e consultas de sanidade.
3. Subir em `http://localhost:5097`, verificar startup/logs/assets/service worker/favicon.
4. Executar as duas jornadas com usuários reais e validar todas as ações/persistência.
5. Abrir e registrar cada viewport solicitado.
6. Validar checkout/limites/downgrade com integrações apropriadas.

Essas pendências impedem afirmar que todos os critérios de aceite foram satisfeitos.
