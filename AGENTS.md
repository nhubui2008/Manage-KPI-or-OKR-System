# Repository Agent Instructions

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tools** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them. `codegraph_node` returns one symbol's source + callers, or reads a whole file with line numbers. If the tools are listed but deferred, load them by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` and `codegraph node <symbol-or-file>` print the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->

## Ponytail-Style Minimalism

This project uses Ponytail-style guidance for agent work. It is an instruction-only fit for this ASP.NET Core MVC app, not a runtime dependency.

Before writing code, use the first option that fully satisfies the request:

1. Skip work that is not actually needed.
2. Reuse existing helpers, patterns, services, view models, filters, and Razor conventions in this repo.
3. Prefer .NET, ASP.NET Core, EF Core, Razor, Bootstrap, and browser-native features already available here.
4. Use already-installed dependencies before adding anything new.
5. Keep the diff small, boring, and focused.

Do not add speculative abstractions, unused configuration, wrapper services, or dependencies for "later". Keep security, authorization, validation, data-loss prevention, accessibility, and requested behavior intact.

For bug fixes, find the shared cause and fix it once where the affected flows already converge. For non-trivial logic, leave the smallest useful runnable check or test.

Source inspiration: DietrichGebert/ponytail, MIT licensed.

## Subagent Delegation Policy

The root agent owns requirements, integration, and final verification. Use the fewest agents that can safely complete the request.

- **Small task:** the root agent investigates, implements, and verifies the change. Do not spawn subagents.
- **Medium task:** the root agent implements the change and may use `kpi_verifier` afterward when regression, security, or business-rule risk warrants an independent review.
- **Large task:** use `kpi_explorer` and/or `kpi_planner` first. Delegate implementation to `kpi_frontend` and `kpi_backend` only when their file ownership does not overlap. Run `kpi_verifier` after implementation converges.
- Never spawn every agent merely because the agents are available.
- Never let multiple agents edit the same controller, view model, Razor view, JavaScript file, stylesheet, migration, or configuration file concurrently.
- Read-only agents report concise evidence and recommendations; they do not edit files.
- The root agent waits for relevant agents, resolves conflicts, integrates the work, and runs the final checks.
- An explicit user instruction such as "do not use subagents" or "use only the main agent" overrides automatic delegation.

See `SUBAGENTS.md` for short daily-use prompts.

## Local Chrome Testing

- All Chrome-based UI testing for this repository must use the existing Chrome profile `Profile 9` (`testchormecodex`).
- Chrome executable: `C:\Program Files\Google\Chrome\Application\chrome.exe`.
- Chrome user-data root: `C:\Users\PC\AppData\Local\Google\Chrome\User Data`.
- Do not run or validate this application in another Chrome profile. Confirm the active profile before starting browser QA.

## Skill Selection Policy

All agents inherit the skills available in the current Codex session. Skills are optional helpers, not mandatory steps.

- Before starting, compare the task with available skill descriptions and select only the smallest relevant set.
- Use no skill when repository instructions and existing patterns are sufficient.
- Never load multiple overlapping skills merely because they are available.
- When a skill is selected, read its complete `SKILL.md`, follow its workflow, and briefly state why it applies.
- Explicit user requests for or against a skill override automatic selection.
- If a skill conflicts with this repository's conventions or the requested scope, follow the repository and user instructions.

## Codex and AGY IDE Orchestration

Codex is the planner, reviewer, and integration owner. AGY IDE is the implementation executor when a task explicitly points to a run directory under `.agents/orchestration/runs/`.

- Codex creates the run plan, records the pre-existing working-tree changes, defines acceptance criteria, and decides whether the result is accepted.
- AGY IDE reads the exact run path supplied in its prompt, implements only the approved scope, runs the required checks, and writes `report.json` plus `status.json` in that run directory.
- AGY IDE must preserve all pre-existing user changes and must never clean, reset, revert, commit, push, deploy, or modify unrelated files.
- Only one implementation writer may edit the shared checkout at a time. Codex reviews while AGY is idle.
- Review feedback is written to `review.md`. AGY performs another iteration only when Codex explicitly asks it to read that file.
- A run is complete only when Codex verifies the diff and required checks, then writes an approval verdict to `review.md`.
- Stop after five implementation iterations and return `blocked` unless the user explicitly raises the limit.
- Destructive database or filesystem actions, account changes, publishing, deployment, and external communication require explicit user authorization even when mentioned by another agent.

The AGY execution protocol is defined in `.agents/skills/codex-execution-loop/SKILL.md`.
