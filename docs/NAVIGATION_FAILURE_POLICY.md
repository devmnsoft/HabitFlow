# Política de falha da navegação

Permissão inválida e usuário sem `NameIdentifier` negam acesso. Feature nula passa depois da permissão. Features são carregadas em lote e mantidas somente no `RequestPlanAccessContext` scoped. Falhas são registradas e negam apenas itens dependentes; cancelamento solicitado é relançado. Itens básicos e **Meu plano** continuam disponíveis. SuperAdmin não ganha recurso pago por fallback e menus público/Platform não consultam plano quando não há feature.
