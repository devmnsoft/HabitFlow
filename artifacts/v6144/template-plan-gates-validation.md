# Validação dos gates de plano para templates

## Evidência estática

- O limite Free canônico é 5 (`AppConstants.FreePlanHabitLimit`).
- `CreateHabitFromTemplateUseCase` conta hábitos ativos, consulta `ActiveHabitsLimit` e recusa criação quando não há vaga.
- A tela GET de customização calcula uso/limite, sem criar hábito; apenas o POST antiforgery executa o caso de uso.
- A criação normal e a ativação de coleção reutilizam gates de limite. Edição de hábito não passa pelo gate de criação.
- A biblioteca descreve templates Ritmo como disponibilidade/aviso, sem prometer execução de recurso inexistente.

## Estado transacional

**Pendente:** sexto hábito, duplicação e edição não foram exercitados contra banco real nesta execução. Devem ser comprovados no runner Windows antes da aprovação da release.
