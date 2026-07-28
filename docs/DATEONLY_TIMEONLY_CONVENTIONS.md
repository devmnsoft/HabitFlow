# Convenções temporais

* Datas civis e cobrança sem horário (`next_due_date`, `overdue_since`, `grace_period_until`, `completed_date`, `start_date`, `end_date`) usam `DateOnly`.
* Horários locais (`reminder_time`) usam `TimeOnly`; timezone do usuário fica separado.
* Eventos (`created_at`, `updated_at`, `paid_at` e confirmação financeira) usam `DateTime` UTC quando a coluna é `timestamptz`.
* `timestamp without time zone` é materializado como `DateTime` com `Kind=Unspecified` e exige contexto documentado.

Os handlers Dapper devem ser registrados no bootstrap, antes de abrir a primeira conexão/query. Round-trips PostgreSQL cobrem null, primeiro/último dia do mês e 29 de fevereiro.
