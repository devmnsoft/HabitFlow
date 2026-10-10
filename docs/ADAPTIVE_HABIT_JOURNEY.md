# Jornada adaptativa de habitos

## Auditoria do incremento

- Branch analisada: `main`.
- Commit base local: `4864143e2eba5d0fdbe2f1cb38eec7914b0854a4`.
- Estado inicial: havia alteracoes locais v6.28.0 ainda nao commitadas; elas foram preservadas.
- Evidencia inicial: `dotnet build HabitFlow.sln -c Release --no-restore` passou antes das novas alteracoes.

## Classificacao dos recursos relevantes

- Funcionando com evidencia: build Release, testes .NET, calendario por frequencia, check-in idempotente por constraint, isolamento por `client_id`/`user_id` nos repositorios principais.
- Parcialmente implementados: quantidade/unidade e versao minima agora existem no dominio e banco, mas a UI ainda precisa expor todos os campos.
- Ausentes apos busca: historico versionado completo de agenda com vigencia futura por alteracao; a entrega atual preserva historico por `start_date/end_date` e nao reescreve conclusoes.
- Nao verificados: banco PostgreSQL limpo/upgrade real, Playwright visual completo desta rodada e homologacao externa de IA/pagamento.

## Formula canonica de consistencia

Para um periodo `[inicio, fim]`:

1. Liste apenas ocorrencias esperadas pela regra do habito no fuso do usuario.
2. Exclua dias antes de `start_date`, depois de `end_date`, depois de `archived_at` e durante pausa vigente.
3. Dias sem ocorrencia planejada nao entram no denominador.
4. Uma ocorrencia concluida conta no numerador quando existe registro confirmado para `(habit_id, completed_date)`.
5. Ausencia de registro e diferente de conclusao e de "nao realizado".
6. Percentual = `concluidas / previstas * 100`, arredondado a 1 casa decimal.
7. Streak considera apenas dias com agenda; o dia atual incompleto nao quebra a sequencia ate o fim do dia.

Essa regra e consumida por `HabitOccurrenceService`, `ProgressSnapshotService`, calendario, lista de habitos e revisoes que usam o snapshot canonico.

## Controles adicionados

- `AdaptiveHabitPlanningService` valida vigencia, quantidade/unidade, versao minima e janela retroativa.
- `CompleteHabitUseCase` bloqueia conclusao futura, fora da janela retroativa ou em dia nao programado.
- `HabitOccurrenceService` agora respeita `start_date`, `end_date`, pausa vigente e arquivamento para denominador.
- Migration `103_adaptive_habit_journey.sql` adiciona campos e constraints para planejamento adaptativo e check-in quantitativo.

## Pendencias reais

- Expor os novos campos em Razor/JS com estados de loading, erro, vazio e sucesso.
- Persistir alteracoes futuras de agenda em uma tabela versionada propria, quando a UI permitir edicoes programadas.
- Aplicar `recorded_quantity` no endpoint de Meu Dia para check-ins quantitativos completos.
- Rodar PostgreSQL limpo e upgrade representativo com o runner canonico.

