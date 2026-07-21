-- v4.9 Guided Journey + Habit Library
create table if not exists habitflow.habit_objectives (
    id uuid primary key,
    slug varchar(80) not null unique,
    name varchar(120) not null,
    description text not null,
    icon varchar(80) null,
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamp not null default now()
);

create table if not exists habitflow.habit_templates (
    id uuid primary key,
    objective_id uuid not null references habitflow.habit_objectives(id) on delete cascade,
    name varchar(120) not null,
    description text not null,
    category varchar(80) not null,
    suggested_frequency varchar(50) not null default 'Daily',
    suggested_color varchar(20) not null default '#10B981',
    difficulty varchar(50) not null default 'Easy',
    estimated_time_minutes integer null,
    benefit_text text null,
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    constraint ck_habitflow_habit_templates_frequency check (suggested_frequency in ('Daily','Weekdays','Weekends','CustomWeekly')),
    constraint ck_habitflow_habit_templates_difficulty check (difficulty in ('Easy','Medium','Hard')),
    constraint uq_habitflow_habit_templates_objective_name unique(objective_id, name)
);

create index if not exists ix_habitflow_habit_objectives_slug on habitflow.habit_objectives(slug);
create index if not exists ix_habitflow_habit_templates_objective_id on habitflow.habit_templates(objective_id);
create index if not exists ix_habitflow_habit_templates_category on habitflow.habit_templates(category);
create index if not exists ix_habitflow_habit_templates_is_active on habitflow.habit_templates(is_active);

insert into habitflow.habit_objectives(id, slug, name, description, icon, sort_order, is_active) values
('10000000-0000-0000-0000-000000000001','saude','Saúde','Hábitos simples para cuidar do corpo e ter mais energia.','♥',1,true),
('10000000-0000-0000-0000-000000000002','estudos','Estudos','Rotinas curtas para aprender com consistência.','✦',2,true),
('10000000-0000-0000-0000-000000000003','produtividade','Produtividade','Ações práticas para organizar prioridades e finalizar tarefas.','→',3,true),
('10000000-0000-0000-0000-000000000004','bem-estar','Bem-estar','Pausas e cuidados para reduzir tensão e melhorar o dia.','☼',4,true),
('10000000-0000-0000-0000-000000000005','organizacao','Organização','Pequenos hábitos para deixar sua rotina mais leve.','▣',5,true)
on conflict(slug) do update set name=excluded.name, description=excluded.description, icon=excluded.icon, sort_order=excluded.sort_order, is_active=excluded.is_active;

with data(slug,name,description,category,suggested_color,difficulty,estimated_time_minutes,benefit_text,sort_order) as (values
('saude','Beber água','Beba um copo de água e registre o cuidado com seu corpo.','Saúde','#10B981','Easy',2,'Ajuda a manter hidratação e energia ao longo do dia.',1),('saude','Caminhar 20 minutos','Faça uma caminhada leve em ritmo confortável.','Saúde','#22C55E','Easy',20,'Movimenta o corpo e melhora disposição.',2),('saude','Dormir antes das 23h','Prepare uma rotina para deitar mais cedo.','Saúde','#6366F1','Medium',30,'Sono regular favorece recuperação e foco.',3),('saude','Alongar por 5 minutos','Alongue pescoço, ombros e pernas com calma.','Saúde','#14B8A6','Easy',5,'Reduz tensão e melhora mobilidade.',4),('saude','Comer uma fruta','Inclua uma fruta em algum momento do dia.','Saúde','#F59E0B','Easy',5,'Facilita uma escolha alimentar simples e positiva.',5),
('estudos','Estudar 30 minutos','Reserve um bloco curto para estudar com foco.','Estudos','#0EA5E9','Easy',30,'Cria constância sem depender de longas sessões.',1),('estudos','Revisar anotações','Revise pontos importantes do conteúdo recente.','Estudos','#3B82F6','Easy',15,'Ajuda a fixar o aprendizado.',2),('estudos','Ler 10 páginas','Leia dez páginas de um livro ou material de estudo.','Estudos','#8B5CF6','Easy',20,'Mantém contato diário com conhecimento.',3),('estudos','Fazer exercícios','Resolva questões para praticar o conteúdo.','Estudos','#06B6D4','Medium',25,'Transforma teoria em prática.',4),('estudos','Organizar material','Separe materiais e tarefas da próxima sessão.','Estudos','#64748B','Easy',10,'Reduz atrito para começar amanhã.',5),
('produtividade','Planejar o dia','Defina as prioridades antes de começar.','Produtividade','#0B4EA2','Easy',10,'Dá clareza para agir com menos dispersão.',1),('produtividade','Organizar tarefas','Atualize sua lista de tarefas em poucos minutos.','Produtividade','#2563EB','Easy',10,'Evita esquecimentos e melhora foco.',2),('produtividade','Evitar celular por 30 minutos','Faça um bloco sem celular para avançar em algo importante.','Produtividade','#7C3AED','Medium',30,'Diminui distrações e aumenta presença.',3),('produtividade','Revisar prioridades','Confira se o esforço do dia está no que importa.','Produtividade','#0891B2','Easy',10,'Ajuda a corrigir a rota cedo.',4),('produtividade','Finalizar uma pendência','Escolha uma pendência pequena e conclua.','Produtividade','#DC2626','Medium',20,'Gera sensação de avanço real.',5),
('bem-estar','Meditar 5 minutos','Faça uma pausa silenciosa e observe a respiração.','Bem-estar','#10B981','Easy',5,'Ajuda a reduzir ansiedade e recomeçar com calma.',1),('bem-estar','Respirar profundamente','Faça ciclos de respiração lenta por alguns minutos.','Bem-estar','#14B8A6','Easy',3,'Acalma o corpo rapidamente.',2),('bem-estar','Escrever gratidão','Anote uma coisa boa do dia.','Bem-estar','#F59E0B','Easy',5,'Treina atenção para progresso e momentos positivos.',3),('bem-estar','Fazer pausa consciente','Pare, levante e retome com intenção.','Bem-estar','#06B6D4','Easy',5,'Previne cansaço mental acumulado.',4),('bem-estar','Ouvir uma música relaxante','Ouça uma música com atenção e sem multitarefa.','Bem-estar','#8B5CF6','Easy',5,'Cria uma pausa emocional simples.',5),
('organizacao','Arrumar a cama','Organize a cama ao acordar.','Organização','#0EA5E9','Easy',5,'Começa o dia com uma pequena vitória.',1),('organizacao','Organizar mesa','Deixe sua mesa limpa para o próximo bloco.','Organização','#64748B','Easy',10,'Reduz distrações visuais.',2),('organizacao','Revisar agenda','Confira compromissos e horários do dia.','Organização','#2563EB','Easy',5,'Evita surpresas e melhora preparo.',3),('organizacao','Separar roupa do dia seguinte','Escolha a roupa antes de dormir.','Organização','#7C3AED','Easy',5,'Diminui decisões pela manhã.',4),('organizacao','Limpar caixa de entrada','Arquive ou responda mensagens importantes.','Organização','#0B4EA2','Medium',15,'Mantém comunicação sob controle.',5)
)
insert into habitflow.habit_templates(id, objective_id, name, description, category, suggested_frequency, suggested_color, difficulty, estimated_time_minutes, benefit_text, sort_order, is_active)
select (substr(md5(o.slug || ':' || d.name),1,8)||'-'||substr(md5(o.slug || ':' || d.name),9,4)||'-'||substr(md5(o.slug || ':' || d.name),13,4)||'-'||substr(md5(o.slug || ':' || d.name),17,4)||'-'||substr(md5(o.slug || ':' || d.name),21,12))::uuid, o.id, d.name, d.description, d.category, 'Daily', d.suggested_color, d.difficulty, d.estimated_time_minutes, d.benefit_text, d.sort_order, true
from data d join habitflow.habit_objectives o on o.slug=d.slug
on conflict(objective_id, name) do update set description=excluded.description, category=excluded.category, suggested_color=excluded.suggested_color, difficulty=excluded.difficulty, estimated_time_minutes=excluded.estimated_time_minutes, benefit_text=excluded.benefit_text, sort_order=excluded.sort_order, is_active=true, updated_at=now();
