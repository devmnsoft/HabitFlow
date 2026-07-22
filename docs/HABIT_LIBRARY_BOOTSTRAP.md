# Bootstrap da Habit Library

A biblioteca usa as tabelas `habitflow.habit_objectives` e `habitflow.habit_templates`, criadas pela migration `017_habit_templates_guided_journey.sql` e pelo `database/script_completo.sql`.

Execute `psql -U postgres -d habitflow -f database/script_completo.sql` em banco limpo e valide com `database/validate_schema_habitflow.sql`.
