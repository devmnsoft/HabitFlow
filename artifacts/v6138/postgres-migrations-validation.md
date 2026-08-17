# PostgreSQL migrations — v6.13.8

O job usa PostgreSQL 17 real e executa `validate-postgres-migrations.ps1` contra banco existente, banco novo temporário e rerun. O helper valida o registro 001–065, tabelas da jornada, nulabilidade/escopo, templates publicados, preços ativos e catálogo comercial.

Status: **pendente de execução no GitHub Actions**. O relatório deste caminho será substituído pelo artifact real produzido pelo job; não há aprovação local porque PostgreSQL, psql e PowerShell estão ausentes.
