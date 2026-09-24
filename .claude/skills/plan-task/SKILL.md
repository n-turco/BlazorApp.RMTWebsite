---
name: plan-task
description: Turn a task from RMT_Website_Plan.docx (e.g. "2.2") or a feature idea into a written plan in docs/plans/ before any code is changed. Use when starting a non-trivial feature or when the user says "plan task X".
---

# Plan a task

1. Identify the task. If the user gave a plan number (e.g. `1.1`), find that task in
   `RMT_Website_Plan.docx` (read it with a docx reader or ask the user to paste it).
2. Read the files the task touches and note anything in the code that changes the plan.
3. Copy `docs/plans/_template.md` to `docs/plans/<task-number>-<short-slug>.md` and fill it in:
   goal, current state, approach, step list, files, test/verification plan, open questions.
4. Suggest a branch name: `feature/<task-number>-<slug>` or `fix/<task-number>-<slug>`.
5. Stop and ask the user to review the plan. Do not start implementing until they agree.
