---
name: codex-execution-loop
description: Executes a Codex-authored implementation plan in AGY IDE, preserves existing changes, runs required verification, and writes a structured handoff report for iterative Codex review.
---

# Codex Execution Loop

Use this skill only when the prompt supplies an exact directory under `.agents/orchestration/runs/`.

## Files in a run

- `plan.md`: immutable requirements, scope, acceptance criteria, and verification commands owned by Codex.
- `review.md`: the latest Codex verdict and requested corrections.
- `status.json`: small machine-readable lifecycle marker owned by AGY while executing.
- `report.json`: structured implementation evidence owned by AGY.

Do not modify `plan.md` or Codex's review text.

## Phase 1: Preflight

1. Read the complete `AGENTS.md` and `plan.md`.
2. Confirm the task ID and iteration are present.
3. Run `git status --short` and compare it with the plan's pre-existing changes.
4. If another active run says `running`, or the working tree contains unexplained overlapping edits, set the current run to `blocked` and stop.
5. If `review.md` exists, address only findings whose verdict is `changes_requested`.

Write `status.json`:

```json
{
  "schemaVersion": 1,
  "taskId": "task-id",
  "iteration": 1,
  "state": "running",
  "updatedAtUtc": "ISO-8601 timestamp",
  "summary": "Short current action"
}
```

## Phase 2: Implementation

- Follow the plan in order unless repository evidence requires a smaller equivalent change.
- Reuse existing services, helpers, view models, Razor conventions, Bootstrap patterns, and tests.
- Keep authorization, validation, antiforgery, data isolation, accessibility, and error handling intact.
- Preserve pre-existing changes. Never reset or replace a whole file merely to make editing easier.
- Do not add dependencies or migrations unless the plan explicitly requires them.
- Record any necessary deviation in `report.json`; do not silently change scope.

## Phase 3: Verification

Run the exact commands listed in the plan. For this repository, the normal baseline is:

```powershell
dotnet build .\Manage-KPI-or-OKR-System.csproj --nologo
dotnet test .\tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj --nologo
```

Use more focused checks when the plan specifies them. For Chrome UI QA, obey the repository rule requiring the existing `Profile 9` (`testchromecodex`) and never create another Chrome profile or window.

## Phase 4: Handoff

Write `report.json` using `.agents/orchestration/report.schema.json`. Every acceptance criterion must have concrete evidence. Use `not_run` rather than inventing evidence.

Then update `status.json`:

- `needs_codex_review`: implementation finished and report is ready.
- `blocked`: a decision, permission, dependency, or non-owned failure prevents progress.
- `failed`: implementation or verification failed and no safe in-scope recovery remains.

Stop after the handoff. Codex independently reviews the diff and may update `review.md` for another iteration.

## Iteration guard

The maximum is five AGY implementation iterations per task. At iteration five, unresolved work must return `blocked` with a concise explanation instead of looping again.
