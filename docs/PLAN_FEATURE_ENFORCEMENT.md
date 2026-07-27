# Aplicação de recursos por plano
`FeatureAccessService` é a fachada de decisões. Booleanos e limites vêm de `PlanEntitlementService` e do plano efetivo. Controllers não comparam nomes de planos nem usam limites fixos. Valores negativos representam ilimitado.
