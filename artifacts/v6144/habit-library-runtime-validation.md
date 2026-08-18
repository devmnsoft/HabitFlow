# Validação runtime da biblioteca

## Estado

**Não aprovada nesta execução.** O container não possui SDK .NET, PostgreSQL/psql, banco local nem credenciais de um usuário autenticado. Em respeito ao gate, não se declara `/habit-library` aberta nem o erro 42601 resolvido em runtime sem executar a aplicação.

## Cobertura preparada no runner Windows

O smoke autenticado existente cobre `/habit-library`, `?favoritesOnly=true`, detalhe e customização. O runner também falha diante de `NpgsqlException`, Dapper, stack trace ou exceção não tratada, e agora executa o `EXPLAIN` antes do startup. A conferência manual ainda deve cobrir filtros, favoritar/desfavoritar, POST de criação e reload.

## Evidência estática

A montagem final possui uma quebra garantida entre `t` e `join`; templates inativos/não publicados permanecem excluídos na consulta de favoritos. Não houve fallback novo, `try/catch` no controller nem alteração do domínio.
