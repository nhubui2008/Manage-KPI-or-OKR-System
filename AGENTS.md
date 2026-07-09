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
