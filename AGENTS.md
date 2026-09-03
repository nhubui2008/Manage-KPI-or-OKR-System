# Repository Agent Instructions

<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tools** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them. `codegraph_node` returns one symbol's source + callers, or reads a whole file with line numbers. If the tools are listed but deferred, load them by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` and `codegraph node <symbol-or-file>` print the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->

## Project Guardrails

All agent work in this repository must follow the global project guardrails defined in [`.agents/project-guardrails.md`](.agents/project-guardrails.md). These guardrails enforce:

1. **Scope as a hard boundary** — do only what was requested; ask before expanding.
2. **Maintainability** — clear naming, simple flow, small focused units, explicit comments.
3. **Clean code rules** — read before modifying; follow the 6-tier commenting standard; follow repo conventions.
4. **Preserve existing behavior** — do not change unrelated interfaces, schemas, or formatting.
5. **Self-review** — deliberate review of actual changes before delivery.
6. **Validation** — run all applicable checks proportional to scope and risk.
7. **Fix errors immediately** — do not deliver a known broken state.
8. **No false claims** — never claim a check was run if it was not.
9. **Definition of Done** — all 14 checklist items must be satisfied before marking `Done`.
10. **Required working sequence** — Understand → Inspect → Plan → Implement → Review → Validate → Repair → Deliver.
11. **Frontend UI design workflow** — use [`.agents/references/frontend-design-intelligence.md`](.agents/references/frontend-design-intelligence.md) for style direction and [`.agents/references/frontend-review-checklist.md`](.agents/references/frontend-review-checklist.md) for implementation review.

When guardrails conflict with other instructions, follow the priority order in section 13 of the guardrails document.

Source: `/run/media/cua/DATA1/proj/skills/SKILL.md` (project-guardrails skill).

## Vibe-Coding Default

The user works outcome-first and normally sends short Vietnamese prompts. When a prompt clearly asks to build, change, or fix something, own the normal local workflow from discovery through implementation and verification.

- Turn the requested outcome into concrete acceptance criteria from the relevant parts of `PRODUCT.md`, `README.md`, the current behavior, and existing repository patterns.
- Do not require the user to name files, choose an architecture, describe implementation details, or provide build and test commands.
- Resolve low-risk ambiguity by inspecting the product and choosing the smallest reasonable assumption. Continue working and mention only material assumptions in the final report.
- Ask a question only when the answer would materially change product behavior, requires a secret or external decision, or authorizes a destructive or irreversible action.
- For build/change/fix requests, trace the real flow, implement the complete smallest vertical slice, run relevant checks, fix failures caused by the change, and review the final diff. Do not stop at a plan, tutorial, patch suggestion, TODO, or partial implementation.
- For diagnose, explain, audit, review, or status requests, remain read-only unless the prompt also asks for a fix.
- Never ask the user to read code. Communicate in concise, beginner-friendly Vietnamese and normally report only the outcome, verification, and an important caveat if one remains.
- Full local tool access removes execution friction; it does not authorize deployment, publishing, pushing, purchases, account changes, secret rotation, messages to third parties, or deletion/reset of material data unless the user explicitly requests that action.

For vague but actionable UI prompts such as "làm trang này đẹp và dễ dùng hơn", preserve the existing business behavior, validation, authorization, and data contracts; improve the responsive and accessible experience; then verify the actual page in the required Chrome profile when the app can run locally.

## Default Local Workflow

- Use CodeGraph first when its index is usable. If the checked-in `.codegraph/` directory is unavailable or the command reports that no index exists, continue immediately with `rg`, targeted file reads, and normal repository tools; never initialize or rebuild the index without a user request.
- Use the solution as the default verification boundary: `dotnet build Manage-KPI-or-OKR-System.sln`.
- After a successful solution build, run `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`; use a focused filter first only when it gives sufficient coverage, then expand when risk warrants it.
- Run the app with `dotnet run --project Manage-KPI-or-OKR-System.csproj --launch-profile https` when browser or runtime verification is relevant.
- Do not reset, reseed, or destructively migrate a real database as part of routine verification. Schema or seed changes must be directly required by the requested feature and handled with data-loss prevention in mind.
- Before finishing, preserve unrelated user changes, inspect the diff, and confirm that no temporary debug code, generated junk, credentials, or unrelated formatting changes remain.

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

### Shared Skills Directory

The global skills library lives at `/run/media/cua/DATA1/proj/skills` (762 skills, 11 domains). Use it when a task requires domain-specific guidance beyond the project-level `.agents/` skills.

- **Discovery:** run `skill find-for "<task description>"` or `skill search "<keyword>"` to locate relevant skills.
- **Reading:** read the recommended `SKILL.md` with `view_file` before applying its workflow.
- **Project guardrails** (`.agents/project-guardrails.md`) always apply and take precedence over domain skills.
- **Project skill** (`.agents/skill.md`) contains repo-specific conventions and also takes precedence over domain skills.
- Domain skills supplement but never override project-level instructions.
