# Diagnóstico de acesso a planos

O diagnóstico privilegiado em `/superadmin/system-health/plan-access` deve validar leitura de `habitflow.clients`, OID/tipo `date`, códigos contratado/efetivo conhecidos, recursos, ausência de plano e grace period incoerente. Exponha somente totais e ids mascarados; jamais documentos, hábitos ou dados pessoais. Contadores operacionais: `plan_access_query_failures`, `dapper_materialization_failures`, `navigation_feature_failures`, `unknown_plan_codes`, `invalid_benefits_status` e `error_page_count`.
