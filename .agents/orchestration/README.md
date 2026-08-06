# Codex ↔ AGY IDE orchestration

This directory is the durable contract for using Codex as planner/reviewer and AGY IDE as the implementation executor.

## One task

1. Codex creates `.agents/orchestration/runs/<task-id>/` from the templates in this directory.
2. Codex records the current `git status --short`, the approved scope, acceptance criteria, and verification commands in `plan.md`.
3. In the existing AGY IDE window, select the `codex-executor` primary agent and submit:

   ```text
   /goal Execute the approved Codex plan at .agents/orchestration/runs/<task-id>/plan.md. Follow the codex-execution-loop skill and stop after writing report.json and status.json.
   ```

4. Codex waits for `status.json` to become `needs_codex_review`, then reviews the actual diff and reruns relevant checks.
5. Codex writes `review.md` with `approved`, `changes_requested`, or `blocked`.
6. For corrections, Codex increments the iteration and asks the same AGY IDE conversation to read `review.md` and continue.

AGY never accepts its own work. The maximum is five implementation iterations.

## Runtime files

Runtime task directories are intentionally ignored by Git. Keep evidence locally during the task; copy only intentionally useful artifacts into tracked documentation.
