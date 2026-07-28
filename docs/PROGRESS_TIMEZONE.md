# Timezone de progresso

O identificador preferencial é IANA (`America/Sao_Paulo`). A resolução tenta também o identificador Windows `E. South America Standard Time`, trata zonas ausentes/inválidas e usa UTC como último fallback, registrando warning. `created_at`/`archived_at` continuam materializados como `DateTime`; nesta correção a convenção existente os normaliza explicitamente como UTC antes da conversão local, sem migration silenciosa de tipo.
