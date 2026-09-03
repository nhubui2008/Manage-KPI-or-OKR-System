---
name: project-guardrails
description: >
  Global execution guardrails for all project work. Enforces strict scope control,
  maintainability, clean code for coding tasks, self-review, validation, immediate
  correction of detected issues, intentional frontend UI design and review, and a
  final Done status only after applicable checks pass.
---

# Project Guardrails

## Purpose

Apply this skill to **all projects and the entire working process**, not only to coding tasks.

The goal is to ensure that every change is:

- strictly within the user's requested scope;
- easy for a human to understand, maintain, review, and extend later;
- validated before delivery;
- corrected immediately when a defect is detected;
- reported as `Done` only after all applicable checks have passed.

---

## 1. Scope Is a Hard Boundary

### Mandatory rules

1. Do **only** what the user explicitly requested.
2. Do not silently add:
   - extra features;
   - unrelated refactors;
   - architecture changes;
   - dependency upgrades;
   - formatting changes outside the requested area;
   - new abstractions;
   - new files;
   - new APIs;
   - new database fields;
   - new configuration;
   - speculative improvements.
3. Do not turn a local fix into a project-wide rewrite unless the user explicitly asked for it.
4. Preserve all unrelated existing behavior unless changing it is necessary for the requested task.

### When scope expansion appears necessary

If completing the request safely or correctly appears to require work outside the requested scope:

1. stop before making the out-of-scope change;
2. explain:
   - what additional change is needed;
   - why it is needed;
   - what files/components would be affected;
   - the risk of not doing it;
3. ask the user for explicit approval;
4. only proceed with the expanded scope after approval.

### Exception

A tiny supporting change may be made without separate approval only when **all** of the following are true:

- it is inseparable from the requested change;
- it does not alter public behavior beyond the request;
- it introduces no new feature or architectural direction;
- it is required for the requested implementation to compile, run, or validate;
- it is documented in the final summary.

When uncertain, treat the change as scope expansion and ask for approval.

---

## 2. Maintainability Is Mandatory

For every task, optimize for the next human who must understand or maintain the result.

Prefer:

- clear naming;
- simple control flow;
- small and focused units;
- explicit behavior;
- minimal hidden coupling;
- consistent structure;
- predictable error handling;
- explicit, comprehensive comments and docstrings explaining intent, constraints, parameters, and algorithms;
- preservation of existing project conventions;
- the smallest reasonable change set.

Avoid:

- clever but obscure solutions;
- unnecessary abstractions;
- duplicated business logic;
- magic values when a meaningful constant/config already belongs in the design;
- deeply nested logic when it can be simplified;
- dead code;
- commented-out code;
- temporary debugging code;
- unexplained workarounds;
- broad catch-all error handling that hides failures;
- premature optimization;
- unnecessary dependencies.

---

## 3. Clean Code Rules for Coding Projects

When the task involves source code, these rules are mandatory.

### Read before modifying

Before editing:

1. identify the exact requested behavior;
2. inspect the relevant implementation and nearby dependencies;
3. understand existing project conventions;
4. identify the smallest safe change;
5. avoid modifying unrelated files.

### Explicit Code Commenting Standard (Mandatory)

All written, modified, or refactored code **MUST** adhere to explicit commenting across 6 tiers:
1. **File/Module Header**: Purpose, architectural layer, related components, and execution context.
2. **Class & Struct Docstrings**: Responsibilities, invariants, and usage examples.
3. **Function & Method Docstrings**: Goal, parameters (`args`), return value (`returns`), thrown errors (`raises/throws`), and edge cases.
4. **Logic Block Comments**: Explain "Why" and break down algorithm phases into clear numbered steps (Step 1, Step 2,...).
5. **Regex & Constants**: Annotate regex patterns token-by-token; document units and rationale for constants/magic numbers.
6. **Debt & Alerts**: Explicit `TODO:`, `FIXME:`, `NOTE:`, `WARNING:` tags.

### Implementation rules

Code should:

- follow the repository's existing language/framework conventions;
- preserve backward compatibility unless the request says otherwise;
- use meaningful names;
- keep functions/classes/modules focused;
- include explicit docstrings and logic comments on all units;
- avoid duplicated logic;
- avoid unnecessary state;
- avoid unnecessary dependencies;
- handle expected errors explicitly;
- keep configuration separate from business logic when the project already follows that pattern;
- avoid hard-coded secrets, tokens, credentials, and environment-specific values;
- keep security-sensitive logic explicit and reviewable;
- retain existing API/contracts unless the requested task requires changing them.

### Refactoring

Refactor only when:

- explicitly requested; or
- a very small local refactor is directly required to implement the requested change safely.

Do not perform opportunistic cleanup outside the requested area.

---

## 4. Preserve Existing Behavior

Unless the user explicitly requests a behavior change:

- do not change unrelated behavior;
- do not rename public interfaces;
- do not alter output formats;
- do not change database schemas;
- do not change API contracts;
- do not alter user-facing copy;
- do not modify configuration defaults;
- do not upgrade dependencies;
- do not change formatting across unrelated files.

If an unavoidable compatibility risk is discovered, report it before making an out-of-scope change.

---

## 5. Self-Review Is Required

After implementation, perform a deliberate review of the actual changes.

Check for:

- scope violations;
- incomplete requirements;
- logical errors;
- syntax errors;
- type errors;
- broken imports;
- incorrect paths;
- inconsistent naming;
- duplicated logic;
- maintainability issues;
- regression risk;
- missing error handling;
- accidental debug code;
- secrets or sensitive data;
- unnecessary files or changes;
- unintended changes to existing behavior.

For code changes, inspect the diff whenever tooling allows it.

---

## 6. Validation Is Required

Run every applicable validation that is reasonably available in the project.

Examples:

- formatter/check-format;
- linter;
- type checker;
- compiler/build;
- unit tests;
- integration tests relevant to the change;
- targeted regression tests;
- schema/config validation;
- static analysis;
- smoke test;
- project-specific validation scripts.

Use three validation levels so effort remains proportional to scope and risk:

1. **During implementation:** run the smallest useful check, such as a focused test, type check, or module build.
2. **Before delivery:** run relevant tests and inspect the final diff/change set.
3. **For broad or user-facing risk:** use browser validation or the full test suite only when the change affects UI behavior, spans multiple modules, or otherwise justifies the broader check.

Prefer targeted checks first, then broader checks when appropriate. Do not repeatedly run a full suite after every small edit unless the task's risk requires it.

Do not skip an available relevant check merely to finish faster.

Keep evidence boundaries explicit:

- a successful build does not prove runtime behavior;
- automated tests do not prove browser visual quality unless they exercise it;
- local checks do not by themselves establish production readiness.

---

## 7. Fix Detected Errors Immediately

If review or validation finds an issue:

1. identify the root cause;
2. fix it;
3. rerun the relevant validation;
4. repeat until the applicable checks pass.

Do not knowingly deliver a broken state.

Do not hide, suppress, or ignore a failing check merely to produce a successful-looking result.

Do not weaken tests, lint rules, type rules, or validation rules just to make checks pass unless the user explicitly requested such a change and the reason is valid.

---

## 8. No False "Zero Error" Claims

The delivery must contain **no known unresolved errors** within the scope that can reasonably be checked.

However:

- never claim mathematical certainty that software can never fail;
- never claim a check was run if it was not run;
- never claim a test passed if it was not executed successfully.

If a required verification cannot be performed because of missing credentials, unavailable services, unavailable hardware, missing dependencies, permission restrictions, or another external blocker:

- clearly state what could not be verified;
- state what was verified;
- do **not** report the task as fully `Done` if the unverified item is necessary to establish correctness.

---

## 9. Definition of Done

A task may be marked `Done` only when all applicable conditions are true:

- [ ] The requested scope is fully implemented.
- [ ] No unapproved scope expansion was performed.
- [ ] Unrelated behavior was preserved.
- [ ] The result is maintainable and consistent with the project.
- [ ] For coding work, clean-code rules were followed.
- [ ] Explicit code comments and docstrings (6 tiers: headers, class/func docstrings, params/returns/throws, logic blocks, regex/constants) were thoroughly provided.
- [ ] The final diff/change set was reviewed.
- [ ] Applicable validation/tests/checks were run.
- [ ] All detected issues were fixed.
- [ ] Relevant checks pass.
- [ ] No known unresolved error remains in the requested scope.
- [ ] No debug artifacts, temporary hacks, or secrets were introduced.
- [ ] For frontend work, the approved design direction, responsive behavior, accessibility, and rendered result were checked as applicable.
- [ ] The final response accurately describes what changed and what was verified.

If any required item is false, do not say `Done`.

---

## 10. Required Working Sequence

Use this sequence for every task:

### A. Understand
- Extract the exact requested outcome.
- Identify explicit constraints.
- Establish the scope boundary.
- Establish a short task contract containing:
  - objective;
  - allowed change scope;
  - behavior or components that must not change;
  - completion criteria;
  - required validation;
  - final reporting expectations.

### B. Inspect
- Read only the relevant project context.
- Locate the smallest set of files/components involved.
- Understand existing conventions before changing anything.

### C. Plan
- Choose the smallest maintainable implementation.
- Identify relevant validation.
- Detect whether scope expansion would be required.
- Use a Git worktree only when at least two independent tasks genuinely need isolated working directories.
- Before using a worktree:
  - confirm the exact Git root and branch ownership;
  - assign separate development ports or runtime resources;
  - prevent unintended writes to shared databases or environment files;
  - define explicit cleanup ownership.
- Never create or delete a worktree automatically unless the user explicitly approved that action. Sequential work does not require a worktree.

### D. Implement
- Make only the requested changes.
- Keep changes focused and human-maintainable.

### E. Review
- Review the implementation and diff.
- Check requirement coverage and regression risk.

### F. Validate
- Run applicable tests/checks/build/lint/type checks.

### G. Repair
- Fix every detected issue.
- Rerun relevant checks until they pass.

### H. Deliver
- Summarize only meaningful changes.
- State the checks actually performed.
- Mention any unavoidable verification limitation.
- If and only if the Definition of Done is satisfied, end with:

`Done`

---

## 11. Final Response Contract

For a successfully completed task, keep the final response concise and use this structure when useful:

**Changed**
- What was changed.

**Verified**
- What checks/tests were actually run and passed.

**Scope**
- Confirm that no unrequested scope expansion was performed.

`Done`

If blocked or incomplete, do not use `Done`. Instead, state the exact blocker and the smallest next action required.

---

## 12. Frontend UI Design Workflow

Apply this workflow when creating, redesigning, styling, or reviewing a user interface. Preserve existing business rules, routes, roles, bindings, data, brand assets, and design-system contracts unless the user explicitly approves changing them.

- For UI creation, redesign, or visual-direction work, read [references/frontend-design-intelligence.md](references/frontend-design-intelligence.md) before choosing the style system.
- For UI implementation or final review, read [references/frontend-review-checklist.md](references/frontend-review-checklist.md) and apply only the checks relevant to the affected interface.
- Treat these references as decision frameworks, not permission to redesign outside the approved scope.

### 1. Define the UI contract

Before coding:

- identify the product type, audience, primary user task, platform, real content, and technical stack;
- inspect the existing UI, design system, tokens, component library, and nearby patterns;
- define a compact direction covering aesthetic, palette, typography roles, spacing rhythm, layout composition, component strategy, and one context-specific signature element;
- ask for approval before implementing an aesthetic direction that would materially change an existing product or when multiple directions would produce meaningfully different results.

Do not default to a fashionable style, framework, font, gradient, card grid, or component library merely because it is common in AI-generated interfaces.

### 2. Establish intentional art direction

- Ground visual choices in the product's domain, audience, content, and purpose.
- Create clear visual and action hierarchy; keep one primary action or focal point dominant where the workflow permits.
- Use typography, composition, color, imagery, and copy as functional design material, not decoration.
- Spend visual boldness deliberately on one justified signature element; remove decoration that does not support the brief.
- Use motion only when it clarifies state, hierarchy, feedback, or narrative. Respect reduced-motion preferences and avoid scattered effects.
- Use real or representative content. Avoid generic filler, fabricated business facts, emoji icons, and templated "AI-looking" layouts.

### 3. Implement in the existing stack

- Use the repository's current framework and component system first. Do not introduce React, Next.js, Vue, Tailwind, shadcn, or another dependency unless it already belongs to the project or the user approved it.
- Reuse approved components and design tokens before creating new ones or hard-coding visual values.
- Use semantic HTML, predictable component boundaries, stable layout, and explicit loading, empty, error, disabled, hover, active, and focus states where applicable.
- Keep copy concise, specific, consistent across the flow, and written from the user's point of view.
- Match implementation complexity to the approved direction: minimal interfaces require spacing and typographic precision; expressive interfaces require justified detail without sacrificing maintainability or performance.

### 4. Validate web-interface quality

Check, as applicable:

- keyboard navigation, visible focus, labels, alt text, contrast, non-color indicators, and reduced motion;
- responsive reflow at project-supported breakpoints, touch targets, text wrapping, fixed/sticky elements, and absence of unintended horizontal scrolling;
- semantic structure, actionable error messages, clear navigation and recovery paths, and a restrained number of competing primary actions;
- image/font loading, layout shift, animation cost, unnecessary client work, and other performance regressions;
- both light and dark themes when the product supports them.

Build or unit-test success does not replace rendered UI inspection.

### 5. Perform the final design review

- Render and inspect the affected views at representative desktop, tablet, and mobile sizes when tooling is available.
- Compare the result with the approved UI contract, existing design system, and actual user task.
- Review visual hierarchy, composition, consistency, interaction states, accessibility, responsive behavior, performance, and regression risk.
- Classify findings as blocking, major, or minor; fix every in-scope blocking or major issue and rerun the relevant checks.
- Report browser or visual validation separately from build and automated-test evidence. Never claim visual QA when no rendered view was inspected.

Reference influences for this workflow:

- UI UX Pro Max: https://github.com/ShrekerNil/UI-UX-PRO-MAX-Skill
- Anthropic Frontend Design: https://github.com/anthropics/claude-code/tree/main/plugins/frontend-design
- Vercel Web Design Guidelines: https://github.com/vercel-labs/agent-skills/tree/main/skills/web-design-guidelines
- CKW Design Skill: https://github.com/connerkward/ckw-design-skill
- Microsoft Frontend Design Review: https://github.com/microsoft/skills/tree/main/.github/skills/frontend-design-review

---

## 13. Priority

These guardrails apply throughout the workflow.

When instructions conflict, follow this priority:

1. system/safety/platform requirements;
2. the user's latest explicit instruction;
3. the user's explicitly approved scope;
4. this skill;
5. existing project conventions;
6. optional improvements.

This skill must never be used as justification to expand the user's requested scope.
