#!/usr/bin/env bash
psql "$DATABASE_URL" -f database/migrate.sql
