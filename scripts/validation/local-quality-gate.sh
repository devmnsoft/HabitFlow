#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
REPORTS="$ROOT/artifacts/validation"
mkdir -p "$REPORTS"
cd "$ROOT"

run_logged() {
  local name="$1"; shift
  echo "==> $name: $*"
  "$@" 2>&1 | tee "$REPORTS/$name.log"
}

python3 scripts/validation/quality_validators.py all
run_logged dotnet-clean dotnet clean HabitFlow.sln
run_logged dotnet-restore dotnet restore HabitFlow.sln
run_logged dotnet-build dotnet build HabitFlow.sln -c Release --no-restore
run_logged dotnet-test dotnet test HabitFlow.sln -c Release --no-build
run_logged npm-ci npm ci
run_logged npm-test npm test
run_logged security-scan npm run security:scan
run_logged npm-audit npm audit --omit=dev
run_logged git-diff-check git diff --check

# Database validation is mandatory but does not require a live database: the .NET
# test suite checks migration ordering/content. A live PostgreSQL replay remains
# available through validate-postgres-migrations.ps1 when a connection is supplied.
run_logged migration-contract dotnet test tests/HabitFlow.Tests/HabitFlow.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~DatabaseScriptTests|FullyQualifiedName~MigrationGovernance'

echo "Quality gate passed. Reports and logs: $REPORTS"
