# Inventário dos erros de runtime — v6.13.2

Base revisada: `bca4d26403476e6e6e7f10a6328bd9e580ac0a2d`, merge do PR #135 e, portanto, posterior ao merge do PR #134 (`d150660`).

| Erro reportado | Arquivo causador | Causa provável confirmada no código | Correção planejada |
|---|---|---|---|
| `/habits`: partial `_HabitStatusBadge` não encontrado | `Views/Habits/Partials/_HabitCard.cshtml` | O nome curto fazia o Razor procurar no diretório da view (`Views/Habits`) e em `Views/Shared`; o partial já existia na subpasta, mas não em um dos locais resolvidos pelo nome curto. | Criar o partial funcional em `Views/Habits/Partials` e usar `Partials/_HabitStatusBadge`. |
| `/reminders`: Dapper não consegue materializar `HabitReminder` | `HabitReminderRepository.cs` | A projeção snake_case e os `timestamp` materializados como `DateTime` não correspondem ao construtor posicional do record, que espera `DateTimeOffset?`. | Projetar aliases explícitos em DTO privado, converter timestamps em UTC e mapear explicitamente ao record. |
