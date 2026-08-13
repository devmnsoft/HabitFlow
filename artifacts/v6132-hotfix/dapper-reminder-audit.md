# Auditoria preventiva Dapper — lembretes

Comando: `rg -n "QueryAsync<.*Reminder|QuerySingleOrDefaultAsync<.*Reminder|QueryAsync<.*record|select \\*|select [a-zA-Z]+\\.\\*" src/HabitFlow.Infrastructure`.

## Resultado

- As duas leituras de lembretes agora materializam `HabitReminderRow`, nunca o record posicional `HabitReminder`.
- A projeção de lembretes lista todas as colunas e usa aliases C# explícitos; `DaysOfWeek` usa `array[]::integer[]`.
- Não há `select *` nem `select r.*` no repositório de lembretes.
- O scan encontrou `select *` somente em repositórios operacionais administrativos que retornam `object`. Eles não participam das rotas de hábitos/lembretes nem apresentam o mesmo problema de construtor posicional; foram registrados, mas não alterados neste hotfix focal.
