#!/usr/bin/env bash
set -euo pipefail
fail=0
if rg -n -i -g '!bin/**' -g '!obj/**' -g '!node_modules/**' "(password|senha)\\s*=\\s*['\"]([[:alnum:]]|[!@#%&*_-]){6,}['\"]" database scripts .github src; then echo 'Security gate: possível senha literal' >&2; fail=1; fi
if rg -n "password_hash\\s*=\\s*['\"][^:@]" database scripts; then echo 'Security gate: hash literal' >&2; fail=1; fi
if rg -n -- '--password([ =]|$)' scripts .github; then echo 'Security gate: senha em argumento de CLI' >&2; fail=1; fi
if rg -n "implementation_status='Partial'.*is_marketable=true|is_marketable=true.*implementation_status='Partial'" database; then echo 'Security gate: feature Partial comercializável' >&2; fail=1; fi
exit "$fail"
