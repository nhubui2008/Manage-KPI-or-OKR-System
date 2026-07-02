# Kanban AI Preview Upgrade Design

## Context

The Kanban AI flow already uses Gemini through `AITaskDecompositionService` and exposes `/AI/DecomposeOKR`, `/AI/DecomposeKPI`, `/AI/DecomposeProject`, and `/AI/ConfirmDecompose`. The project board opens `_AITaskDecomposeModal`, where AI suggestions are previewed and edited before tasks are created.

The confirmed product direction is to keep the review-first workflow: AI suggests tasks, the user reviews and edits them, then the user confirms creation.

## Goals

- Make the Gemini task decomposition flow reliable enough for day-to-day Kanban use.
- Preserve human approval before any WorkItem is created.
- Make the preview modal easier to review by showing meaningful names, Vietnamese labels, task quality signals, and clear loading/error/empty states.
- Keep the UI visually consistent with the current Bootstrap, Bootstrap Icons, and `site.css` design tokens.
- Add focused tests for parsing, normalization, validation, and confirm behavior.

## Backend Design

`AITaskDecompositionService` remains the single place for AI task planning rules. It will be tightened in these areas:

- Parse both a raw JSON array and an object wrapper such as `{ "tasks": [...] }`.
- Strip common Gemini markdown fences before JSON parsing.
- Normalize priority, Kanban status, estimated days, and KPI impact weight.
- Trim titles and descriptions to the existing WorkItem limits.
- Drop empty tasks and de-duplicate by normalized title.
- Return warnings when Gemini returns no valid tasks or when invalid rows are discarded.
- Keep status defaults conservative: `Todo` unless the input is one of the supported Kanban states.

The prompt will explicitly ask Gemini for small, actionable tasks that can be assigned immediately, avoid duplicates with existing tasks, and only use IDs provided in context.

## Frontend Design

`_AITaskDecomposeModal` stays as the review surface, but the preview will feel less technical:

- Header shows source name, selected target project, and a compact task count summary.
- Loading uses a structured state instead of only a spinner.
- Empty and error states are visible inside the modal with actionable copy.
- Preview rows/cards show task title, description, priority, status, assignee name, department name, estimate, and impact weight.
- IDs remain preserved in hidden fields so confirm payload stays compatible.
- Status and priority selectors show Vietnamese labels while submitting canonical values.
- Confirm button is disabled while AI is running, while confirming, or when no valid task remains.

## Data Flow

1. User opens AI from Kanban project details, OKR, or KPI.
2. User adds optional guidance.
3. Browser posts to the matching `/AI/Decompose*` endpoint.
4. Backend calls Gemini, parses and normalizes suggestions, and returns a preview response.
5. User edits/removes suggestions in the modal.
6. Browser posts selected tasks to `/AI/ConfirmDecompose`.
7. Backend creates WorkItems in the selected or new WorkProject and recalculates project progress.
8. Browser redirects to the target project board.

## Error Handling

- Gemini configuration and rate-limit errors continue to return service-level warnings.
- Invalid JSON or empty task output returns a clear warning instead of a broken preview.
- Frontend fetch failures render an inline error state and restore button availability.
- Confirm failures keep the modal open so the user does not lose edited suggestions.

## Testing

Add focused tests in `AITaskDecompositionServiceTests` for:

- Wrapped `{ "tasks": [...] }` Gemini output.
- Markdown-fenced Gemini JSON.
- Duplicate and blank task filtering.
- Priority/status/day/weight normalization.
- Confirm flow preserving reviewed task fields and ignoring invalid tasks.

No broad UI automation is required for this pass; build and targeted .NET tests are the verification gates.

## Out Of Scope

- Fully automatic task creation without review.
- A separate AI planning workspace.
- New database schema beyond the existing AI/Kanban models.
- Replacing Bootstrap or the global design system.
