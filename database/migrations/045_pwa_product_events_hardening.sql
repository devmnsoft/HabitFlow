BEGIN;
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_events_pwa_install_day
 ON habitflow.product_events(user_id,event_name,(occurred_at::date)) WHERE event_name='pwa_installed' AND user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_product_events_analytics ON habitflow.product_events(event_name,occurred_at DESC,client_id);
COMMIT;
