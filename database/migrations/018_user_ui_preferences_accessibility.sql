create table if not exists habitflow.user_ui_preferences (
  id uuid primary key,
  user_id uuid not null references habitflow.users(id) on delete cascade,
  contrast_mode varchar(50) not null default 'Default',
  font_scale varchar(50) not null default 'Normal',
  reduce_motion boolean not null default false,
  created_at timestamp not null default now(),
  updated_at timestamp not null default now(),
  constraint user_ui_preferences_user_unique unique(user_id),
  constraint user_ui_preferences_contrast_check check (contrast_mode in ('Default','HighContrast')),
  constraint user_ui_preferences_font_check check (font_scale in ('Normal','Large'))
);
create index if not exists ix_user_ui_preferences_user_id on habitflow.user_ui_preferences(user_id);
