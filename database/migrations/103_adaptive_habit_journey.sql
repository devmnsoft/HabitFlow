-- HabitFlow adaptive habit journey: schedule validity, quantity targets and auditable adjustment window.

alter table habitflow.habits add column if not exists end_date date null;
alter table habitflow.habits add column if not exists target_quantity numeric(12,2) null;
alter table habitflow.habits add column if not exists target_unit varchar(32) null;
alter table habitflow.habits add column if not exists minimum_version_name varchar(120) null;
alter table habitflow.habits add column if not exists minimum_version_quantity numeric(12,2) null;
alter table habitflow.habits add column if not exists retroactive_adjustment_days integer not null default 7;

alter table habitflow.habit_completions add column if not exists recorded_quantity numeric(12,2) null;
alter table habitflow.habit_completions add column if not exists recorded_unit varchar(32) null;
alter table habitflow.habit_completions add column if not exists completion_mode varchar(24) not null default 'Binary';

do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_habits_validity_range') then
        alter table habitflow.habits add constraint ck_habits_validity_range
            check (end_date is null or start_date is null or end_date >= start_date);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ck_habits_quantity_unit_pair') then
        alter table habitflow.habits add constraint ck_habits_quantity_unit_pair
            check ((target_quantity is null and target_unit is null) or (target_quantity > 0 and target_unit is not null));
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ck_habits_minimum_quantity') then
        alter table habitflow.habits add constraint ck_habits_minimum_quantity
            check (minimum_version_quantity is null or minimum_version_quantity > 0);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ck_habits_retroactive_window') then
        alter table habitflow.habits add constraint ck_habits_retroactive_window
            check (retroactive_adjustment_days between 0 and 31);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ck_habit_completions_quantity_unit_pair') then
        alter table habitflow.habit_completions add constraint ck_habit_completions_quantity_unit_pair
            check ((recorded_quantity is null and recorded_unit is null) or (recorded_quantity >= 0 and recorded_unit is not null));
    end if;
    if not exists (select 1 from pg_constraint where conname = 'ck_habit_completions_mode') then
        alter table habitflow.habit_completions add constraint ck_habit_completions_mode
            check (completion_mode in ('Binary','Quantity'));
    end if;
end $$;

create index if not exists ix_habits_user_validity
on habitflow.habits(client_id, user_id, start_date, end_date)
where is_archived = false;

