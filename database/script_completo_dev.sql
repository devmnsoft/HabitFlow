-- HabitFlow - complemento de desenvolvimento. Execute após database/script_completo.sql.
-- Usuários dev: admin@habitflow.local e user@habitflow.local. Senha documentada: Admin@123

insert into habitflow.users(id, name, email, password_hash, role, account_status, risk_status, plan, plan_status, wants_premium_notice, onboarding_completed, accepted_terms_at, accepted_privacy_at, created_at, updated_at)
values
    ('00000000-0000-0000-0000-000000000001', 'Admin Dev', 'admin@habitflow.local', '$2a$11$CwTycUXWue0Thq9StjUM0uJ8wQzJsGMjFbc3ziqE9K28dEe/O8RQq', 'Admin', 'Active', 'Normal', 'Premium', 'Active', false, true, now(), now(), now(), now()),
    ('00000000-0000-0000-0000-000000000002', 'Usuário Dev', 'user@habitflow.local', '$2a$11$CwTycUXWue0Thq9StjUM0uJ8wQzJsGMjFbc3ziqE9K28dEe/O8RQq', 'User', 'Active', 'Normal', 'Free', 'Active', false, true, now(), now(), now(), now())
on conflict(email) do nothing;
