-- v6.12.3: repair the persisted Habit/domain contract without rewriting historical migrations.
update habitflow.habits
set start_date = created_at::date
where start_date is null;

alter table habitflow.habits
    alter column start_date set default current_date,
    alter column start_date set not null;

create index if not exists ix_goal_habits_tenant_goal
    on habitflow.goal_habits(client_id, goal_id, habit_id);
