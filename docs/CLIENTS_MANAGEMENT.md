# Gestão de clientes

Cliente é a empresa ou pessoa que usa o HabitFlow. Administradores acessam `/admin/clients`, cadastram em `/admin/clients/create`, editam pelo detalhe e podem ativar, desativar ou bloquear com motivo auditável.

Mensagens: cadastro e edição exibem toast de sucesso; bloqueio/desativação exibem aviso; erros de banco exibem modal amigável. O vínculo com usuários está preparado por `habitflow.users.client_id` para evolução futura.
