# Diagnóstico seguro do SQL de HabitTemplate

## Causa confirmada

A projeção terminava em `from habitflow.habit_templates t` e era concatenada diretamente com uma raw string iniciada por `join`. Como raw strings não preservam uma quebra final implícita, a consulta de favoritos resultava em `tjoin habitflow...`. O PostgreSQL tratava `tjoin` como alias e acusava o token seguinte, `habitflow` (SQLSTATE 42601).

`WithClause` agora remove somente whitespace nas bordas e insere `Environment.NewLine` entre a projeção e a cláusula. Nenhum dado de usuário é registrado.

## `ListActiveAsync` — SQL final

```sql
select
    -- lista explícita das 28 colunas (ver HabitTemplateProjection.cs)
from habitflow.habit_templates t
where t.is_active = true
  and t.published_at is not null
order by t.is_featured desc, t.sort_order, t.name
```

## Favoritos `ListAsync` — SQL final

```sql
select
    -- lista explícita das 28 colunas (ver HabitTemplateProjection.cs)
from habitflow.habit_templates t
join habitflow.habit_template_favorites f
  on f.template_id = t.id
 and f.client_id = @clientId
 and f.user_id = @userId
where t.is_active = true
  and t.published_at is not null
order by t.sort_order, t.name
```

Os nomes dos parâmetros foram mantidos para demonstrar parametrização; valores de `clientId` e `userId` não foram coletados. A varredura confirmou a ausência de `tjoin` e de concatenação direta da projeção.
