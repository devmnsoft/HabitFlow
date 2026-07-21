alter table habitflow.user_ui_preferences add column if not exists show_achievement_popups boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists show_tip_popups boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists enable_toasts boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists reduce_popups boolean not null default false;
