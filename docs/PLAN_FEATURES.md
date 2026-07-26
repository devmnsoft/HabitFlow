# Recursos dos planos

`feature_catalog` define tipo e texto; `plan_features` guarda o valor por plano. Inteiros negativos representam ilimitado. Recurso ausente é negado de forma segura. Controllers consultam `PlanEntitlementService`; a página pública recebe nomes já preparados e não interpreta códigos.
