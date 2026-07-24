# HabitFlow v6.1.1 — Cadastro SaaS PF/PJ com CPF/CNPJ

O fluxo público `/register` cria um cliente SaaS completo e o primeiro usuário administrador vinculado por `client_id`.

- Pessoa Física usa `person_type = NaturalPerson` e `document_type = CPF`.
- Pessoa Jurídica usa `person_type = LegalPerson` e `document_type = CNPJ`.
- `document_raw` guarda o documento formatado e `document_normalized` guarda apenas números.
- O documento é validado no backend, normalizado e protegido por índice único parcial em `habitflow.clients`.
- O cliente nasce no plano Free, com `subscription_status = Free`, `benefits_status = Free` e `payment_status = None`.
- O primeiro usuário nasce como `Admin`, `Active`, `Free` e com `client_id` preenchido.
- O onboarding inicial e a comunicação de boas-vindas são criados no cadastro.
- Dados fiscais completos devem ser visíveis apenas para SuperAdmin ou Admin do próprio cliente.
