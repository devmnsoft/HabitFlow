-- transaction-mode: runner-managed
create table if not exists habitflow.product_tips(
 id uuid primary key,
 code varchar(80) not null unique,
 route_pattern varchar(160) not null,
 target_selector varchar(200) not null,
 title varchar(120) not null,
 content varchar(400) not null,
 display_order integer not null default 0,
 is_active boolean not null default true,
 created_at timestamptz not null default now()
);
create table if not exists habitflow.user_product_tips(
 user_id uuid not null references habitflow.users(id) on delete cascade,
 product_tip_id uuid not null references habitflow.product_tips(id) on delete cascade,
 seen_at timestamptz null,
 dismissed_at timestamptz null,
 updated_at timestamptz not null default now(),
 primary key(user_id,product_tip_id)
);
create index if not exists ix_user_product_tips_pending on habitflow.user_product_tips(user_id,dismissed_at);

insert into habitflow.product_tips(id,code,route_pattern,target_selector,title,content,display_order) values
('62000000-0000-0000-0000-000000000001','dashboard','/dashboard%','#conteudo','Seu painel diário','Veja consistência, próximos passos e alertas do seu plano usando somente seus dados reais.',10),
('62000000-0000-0000-0000-000000000002','header','/%','[data-app-header]','Navegação rápida','Use a busca, o menu Novo e suas notificações sem sair do contexto atual.',20),
('62000000-0000-0000-0000-000000000003','my-day','/my-day%','#conteudo','Organize o seu dia','Conclua hábitos planejados e acompanhe o que ainda precisa da sua atenção.',30),
('62000000-0000-0000-0000-000000000004','progress','/progress%','#conteudo','Entenda seu progresso','Explore o calendário para reconhecer padrões de consistência ao longo do tempo.',40),
('62000000-0000-0000-0000-000000000005','reports','/reports%','#conteudo','Leia seus relatórios','Compare períodos e transforme seus resultados em próximos passos possíveis.',50),
('62000000-0000-0000-0000-000000000006','library','/habit-library%','#conteudo','Descubra hábitos','Revise cada sugestão antes de ativá-la e adapte-a à sua rotina.',60),
('62000000-0000-0000-0000-000000000007','plan','/account/plan%','#conteudo','Acompanhe seu plano','Confira limites e uso antes de decidir por qualquer alteração.',70),
('62000000-0000-0000-0000-000000000008','security','/account/security%','#conteudo','Proteja sua conta','Revise sessões, senha e autenticação para manter seus dados seguros.',80),
('62000000-0000-0000-0000-000000000009','privacy','/account/privacy%','#conteudo','Controle sua privacidade','Gerencie preferências e solicitações relacionadas aos seus dados.',90),
('62000000-0000-0000-0000-000000000010','notifications','/notifications%','#conteudo','Sua central de notificações','Filtre, leia e arquive avisos operacionais vinculados à sua conta.',100)
on conflict(code) do update set route_pattern=excluded.route_pattern,target_selector=excluded.target_selector,title=excluded.title,content=excluded.content,display_order=excluded.display_order;

