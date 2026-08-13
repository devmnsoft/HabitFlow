# v6.12.6 — relatório final de validação

## Resultado desta execução

- **Limpeza:** removidos os três campos acidentais e espaços mortos de
  `HabitScheduleService`; regras e normalizador foram mantidos.
- **Build .NET:** bloqueado antes da compilação porque `dotnet` não existe no
  ambiente (`dotnet: command not found`). A falha não foi ocultada.
- **npm:** `npm install`, `npm run security:scan`, `npm test` e
  `npm audit --omit=dev` concluíram com sucesso; o audit encontrou 0
  vulnerabilidades.
- **JavaScript:** `node --check` passou para `habits-v4.js`, `global-search.js`,
  `header-v4.js`, `feedback-v5.js` e `guided-tour-v4.js`.
- **Testes do repositório:** nenhum arquivo foi criado, removido ou alterado.

## Revisão estática preservada do PR #127

- O editor mantém sugestões padrão e categorias existentes, digitação livre,
  lista de objetivos ativos/pausados com escopo validado no backend, CTAs para
  criar/ver objetivos e preview preenchido por JavaScript.
- `_GoalProgressBar.cshtml` existe em `Views/Goals/Partials`, é chamado por
  `_GoalCard.cshtml`, trata modelo nulo e meta zero e expõe semântica acessível.
- O hardening de overlays, busca global e product tips do PR anterior não foi
  revertido por esta limpeza.

Esses itens foram somente revisados estaticamente nesta execução; não são
apresentados como confirmação de runtime.

## Validação manual e responsiva

Não executada: sem o host ASP.NET Core não foi possível abrir as rotas, operar
ações autenticadas, investigar visualmente o painel branco nem produzir captura
de tela legítima. A matriz completa de rotas e os oito viewports pendentes estão
em `manual-runtime-validation.md`.

## Pendências reais

1. Instalar/disponibilizar o SDK .NET 10 e executar `clean`, `restore`, `build`
   e `dotnet run`.
2. Usar PostgreSQL/configuração de desenvolvimento e uma conta autenticada com
   hábitos, objetivos, notificações e lembretes para percorrer a matriz.
3. Repetir a inspeção visual de overlays e responsividade nos oito viewports,
   registrando screenshots somente após abrir as telas.
4. Exercitar persistência de categoria/objetivo, ciclo de vida e feedback das
   ações em navegador real.
5. Na fase final de testes, revisar a classe de testes de recorrência removida
   pelo commit `ajuste`.
