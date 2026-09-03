# Frontend Review Checklist

Use this reference after implementation. Review only the affected surface, but examine every relevant state and breakpoint. Report findings as `file:line - issue - recommended correction` when reviewing code, and distinguish code evidence from rendered visual evidence.

## 1. Product and task

- The interface serves the approved audience and primary task.
- The primary action is clear; secondary actions do not compete unnecessarily.
- Navigation has understandable entry, exit, back, cancel, and recovery paths.
- Copy uses consistent action names and gives specific next steps for errors.
- Empty, loading, success, partial, permission, and failure states are purposeful.

## 2. Design-system and visual quality

- Existing components, tokens, variants, and brand assets are used correctly.
- Any deviation from the design system is documented and approved.
- Palette, typography, spacing, radius, elevation, icons, and imagery form one coherent system.
- Hierarchy and composition remain clear with real content, not only ideal fixtures.
- The signature element is distinctive but does not overpower the task.
- Decoration communicates structure, state, brand, or narrative; otherwise remove it.
- Hover or active effects do not shift layout.

## 3. Semantic structure and accessibility

- Use native elements for their purpose: buttons for actions, links for navigation, labels for form controls, and real table semantics for tabular data.
- Maintain a logical heading structure and reading order.
- Give informative images alternative text and hide decorative graphics from assistive technology.
- Give icon-only controls accessible names and expose dynamic feedback appropriately.
- Support full keyboard use with visible focus and no keyboard traps.
- Never remove outlines without an equivalent focus-visible treatment.
- Do not disable zoom or block paste.
- Ensure contrast is sufficient and meaning never depends only on color.
- Respect reduced-motion and high-contrast behavior when supported.

## 4. Forms and interaction

- Inputs have persistent labels, meaningful names, suitable types/input modes, and appropriate autocomplete behavior.
- Validation is placed near the relevant field; submission directs focus to the first error when appropriate.
- Loading prevents duplicate submission without hiding the action's meaning.
- Destructive actions require confirmation or a practical undo path.
- Click/touch targets are large enough and interactive states are obvious.
- Links preserve browser navigation behavior such as opening in a new tab.
- Modals, drawers, menus, tooltips, and popovers manage focus, dismissal, scrolling, and layering correctly.

## 5. Responsive and content resilience

- Inspect representative desktop, tablet, narrow mobile, and project-required breakpoints.
- No unintended horizontal scrolling, clipped controls, overlapping content, or content hidden behind fixed elements.
- Touch layouts account for safe areas and avoid desktop-only hover assumptions.
- Long names, translated strings, large numbers, empty values, and user-generated content reflow safely.
- Flex/grid children can shrink correctly; truncation does not hide information users must act on.
- Tables have a deliberate narrow-screen strategy: priority columns, wrapping, scrolling, or cards according to the task.

## 6. Motion and perceived performance

- Motion has a clear purpose and remains interruptible.
- Avoid broad property transitions; animate inexpensive properties where feasible.
- Skeletons and placeholders preserve layout and do not masquerade as real content.
- Images declare dimensions; defer below-fold media and prioritize critical above-fold media appropriately.
- Fonts and assets do not cause avoidable flash, blocking, or layout shift.
- Large lists, frequent renders, DOM measurement, and client-side work are proportionate to the interaction.

## 7. Theme, locale, and state integrity

- Supported light, dark, and high-contrast themes retain readable text, borders, controls, and native widgets.
- Dates, times, numbers, currencies, and pluralization use locale-aware formatting.
- Stateful filters, tabs, pagination, or expanded views are deep-linkable when users need to share or restore them.
- Server/client rendering avoids hydration-dependent output mismatches.
- AI-generated content is labeled when transparency is relevant to user trust.

## 8. Rendered review loop

1. Render the affected screen with representative data.
2. Inspect at desktop, tablet, and mobile sizes required by the project.
3. Exercise keyboard navigation, focus, forms, errors, loading, empty data, themes, and reduced motion as applicable.
4. Compare against the approved design contract and existing product patterns.
5. Classify findings:
   - **Blocking:** prevents task completion, creates serious accessibility failure, data risk, or broken layout.
   - **Major:** materially harms usability, hierarchy, consistency, responsiveness, or performance.
   - **Minor:** polish issue with limited user impact.
6. Fix all in-scope blocking and major findings, then rerun the affected checks.
7. Report which sizes, states, themes, browsers, and tools were actually inspected.

Do not claim visual QA from source review, a build, or unit tests alone.
