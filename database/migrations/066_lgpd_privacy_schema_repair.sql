begin;

create table if not exists habitflow.user_privacy_consents (
    user_id uuid not null references habitflow.users(id) on delete cascade,
    consent_key varchar(60) not null,
    granted boolean not null,
    updated_at timestamp not null default now(),
    primary key (user_id, consent_key),
    constraint ck_user_privacy_consents_key
        check (consent_key in ('analytics', 'communications'))
);

create table if not exists habitflow.privacy_request_events (
    id bigint generated always as identity primary key,
    request_id uuid not null references habitflow.lgpd_requests(id) on delete cascade,
    event_type varchar(40) not null,
    status varchar(50) not null,
    occurred_at timestamp not null default now()
);

create index if not exists ix_privacy_request_events_request
    on habitflow.privacy_request_events(request_id, occurred_at desc);

create or replace function habitflow.audit_privacy_request()
returns trigger
language plpgsql
as $$
begin
    insert into habitflow.privacy_request_events(request_id, event_type, status)
    values (
        new.id,
        case when tg_op = 'INSERT' then 'requested' else 'status_changed' end,
        new.status
    );
    return new;
end
$$;

drop trigger if exists trg_audit_privacy_request on habitflow.lgpd_requests;

create trigger trg_audit_privacy_request
after insert or update of status on habitflow.lgpd_requests
for each row execute function habitflow.audit_privacy_request();

commit;
