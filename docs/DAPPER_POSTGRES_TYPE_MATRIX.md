# Matriz PostgreSQL, Npgsql e Dapper (v6.6.1)

| PostgreSQL | Contrato de persistência C# | Convenção |
|---|---|---|
| `date` | `DateOnly` / `DateOnly?` | Data civil, nunca horário artificial. |
| `time` | `TimeOnly` / `TimeOnly?` | Horário local; timezone é armazenado separadamente. |
| `timestamp without time zone` | `DateTime` | Somente valor civil documentado; `Kind=Unspecified`. |
| `timestamp with time zone` | `DateTime` UTC | Instantes entram e saem em UTC. |
| `uuid` | `Guid` / `Guid?` | Nullability acompanha a coluna. |
| `numeric` | `decimal` / `decimal?` | Nunca `double` para dinheiro. |
| `jsonb` | `string`, `JsonDocument` ou Row DTO | Conversão explícita antes do domínio. |
| `varchar` representando enum | `string` em Row DTO | Validar antes de criar o objeto de domínio. |

A auditoria estática das 77 chamadas Dapper priorizou clientes, catálogo, assinatura, fatura, pagamentos, objetivos, templates, lembretes e snapshots. Consultas cujo shape é projeção devem usar Row DTO; aliases citados eliminam dependência de `MatchNamesWithUnderscores`.
