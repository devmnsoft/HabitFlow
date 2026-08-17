# v6.13.5 — validação de runtime da biblioteca

| Rota/fluxo | Ação | Resultado neste ambiente | Erro encontrado | Correção aplicada | Pendência |
|---|---|---|---|---|---|
| `/habit-library` | Listar templates públicos e aplicar filtros | Validação estática aprovada; runtime bloqueado | Materialização direta do record | DTO e mapper explícito; consulta exige ativo e publicado | Smoke test autenticado com .NET/PostgreSQL |
| `/habit-library?favoritesOnly=true` | Carregar e filtrar favoritos | Validação estática aprovada; runtime bloqueado | Favoritos materializavam o record diretamente | Repositório de favoritos usa a projeção compartilhada e escopo tenant/user | Confirmar lista e contagem no navegador |
| `/habit-library/templates/{id}` | Abrir detalhe | Caminho de leitura corrigido; runtime bloqueado | `GetAsync` materializava o record | `QuerySingleOrDefaultAsync<HabitTemplateRow>` seguido do mapper | Validar template publicado e restrição de plano |
| `/habit-library/templates/{id}/customize` | Abrir e confirmar customização | Caminho de leitura corrigido; runtime bloqueado | Mesmo caminho de `GetAsync` | Projeção preserva dias, hora, tags e plano | Confirmar que GET não cria hábito e POST cria uma vez |
| `/onboarding` | Escolher template | Dependência de repository corrigida; runtime bloqueado | Materialização Dapper podia interromper o fluxo | Conversões explícitas no mapper | Executar onboarding autenticado |
| Favoritar/remover | POST das ações | Repository e JavaScript verificados estaticamente | Leitura de favoritos falhava | Projeção compartilhada; regras `client_id/user_id` mantidas | Confirmar ambos os POSTs e atualização visual |
| Usar template | Criar hábito após confirmação | Caminho de leitura corrigido; runtime bloqueado | Falha antes da execução do caso de uso | Domínio construído explicitamente sem mutabilidade | Confirmar entitlement e idempotência |

## Tipos e regras verificados no código

- Dificuldade válida é convertida sem diferenciar maiúsculas; nula/desconhecida usa `Easy`.
- Dias nulos/vazios usam `EveryDay`; arrays preenchidos viram flags.
- Tags nulas usam array vazio e múltiplas tags permanecem `string[]`.
- Hora sugerida e publicação continuam anuláveis.
- Plano nulo/vazio usa `free`; versão não positiva usa `1`.
- A listagem pública e favoritos excluem templates inativos e não publicados.

## Limitação

`dotnet` não existe neste container, logo não foi possível iniciar a aplicação na porta 5097, autenticar um usuário ou validar o banco real. Não houve erro de teste; trata-se de limitação do ambiente.
