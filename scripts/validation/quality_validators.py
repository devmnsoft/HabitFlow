#!/usr/bin/env python3
"""Static, dependency-free safeguards for Razor, C# SQL and repository assets."""

from __future__ import annotations

import argparse
import os
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REPORTS = ROOT / "artifacts" / "validation"


def files(pattern: str) -> list[Path]:
    return [p for p in ROOT.glob(pattern) if not ({"bin", "obj", "node_modules"} & set(p.parts))]


def write_report(name: str, title: str, findings: list[str], errors: list[str]) -> None:
    REPORTS.mkdir(parents=True, exist_ok=True)
    status = "FAIL" if errors else "PASS"
    body = [f"HabitFlow local quality gate - {title}", f"STATUS: {status}", ""]
    body += findings or ["No findings."]
    if errors:
        body += ["", "Blocking errors:", *errors]
    (REPORTS / name).write_text("\n".join(body) + "\n", encoding="utf-8")


def razor() -> int:
    findings: list[str] = []
    errors: list[str] = []
    targets = files("src/HabitFlow.Web/Views/**/*.cshtml") + files("src/HabitFlow.Web/Pages/**/*.cshtml")
    at_rule = re.compile(r"(?<!@)@(media|supports|keyframes|container|font-face|page)\b", re.I)
    for path in targets:
        text = path.read_text(encoding="utf-8")
        relative = path.relative_to(ROOT)
        for match in at_rule.finditer(text):
            line = text.count("\n", 0, match.start()) + 1
            errors.append(f"{relative}:{line}: CSS @{match.group(1)} must be escaped as @@{match.group(1)} in Razor.")
        for match in re.finditer(r"(?m)^[ \t]+@section\b", text):
            line = text.count("\n", 0, match.start()) + 1
            errors.append(f"{relative}:{line}: @section must start at column 1 and remain top-level.")
        for match in re.finditer(r"<style\b[^>]*>(.*?)</style\s*>", text, re.I | re.S):
            css_lines = match.group(1).count("\n") + 1
            if css_lines > 80 or len(match.group(1)) > 5000:
                line = text.count("\n", 0, match.start()) + 1
                errors.append(f"{relative}:{line}: inline <style> is too large ({css_lines} lines); move it to wwwroot/css.")
        # This intentionally checks structural HTML only; Razor control blocks do not affect tag nesting.
        scrubbed = re.sub(r"@\*.*?\*@|<!--.*?-->|<(script|style)\b.*?</\1\s*>", "", text, flags=re.I | re.S)
        stack: list[tuple[str, int]] = []
        for match in re.finditer(r"</?(div|section)\b[^>]*>", scrubbed, re.I):
            tag = match.group(1).lower()
            line = scrubbed.count("\n", 0, match.start()) + 1
            if match.group().startswith("</"):
                if not stack or stack[-1][0] != tag:
                    errors.append(f"{relative}:{line}: suspicious closing </{tag}> (expected {stack[-1][0] if stack else 'no tag'}).")
                else:
                    stack.pop()
            else:
                stack.append((tag, line))
        errors += [f"{relative}:{line}: unclosed <{tag}>." for tag, line in stack]
    findings.append(f"Scanned {len(targets)} Razor views/partials/layouts.")
    write_report("razor-css-report.txt", "Razor/CSS", findings, errors)
    return bool(errors)


def mask_csharp(text: str) -> tuple[str, list[tuple[int, str, str]]]:
    """Mask comments/string contents and return (line, prefix, content) for strings."""
    masked = list(text)
    strings: list[tuple[int, str, str]] = []
    i = 0
    while i < len(text):
        if text.startswith("//", i):
            end = text.find("\n", i); end = len(text) if end < 0 else end
            masked[i:end] = " " * (end - i); i = end; continue
        if text.startswith("/*", i):
            end = text.find("*/", i + 2); end = len(text) - 2 if end < 0 else end
            end += 2; masked[i:end] = [("\n" if c == "\n" else " ") for c in text[i:end]]; i = end; continue
        match = re.match(r'(\$?@|@\$|\$)?(""")|(\$?@|@\$|\$)?"', text[i:])
        if match:
            prefix = match.group(1) or match.group(3) or ""
            delimiter = '"""' if match.group(2) else '"'
            start, line = i, text.count("\n", 0, i) + 1
            i += len(prefix) + len(delimiter)
            content_start = i
            while i < len(text):
                if delimiter == '"""' and text.startswith(delimiter, i): break
                if delimiter == '"' and text[i] == '"':
                    if "@" in prefix and i + 1 < len(text) and text[i + 1] == '"': i += 2; continue
                    if "@" not in prefix and i and text[i - 1] == "\\": i += 1; continue
                    break
                i += 1
            content = text[content_start:i]
            i = min(len(text), i + len(delimiter))
            masked[start:i] = [("\n" if c == "\n" else " ") for c in text[start:i]]
            strings.append((line, prefix + delimiter, content))
            continue
        i += 1
    return "".join(masked), strings


def sql() -> int:
    findings: list[str] = []
    errors: list[str] = []
    targets = files("src/**/*.cs") + files("tests/**/*.cs")
    leading = re.compile(r"(?im)^\s*(select|from|where|join|case\s+when|order\s+by|group\s+by)\b")
    sql_words = re.compile(r"\b(select|insert|update|delete|from|where|join|values|returning)\b", re.I)
    changed = set(changed_files())
    for path in targets:
        text = path.read_text(encoding="utf-8")
        relative = path.relative_to(ROOT)
        masked, literals = mask_csharp(text)
        for match in leading.finditer(masked):
            line = masked.count("\n", 0, match.start()) + 1
            errors.append(f"{relative}:{line}: suspected SQL outside a C# string ({match.group(1)}).")
        for line, prefix, content in literals:
            keywords = sql_words.findall(content)
            if len(keywords) >= 4 and '"""' not in prefix and "@" not in prefix:
                message = f"{relative}:{line}: long SQL must use a raw or verbatim string literal."
                (errors if str(relative) in changed else findings).append(("" if str(relative) in changed else "BASELINE REVIEW: ") + message)
            if len(keywords) >= 2 and "$" in prefix and re.search(r"\{[^{}]+\}", content):
                message = f"{relative}:{line}: interpolated SQL is forbidden; use Dapper parameters."
                (errors if str(relative) in changed else findings).append(("" if str(relative) in changed else "BASELINE REVIEW: ") + message)
    findings.append(f"Scanned {len(targets)} C# files; SQL interpolation and literal style checked.")
    write_report("sql-string-report.txt", "C# SQL strings", findings, errors)
    return bool(errors)


def changed_files() -> list[str]:
    base = os.environ.get("QUALITY_GATE_BASE")
    commands = [
        ["git", "diff", "--name-only", "--cached"],
        ["git", "diff", "--name-only"],
        ["git", "ls-files", "--others", "--exclude-standard"],
    ]
    if base:
        commands.append(["git", "diff", "--name-only", f"{base}...HEAD"])
    names: set[str] = set()
    for command in commands:
        result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, check=False)
        if result.returncode == 0:
            names.update(result.stdout.splitlines())
    return sorted(names)


def forbidden() -> int:
    changed = changed_files()
    errors: list[str] = []
    forbidden_path = re.compile(r"(^|/)(bin|obj|\.vs|node_modules|test-results|playwright-report)(/|$)|\.g\.cs$|\.(log|tmp|user)$", re.I)
    image = re.compile(r"\.(png|jpe?g|webp|gif)$", re.I)
    secret = re.compile(r"(password|pwd)\s*=\s*[^;\s${}]+|User Id=[^;]+;.*Password=", re.I)
    for name in changed:
        if forbidden_path.search(name): errors.append(f"{name}: generated/temporary file is forbidden.")
        if image.search(name) and not name.startswith("artifacts/"): errors.append(f"{name}: screenshots must be stored under artifacts/.")
        path = ROOT / name
        if path.is_file() and path.stat().st_size < 2_000_000 and not image.search(name):
            try: content = path.read_text(encoding="utf-8")
            except UnicodeDecodeError: continue
            scan_content = "\n".join(line for line in content.splitlines() if "secret = re.compile" not in line)
            if secret.search(scan_content) and not name.endswith((".example", ".example.json")):
                errors.append(f"{name}: possible real connection string/secret in changed content.")
    findings = [f"Diff scope contains {len(changed)} file(s).", *(f"AUDITED: {name}" for name in changed)]
    write_report("forbidden-files-report.txt", "forbidden files", findings, errors)
    return bool(errors)


def css() -> int:
    findings: list[str] = []
    errors: list[str] = []
    selectors = re.compile(r"(?m)^\s*([^@/][^{]+)\{")
    dangerous = re.compile(r"(^|[>+~\s,])(?:\.card|\.btn|\.dropdown-menu|\.modal|\.offcanvas|dialog|section|main|\.container|\.hf-habit-card)(?=[\s>+~,:.#\[]|$)")
    global_files = {"design-system-v2.css", "app-shell-premium.css", "tokens.css", "public.css"}
    changed = set(changed_files())
    for path in files("src/HabitFlow.Web/wwwroot/css/**/*.css"):
        text = path.read_text(encoding="utf-8")
        relative = path.relative_to(ROOT)
        count = len(re.findall(r"!important\b", text, re.I))
        if count: findings.append(f"REVIEW: {relative}: {count} use(s) of !important.")
        for match in selectors.finditer(text):
            selector = " ".join(match.group(1).split())
            if dangerous.search(selector):
                line = text.count("\n", 0, match.start()) + 1
                findings.append(f"REVIEW: {relative}:{line}: global selector `{selector[:160]}`.")
                if str(relative) in changed and path.name not in global_files and selector.strip().split()[0] in {".card", ".btn", ".dropdown-menu", ".modal", ".offcanvas", "dialog", "section", "main", ".container", ".hf-habit-card"}:
                    errors.append(f"{relative}:{line}: page CSS must scope dangerous selector under a page root class.")
    write_report("css-global-report.txt", "global CSS", findings, errors)
    return bool(errors)


VALIDATORS = {"razor": razor, "sql": sql, "forbidden": forbidden, "css": css}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("validator", choices=[*VALIDATORS, "all"])
    args = parser.parse_args()
    selected = VALIDATORS.values() if args.validator == "all" else [VALIDATORS[args.validator]]
    results = [run() for run in selected]
    failed = any(results)
    return int(failed)


if __name__ == "__main__":
    raise SystemExit(main())
