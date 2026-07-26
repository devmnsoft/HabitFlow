# HabitFlow v6.1.2

Esta versão consolida cadastro PF/PJ com CPF/CNPJ validado, claim tenant `client_id`, cadastro transacional, onboarding Admin, área Minha Conta/Minha Empresa e painel SuperAdmin Registration Quality.

- Admin/User autenticados recebem e usam `client_id`.
- SuperAdmin opera sem vínculo obrigatório de cliente.
- Cadastro público cria Client, Admin, onboarding, comunicação e auditoria em uma transação.
- CPF/CNPJ completo é restrito ao Admin do próprio cliente e ao SuperAdmin.
- Exportação CSV SuperAdmin aplica proteção contra CSV injection.
