---
name: pre-commit-check
description: Verify the working tree is safe to commit - builds with zero warnings, tests pass, formatting is clean, and no secrets or API keys are in the diff. Use before committing or opening a PR.
---

# Pre-commit check

Run these from the repository root and report each result as pass/fail with the relevant output.

1. **Build:** `dotnet build -nologo -v q` — must report `0 Warning(s)` and `0 Error(s)`.
2. **Tests:** `dotnet test --nologo` — skip with a note if no test project exists yet.
3. **Formatting:** `dotnet format --verify-no-changes` — list any files that need formatting.
4. **Secrets scan** on staged and unstaged changes:
   ```bash
   git diff HEAD | grep -nEi "api[_-]?key|secret|password|token|[0-9a-f]{32}-[0-9a-f]{8}-[0-9a-f]{8}|key-[0-9a-f]{20,}"
   ```
   Also confirm no `secrets.json`, `.env`, `*.pfx`, or `*.pem` files are staged (`git status --short`).
   Any real-looking credential is a **blocker** — do not commit; tell the user.
5. **Markup sanity** for changed `.razor` files: no inline `style=""`, no `<br />` in running text,
   exactly one `<h1>`, and a `<PageTitle>` on routable pages.

Finish with a one-line verdict: "Ready to commit" or "Not ready" plus the blocking items.
Do not commit automatically.
