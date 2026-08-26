-- HabitFlow v6.18.0 - corporate collaboration, privacy-first and tenant isolated.
begin;
create table if not exists habitflow.organization_members (
 client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 role varchar(30) not null check(role in ('Owner','Admin','TeamManager','Member','ReportReader')),
 is_active boolean not null default true, created_at timestamptz not null default now(), primary key(client_id,user_id));
create table if not exists habitflow.teams (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), name varchar(120) not null,
 description varchar(500), is_archived boolean not null default false, created_at timestamptz not null default now(), updated_at timestamptz not null default now(), unique(client_id,name));
create table if not exists habitflow.team_members (
 client_id uuid not null, team_id uuid not null, user_id uuid not null references habitflow.users(id), is_manager boolean not null default false,
 joined_at timestamptz not null default now(), primary key(client_id,team_id,user_id), foreign key(team_id) references habitflow.teams(id) on delete restrict);
create table if not exists habitflow.team_invitations (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), team_id uuid references habitflow.teams(id), email varchar(320) not null,
 role varchar(30) not null check(role in ('Admin','TeamManager','Member','ReportReader')), token_hash char(64) not null unique,
 status varchar(20) not null check(status in ('Pending','Accepted','Declined','Cancelled','Expired')), sent_at timestamptz not null,
 expires_at timestamptz not null, responded_at timestamptz, invited_by uuid not null references habitflow.users(id), check(expires_at>sent_at));
create table if not exists habitflow.corporate_programs (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), name varchar(160) not null, description varchar(1000) not null,
 objective varchar(500) not null, starts_on date not null, ends_on date not null, audience varchar(300) not null,
 status varchar(20) not null check(status in ('Draft','Active','Paused','Ended','Archived')), owner_user_id uuid not null references habitflow.users(id),
 allow_leaving boolean not null default true, created_at timestamptz not null default now(), updated_at timestamptz not null default now(), check(ends_on>=starts_on));
create table if not exists habitflow.corporate_program_teams (client_id uuid not null, program_id uuid not null references habitflow.corporate_programs(id), team_id uuid not null references habitflow.teams(id), primary key(client_id,program_id,team_id));
create table if not exists habitflow.corporate_program_habits (client_id uuid not null, program_id uuid not null references habitflow.corporate_programs(id), habit_template_id uuid not null references habitflow.habit_templates(id), is_optional boolean not null default true, primary key(client_id,program_id,habit_template_id), check(is_optional));
create table if not exists habitflow.corporate_program_members (client_id uuid not null, program_id uuid not null references habitflow.corporate_programs(id), user_id uuid not null references habitflow.users(id), joined_at timestamptz not null default now(), left_at timestamptz, consented_at timestamptz not null, primary key(client_id,program_id,user_id));
create table if not exists habitflow.team_challenges (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), team_id uuid not null references habitflow.teams(id), program_id uuid references habitflow.corporate_programs(id),
 name varchar(160) not null, goal varchar(500) not null, starts_on date not null, ends_on date not null, target integer not null check(target>0), is_collective boolean not null,
 ranking_enabled boolean not null default false, status varchar(20) not null check(status in ('Draft','Active','Finished','Cancelled','Archived')), created_at timestamptz not null default now(), check(ends_on>=starts_on));
create table if not exists habitflow.team_challenge_progress (client_id uuid not null, challenge_id uuid not null references habitflow.team_challenges(id), user_id uuid not null references habitflow.users(id), progress integer not null default 0 check(progress>=0), opted_in boolean not null default false, updated_at timestamptz not null default now(), primary key(client_id,challenge_id,user_id));
create table if not exists habitflow.privacy_preferences (client_id uuid not null, user_id uuid not null references habitflow.users(id), habits_private boolean not null default true, share_program_progress boolean not null default false, updated_at timestamptz not null default now(), primary key(client_id,user_id));
create index if not exists ix_teams_tenant_status on habitflow.teams(client_id,is_archived);
create index if not exists ix_team_members_tenant_team on habitflow.team_members(client_id,team_id);
create index if not exists ix_invitations_tenant_status_expiry on habitflow.team_invitations(client_id,status,expires_at);
create index if not exists ix_programs_tenant_status_period on habitflow.corporate_programs(client_id,status,starts_on,ends_on);
create index if not exists ix_challenges_tenant_team_status on habitflow.team_challenges(client_id,team_id,status);
commit;
