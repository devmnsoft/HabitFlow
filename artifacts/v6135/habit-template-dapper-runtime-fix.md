# v6.13.5 — correção da materialização Dapper de `HabitTemplate`

## Erro original e causa raiz

A biblioteca falhava com `InvalidOperationException` porque Dapper tentava materializar diretamente o record posicional `HabitTemplate`. A linha SQL contém nomes e representações de persistência que não correspondem ao construtor de domínio: aliases `snake_case`, dificuldade textual, dias sugeridos como array PostgreSQL, `TimeOnly?`, `text[]` e vários valores anuláveis.

## Correção aplicada

- Criados o DTO interno `HabitTemplateRow` e o mapper compartilhado `HabitTemplateProjection`, exclusivamente na Infrastructure.
- A projeção nomeia cada coluna; não usa `select *` ou `select t.*`.
- `difficulty` é convertido explicitamente, com fallback `Easy` para nulo/desconhecido.
- `suggested_days` nulo/vazio resulta em `EveryDay`; valor positivo é convertido para o flags enum.
- `tags` nulo resulta em `text[]` vazio no SQL e array vazio no mapper.
- `minimum_plan_code` nulo/vazio resulta em `free`; `is_featured` e `content_version` também são normalizados.
- `suggested_reminder_time` e `published_at` preservam nulabilidade.
- `HabitTemplateRepository.ListActiveAsync`, `ListActiveByObjectiveAsync`, `GetAsync` e `ListAllForAdminAsync` agora consultam `HabitTemplateRow` e mapeiam para o domínio.
- `HabitTemplateFavoriteRepository.ListAsync` usa a mesma projeção e mantém os filtros de `client_id` e `user_id`.
- O record de domínio não foi tornado mutável e não recebeu construtor vazio. Nenhuma migration ou teste foi criado.

## Varredura

- A busca por `QueryAsync<HabitTemplate>`, `QuerySingleOrDefaultAsync<HabitTemplate>` e `QueryFirstOrDefaultAsync<HabitTemplate>` na Infrastructure não retornou ocorrências.
- Os dois repositórios compartilham uma única projeção SQL.
- Não há `select *` relacionado a `HabitTemplate`. As ocorrências em `AdminOperationalRepositories.cs` são preexistentes e fora deste hotfix.

## Build e verificações

- `dotnet build HabitFlow.sln --configuration Release`: bloqueado porque `dotnet` não está instalado (`command not found`).
- `npm run security:scan`: aprovado.
- `npm test`: aprovado.
- `npm audit --omit=dev`: aprovado, zero vulnerabilidades.
- Os oito arquivos JavaScript solicitados passaram em `node --check`.

## Rotas e pendências reais

A validação integrada de `/habit-library`, favoritos, detalhe, customização, onboarding e criação a partir de template permanece pendente. Este ambiente não possui o runtime .NET, PostgreSQL configurado nem sessão autenticada; portanto, a aplicação não pôde ser iniciada. É necessário executar smoke tests autenticados e confirmar os logs em um ambiente integrado. `GET /service-worker.js 304` é esperado e não faz parte deste hotfix.
