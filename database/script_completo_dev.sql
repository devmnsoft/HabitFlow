\set ON_ERROR_STOP on
-- Development bootstrap for a NEW disposable database only.
-- The production aggregate remains the single schema source of truth.
-- Never run this wrapper and scripts/database/run-migrations.sh on the same DB.
\ir script_completo.sql
\ir seed_dev.sql
