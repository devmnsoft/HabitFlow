insert into habitflow.system_settings(key,value,updated_at) values
('company','{"name":"MNSOFT","legalName":"MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA","cnpj":"18.160.057/0001-13","email":"comercial@mnsoft.com.br"}'::jsonb,now()),
('whatsapp','{"enabled":true,"number":"+5511999999999","defaultMessage":"Olá, preciso de suporte no HabitFlow."}'::jsonb,now())
on conflict(key) do nothing;
