#!/usr/bin/env bash
set -euo pipefail

if command -v rg >/dev/null 2>&1; then
  search() { rg -n "$@"; }
elif command -v grep >/dev/null 2>&1; then
  # Translate the small subset of rg options used by this gate.  Excluded
  # generated directories are handled by grep itself, not by the caller.
  search() {
    local insensitive=0
    if [[ "${1:-}" == "-i" ]]; then insensitive=1; shift; fi
    [[ "${1:-}" == "--" ]] && shift
    local expression="$1"; shift
    local options=(-R -n -E --exclude-dir=bin --exclude-dir=obj --exclude-dir=node_modules)
    (( insensitive )) && options+=(-i)
    grep "${options[@]}" -- "$expression" "$@"
  }
else
  echo 'Security gate: nenhuma ferramenta de busca compatível (rg ou grep) está disponível.' >&2
  exit 2
fi

fail=0
if search -i "(password|senha)[[:space:]]*=[[:space:]]*['\"][[:alnum:]!@#%&*_-]{6,}['\"]" database scripts .github src; then echo '[SEC001] possível senha literal' >&2; fail=1; fi
if search "password_hash[[:space:]]*=[[:space:]]*['\"][^:@]" database scripts; then echo '[SEC002] hash literal' >&2; fail=1; fi
if search -- "--password([ =]|$)" scripts .github; then echo '[SEC003] senha em argumento de CLI' >&2; fail=1; fi
if search "implementation_status='Partial'.*is_marketable=true|is_marketable=true.*implementation_status='Partial'" database; then echo '[PLAN001] feature Partial comercializável' >&2; fail=1; fi
exit "$fail"
