# Correção da materialização Dapper de `HabitTemplate`

## Escopo

Este workspace contém somente o repositório HabitFlow. Os arquivos e a solution do PlantaoPro não estão presentes; portanto, o erro do `SaasRouteGuardFilter` deve ser corrigido no repositório PlantaoPro e não foi alterado aqui.

## Causa raiz

Os repositórios materializavam diretamente o record posicional de domínio `HabitTemplate`. A consulta retornava nomes PostgreSQL em `snake_case` e valores que precisam de conversão explícita, incluindo dificuldade textual, flags de dias armazenadas como array, tags e valores temporais anuláveis. A assinatura retornada não correspondia a um construtor que o Dapper pudesse usar.

## Correção

- `HabitTemplateRepository`: corrigidas as leituras `ListActiveAsync`, `ListActiveByObjectiveAsync`, `GetAsync` e `ListAllForAdminAsync`.
- `HabitTemplateFavoriteRepository`: corrigida a leitura `ListAsync`, que originava o erro reportado.
- Criados `HabitTemplateRow` e `HabitTemplateProjection` na camada Infrastructure.
- A projeção SQL nomeia explicitamente todas as colunas com aliases C#.
- O mapper converte a dificuldade com fallback para `Easy`, o inteiro agregado para `SuggestedWeekDays`, tags nulas para array vazio e plano vazio para `free`.
- O record de domínio permaneceu imutável e não recebeu construtor vazio.

## Validação

- A varredura estática não encontrou mais materialização Dapper direta de `HabitTemplate` nos repositórios.
- `npm run security:scan`, `npm test` e `npm audit --omit=dev` foram executados com sucesso.
- O build e a execução web não puderam ser realizados porque o SDK/host `dotnet` não está instalado no ambiente.

## Rotas e fluxos

Não foi declarada validação manual das rotas. `/habit-library`, favoritos, detalhe, customização, uso de template, onboarding e sugestão do dashboard permanecem pendentes de smoke test em ambiente com .NET, PostgreSQL e autenticação configurados.

## Pendências reais

1. Executar o build Release em ambiente com .NET SDK.
2. Subir a aplicação e validar os fluxos autenticados contra um banco migrado.
3. Confirmar nos logs que a exceção de materialização não volta a ocorrer.
4. Tratar o problema PlantaoPro no repositório correspondente.
