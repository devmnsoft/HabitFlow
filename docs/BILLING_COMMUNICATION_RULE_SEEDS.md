# Integridade dos seeds de regras de cobrança

## Contrato canônico

Os seeds de desenvolvimento e de produção informam explicitamente um `id` com
`gen_random_uuid()` e fazem *upsert* por `code`. Os quatro momentos suportados são:

| code | trigger_type | days_offset |
| --- | --- | ---: |
| `due_minus_3` | `BeforeDueDate` | -3 |
| `due_today` | `OnDueDate` | 0 |
| `due_plus_2` | `AfterDueDate` | 2 |
| `due_plus_5` | `AfterDueDate` | 5 |

O `DO UPDATE` corrige conteúdo, tipo, offset e estado ativo de instalações já
existentes. A migration 049 adiciona o default defensivo de UUID.

## Dados legados

A migration 049 é transacional. `overdue_plus_2` e `overdue_plus_5` são renomeados
quando ainda não existe o correspondente canônico, preservando `id` e `created_at`.
Quando as duas formas existem, a canônica é preservada e a legada é desativada;
nenhum registro histórico é apagado.

## Verificação

`scripts/qa/check-seed-required-ids.ps1` consulta o catálogo do PostgreSQL e falha
se um INSERT omitir a PK UUID obrigatória sem default. Execute-o depois das
migrations e antes dos testes de repetição dos seeds.
