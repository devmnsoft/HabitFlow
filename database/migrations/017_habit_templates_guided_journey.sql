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
('10000000-0000-0000-0000-000000000005','organizacao','Organização','Pequenos hábitos para deixar sua rotina mais leve.','▣',5,true),
('10000000-0000-0000-0000-000000000006','sono','Sono','Rituais para noites mais consistentes e restauradoras.','☾',6,true),
('10000000-0000-0000-0000-000000000007','exercicio','Exercício','Movimentos simples para ganhar disposição.','✓',7,true),
('10000000-0000-0000-0000-000000000008','leitura','Leitura','Leitura leve e constante para evoluir todos os dias.','◇',8,true)
on conflict(slug) do update set name=excluded.name, description=excluded.description, icon=excluded.icon, sort_order=excluded.sort_order, is_active=excluded.is_active;

with data(slug,name,description,category,suggested_color,difficulty,estimated_time_minutes,benefit_text,sort_order) as (values
('saude','Beber água','Sugestão prática: beber água com constância e leveza.','Saúde','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('saude','Comer uma fruta','Sugestão prática: comer uma fruta com constância e leveza.','Saúde','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('saude','Alongar por 5 minutos','Sugestão prática: alongar por 5 minutos com constância e leveza.','Saúde','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('saude','Caminhar 20 minutos','Sugestão prática: caminhar 20 minutos com constância e leveza.','Saúde','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('saude','Evitar refrigerante','Sugestão prática: evitar refrigerante com constância e leveza.','Saúde','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('estudos','Estudar 30 minutos','Sugestão prática: estudar 30 minutos com constância e leveza.','Estudos','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('estudos','Revisar anotações','Sugestão prática: revisar anotações com constância e leveza.','Estudos','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('estudos','Ler 10 páginas','Sugestão prática: ler 10 páginas com constância e leveza.','Estudos','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('estudos','Resolver exercícios','Sugestão prática: resolver exercícios com constância e leveza.','Estudos','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('estudos','Organizar material','Sugestão prática: organizar material com constância e leveza.','Estudos','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('produtividade','Planejar o dia','Sugestão prática: planejar o dia com constância e leveza.','Produtividade','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('produtividade','Revisar prioridades','Sugestão prática: revisar prioridades com constância e leveza.','Produtividade','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('produtividade','Evitar celular por 30 minutos','Sugestão prática: evitar celular por 30 minutos com constância e leveza.','Produtividade','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('produtividade','Finalizar uma pendência','Sugestão prática: finalizar uma pendência com constância e leveza.','Produtividade','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('produtividade','Organizar tarefas','Sugestão prática: organizar tarefas com constância e leveza.','Produtividade','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('bem-estar','Meditar 5 minutos','Sugestão prática: meditar 5 minutos com constância e leveza.','Bem-estar','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('bem-estar','Respirar profundamente','Sugestão prática: respirar profundamente com constância e leveza.','Bem-estar','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('bem-estar','Escrever gratidão','Sugestão prática: escrever gratidão com constância e leveza.','Bem-estar','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('bem-estar','Fazer pausa consciente','Sugestão prática: fazer pausa consciente com constância e leveza.','Bem-estar','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('bem-estar','Ouvir música relaxante','Sugestão prática: ouvir música relaxante com constância e leveza.','Bem-estar','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('organizacao','Arrumar a cama','Sugestão prática: arrumar a cama com constância e leveza.','Organização','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('organizacao','Organizar mesa','Sugestão prática: organizar mesa com constância e leveza.','Organização','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('organizacao','Revisar agenda','Sugestão prática: revisar agenda com constância e leveza.','Organização','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('organizacao','Separar roupa do dia seguinte','Sugestão prática: separar roupa do dia seguinte com constância e leveza.','Organização','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('organizacao','Limpar caixa de entrada','Sugestão prática: limpar caixa de entrada com constância e leveza.','Organização','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('sono','Dormir antes das 23h','Sugestão prática: dormir antes das 23h com constância e leveza.','Sono','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('sono','Evitar telas antes de dormir','Sugestão prática: evitar telas antes de dormir com constância e leveza.','Sono','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('sono','Preparar ambiente','Sugestão prática: preparar ambiente com constância e leveza.','Sono','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('sono','Fazer rotina noturna','Sugestão prática: fazer rotina noturna com constância e leveza.','Sono','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('sono','Acordar no mesmo horário','Sugestão prática: acordar no mesmo horário com constância e leveza.','Sono','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('exercicio','Caminhar','Sugestão prática: caminhar com constância e leveza.','Exercício','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('exercicio','Fazer 10 flexões','Sugestão prática: fazer 10 flexões com constância e leveza.','Exercício','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('exercicio','Alongar','Sugestão prática: alongar com constância e leveza.','Exercício','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('exercicio','Subir escadas','Sugestão prática: subir escadas com constância e leveza.','Exercício','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('exercicio','Treino leve','Sugestão prática: treino leve com constância e leveza.','Exercício','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('leitura','Ler 10 páginas','Sugestão prática: ler 10 páginas com constância e leveza.','Leitura','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('leitura','Ler 15 minutos','Sugestão prática: ler 15 minutos com constância e leveza.','Leitura','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('leitura','Anotar uma ideia','Sugestão prática: anotar uma ideia com constância e leveza.','Leitura','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('leitura','Revisar leitura anterior','Sugestão prática: revisar leitura anterior com constância e leveza.','Leitura','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('leitura','Separar próximo livro','Sugestão prática: separar próximo livro com constância e leveza.','Leitura','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5)
)
insert into habitflow.habit_templates(id, objective_id, name, description, category, suggested_frequency, suggested_color, difficulty, estimated_time_minutes, benefit_text, sort_order, is_active)
select (substr(md5(o.slug || ':' || d.name),1,8)||'-'||substr(md5(o.slug || ':' || d.name),9,4)||'-'||substr(md5(o.slug || ':' || d.name),13,4)||'-'||substr(md5(o.slug || ':' || d.name),17,4)||'-'||substr(md5(o.slug || ':' || d.name),21,12))::uuid, o.id, d.name, d.description, d.category, 'Daily', d.suggested_color, d.difficulty, d.estimated_time_minutes, d.benefit_text, d.sort_order, true
from data d join habitflow.habit_objectives o on o.slug=d.slug
on conflict(objective_id, name) do update set description=excluded.description, category=excluded.category, suggested_color=excluded.suggested_color, difficulty=excluded.difficulty, estimated_time_minutes=excluded.estimated_time_minutes, benefit_text=excluded.benefit_text, sort_order=excluded.sort_order, is_active=true, updated_at=now();
