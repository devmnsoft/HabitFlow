# Relatório final v6.14.4

## Erro e causa raiz

O SQLSTATE 42601 em `/habit-library` foi rastreado à concatenação direta entre a projeção terminada no alias `t` e `join`, gerando `tjoin habitflow...`. A correção introduz `HabitTemplateProjection.WithClause`, que garante uma quebra de linha, e migra os quatro métodos do repositório e a consulta de favoritos.

Antes: `from habitflow.habit_templates tjoin habitflow.habit_template_favorites f`.

Depois:

```sql
from habitflow.habit_templates t
join habitflow.habit_template_favorites f
```

A varredura preventiva também corrigiu o mesmo formato arriscado nas projeções compartilhadas de lembretes e documentos legais. Nenhuma entidade de domínio, feature ou teste foi criada.

## PostgreSQL e runtime

- `EXPLAIN`: **pendente**; `psql`/PostgreSQL não estão disponíveis no container.
- `/habit-library`, favoritos e filtros: **pendentes de runtime**; o SDK .NET e um usuário/banco autenticado não estão disponíveis.
- Template → customização → hábito → conclusão: **pendente de runtime** pelo mesmo motivo.
- O runner Windows v6.14.4 foi preparado para validar tipos reais, executar `EXPLAIN`, subir a aplicação e cobrir as rotas autenticadas sem persistir segredos.

## Regras Free

A revisão estática confirma limite canônico 5, gate no caso de uso de criação por template e ausência de mutação no GET de customização. O sexto hábito/duplicação/edição permanece pendente de prova transacional no runner Windows; não é declarado aprovado.

## Checks executados

- `npm run security:scan`: aprovado.
- `npm test`: aprovado.
- `npm audit --omit=dev`: aprovado, 0 vulnerabilidades.
- Nove `node --check` solicitados: aprovados.
- `dotnet build HabitFlow.sln --configuration Release`: não executado, pois `dotnet` não existe no container.
- As três varreduras `rg`: projeção de templates sem concatenação; achados preventivos de lembretes/legal corrigidos.
- `git diff --check`: aprovado.

## Pendências reais para aprovar a release

1. Executar o runner em Windows com PowerShell 7, .NET 10, psql e connection string local.
2. Obter `EXPLAIN` real e tipos de `information_schema`.
3. Abrir `/habit-library` autenticado e exercitar favoritos, filtros, detalhe, customização, criação, reload e conclusão.
4. Provar limite Free, duplicação e edição transacionalmente.
5. Só então substituir os artifacts marcados como pendentes e decidir a release.
