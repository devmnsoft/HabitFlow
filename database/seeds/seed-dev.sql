insert into habitflow.users(id,name,email,password_hash,role,account_status,risk_status,plan,plan_status,created_at,updated_at) values
('00000000-0000-0000-0000-000000000001','Admin Local','admin@habitflow.local','$2a$11$C6UzMDM.H6dfI/f/IKcEeO6UAbPpsQKHBqNDpOoFBqBa6hG7vMA9G','Admin','Active','Normal','Premium','Active',now(),now()),
('00000000-0000-0000-0000-000000000002','User Local','user@habitflow.local','$2a$11$C6UzMDM.H6dfI/f/IKcEeO6UAbPpsQKHBqNDpOoFBqBa6hG7vMA9G','User','Active','Normal','Free','Active',now(),now())
on conflict(email) do nothing;
