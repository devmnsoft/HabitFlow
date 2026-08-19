# HabitFlow v6.15.4 CI release gate

- Initial SHA: `4f68a27e6f70ea814153490b795f2482d06dd94f`.
- Final SHA: generated from `GITHUB_SHA` in the executed workflow report; the commit containing this document does not predeclare its own SHA.
- Workflow: `HabitFlow .NET Release Gate`.
- Jobs: build/publish, PostgreSQL migrations, public runtime smoke, authenticated runtime smoke, and artifact summary.
- Build/publish: configured as a real Release build and publish; the output is an Actions artifact, not tracked in Git.
- Migrations: canonical 001–066 run twice against PostgreSQL 17, with registry, required-table, LGPD table, drift and audit-trigger assertions.
- Startup/public smoke: published DLL starts with a disposable PostgreSQL service and checks `/`, `/login`, `/register`, `/plans`, and `/favicon.ico`.
- Authenticated smoke: an ephemeral masked password provisions a tenant-bound Free user and checks all required MVC routes, including `/account/privacy` and the habit library.
- Minimum MVP: login and required read-only navigation are automated. Habit creation/completion, Free limit behavior at its boundary, real-browser UX and mobile behavior remain manual and are not declared approved.
- Secrets: no production secret or real connection string is stored. The workflow values are disposable service-container credentials only.

## Decision

**CI Release Gate pronto para execução**

This means the executable gate is implemented. Each workflow run produces the real pass/fail report, and any failed job changes its decision to **Não aprovado — erro real pendente**.
