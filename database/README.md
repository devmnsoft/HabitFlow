# Banco HabitFlow
Execute `psql -U postgres -d habitflow -f database/migrate.sql`. Use `database/seeds/seed-dev.sql` somente em desenvolvimento; produção deve usar `seed-prd.sql` e secrets locais.
