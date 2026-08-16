# Validação final de runtime — HabitFlow v6135

## Resultado dos comandos

| Comando | Resultado |
|---|---|
| `dotnet build HabitFlow.sln --configuration Release` | Não executado: `dotnet: command not found` (limitação do ambiente) |
| `npm run security:scan` | Aprovado: `Security scan OK (projeto).` |
| `npm test` | Aprovado: validações Firestore/Firebase e testes unitários de segurança OK |
| `npm audit --omit=dev` | Aprovado: zero vulnerabilidades |

## Verificações estáticas

- As cinco leituras de templates usam um DTO de banco antes de construir o domínio.
- Não há `QueryAsync<HabitTemplate>` nem `QuerySingleOrDefaultAsync<HabitTemplate>` na Infrastructure.
- A projeção não usa `select *` ou `select t.*`.
- `suggested_days`, `difficulty`, `tags`, `suggested_reminder_time` e `published_at` possuem tipos e conversões explícitos no caminho de leitura.
- Nenhum teste foi criado ou alterado.

## Validação de runtime

A aplicação não foi iniciada porque o executável `dotnet` não existe no ambiente. Consequentemente, não foram declarados como validados os seguintes fluxos:

- `/habit-library` e filtro de favoritos;
- favoritar e remover favorito;
- detalhe, customização e uso de template;
- onboarding escolhendo template;
- sugestão de biblioteca no dashboard.

Esses smoke tests exigem ambiente com .NET, PostgreSQL migrado e sessão autenticada. A eliminação da exceção reportada foi verificada no código ao remover a materialização direta do record, mas ainda requer confirmação integrada nos logs.

## Escopo externo

PlantaoPro não está neste workspace. Nenhuma alteração ou validação do `SaasRouteGuardFilter`, `OperationBff` ou `MinhaCentral` foi realizada neste repositório.
