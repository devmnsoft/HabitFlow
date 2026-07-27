# Locking e idempotência dos jobs

A migration 041 adota **lease em tabela**: uma linha por `job_name`, identificada por `locked_by`, com expiração em `lock_expires_at`. A aquisição deve ser um `INSERT ... ON CONFLICT DO UPDATE` condicionado a lock livre, expirado ou pertencente ao mesmo worker. O worker que não adquirir encerra silenciosamente. A liberação somente pode limpar uma lease cujo `locked_by` seja o seu identificador.

Entregas usam `notification_deliveries` e sua chave única de origem, canal e instante agendado. Assim, repetição após falha ou retomada de lock expirado não duplica a notificação. Cada processador deve trabalhar em lotes, respeitar cancelamento e registrar sucesso ou falha nos logs de execução já existentes.
