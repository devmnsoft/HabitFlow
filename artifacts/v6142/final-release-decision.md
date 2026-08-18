# Decisão final de release — v6.14.2

- SHA inicial: `7b1d5bd4f5022bd613b2e37b06a98bbfc9f62e38`.
- SHA final: commit que contém este relatório (consultar `git rev-parse HEAD`; um arquivo não pode conter de forma estável o hash do próprio commit).
- Run URL final: inexistente.
- Bugs de produto corrigidos: nenhum; não houve falha real de CI observável.
- Checks locais verdes: security scan, testes npm, audit de produção e sintaxe dos nove arquivos JavaScript.

| Gate | Resultado |
|---|---|
| Jobs remotos | Não executados |
| Build / publish | Não executados; `dotnet` ausente |
| Migrations | Não executadas; Actions/PostgreSQL indisponíveis |
| Smoke público | Não executado |
| Smoke autenticado | Não executado |
| Jornada MVP | Não validada |
| Regras Free/Ritmo | Não validadas |
| Mobile | Não validado; zero screenshots |

## P0s pendentes

1. Executar e tornar verdes os seis jobs do release gate.
2. Coletar logs e artifacts reais do run.
3. Validar a jornada MVP completa e persistência.
4. Validar regras Free/Ritmo/Evolução/uso do plano.
5. Validar as dez telas nos nove viewports com screenshots reais.

## Decisão

**Release não aprovada — P0 pendente**.

A decisão é estritamente baseada na ausência de evidência exigida, não em uma inferência de defeito do produto. O próximo passo permitido é restaurar acesso ao repositório remoto/Actions e executar o gate existente; nenhum novo módulo é recomendado antes do fechamento dos P0s.
