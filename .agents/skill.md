---
name: manage-kpi-okr-system
description: Use when working in the Manage-KPI-or-OKR-System ASP.NET MVC repo, especially for UI/layout optimization, KPI/OKR workflow changes, white-label branding, RBAC-safe feature work, AI-assisted KPI/OKR screens, deployment packaging, and verification on Windows.
---

# Manage KPI/OKR System Skill

This repo is an ASP.NET Core MVC KPI/OKR management system with EF Core, SQL Server, Bootstrap 5, Bootstrap Icons, Be Vietnam Pro, dynamic branding, RBAC permissions, AI helpers, and a dense enterprise dashboard layout.

## First Reads

For UI/layout work, read [design.md](design.md) first, then inspect the specific view and shared shell:

- [Views/Shared/_Layout.cshtml](Views/Shared/_Layout.cshtml)
- [Views/Shared/_SaaSAdminLayout.cshtml](Views/Shared/_SaaSAdminLayout.cshtml)
- [wwwroot/css/site.css](wwwroot/css/site.css)
- [wwwroot/js/site.js](wwwroot/js/site.js)

For business logic, inspect the controller, model/viewmodel, service, helper, and migration that already own the flow before adding a new abstraction.

## Repo Map

- `Controllers/`: MVC actions and endpoint orchestration.
- `Models/`: EF entities and domain models.
- `Models/ViewModels/`: screen-specific data contracts.
- `Data/MiniERPDbContext.cs`: EF Core DbContext.
- `Services/`: AI, notifications, email, branding, workflow/progress services.
- `Helpers/`: access scope, permission, workflow status, SEO, progress, pagination.
- `Views/`: Razor views; shared app shell lives in `Views/Shared`.
- `wwwroot/css/site.css`: global design system and layout components.
- `wwwroot/js/site.js`: global UI behavior, anti-forgery helpers, sidebar behavior, toast helpers.
- `Migrations/`: EF migrations.
- `tests/ManageKpiOkrSystem.Tests/`: unit tests.

## Working Rules

- Preserve RBAC and data scope. Check `HasPermission`, role logic, `PermissionClaimsTransformation`, and `AccessScopeHelper` before exposing data or actions.
- Preserve dynamic branding. Use `ISystemSettingsService`/branding variables instead of hard-coded product names, logo URLs, favicon URLs, and primary colors.
- Keep UI real-data first. Do not introduce mock-only cards, fake dashboard numbers, or static AI output when controller/service data exists.
- Keep layout dense and operational. Follow `design.md`: page header, toolbar/filter, metric cards, content card/table, empty state.
- Use Bootstrap Icons for buttons and state markers.
- Prefer existing tokens/classes from `site.css`; add view-scoped CSS only when the component is unique to that view.
- Avoid destructive seed or database scripts unless the user explicitly asks. `seeddata.sql` contains broad demo data operations and should be reviewed before running.

## UI Change Workflow

1. Identify the exact view and shared layout involved.
2. Read nearby CSS in `site.css` before adding new styles.
3. Standardize structure:
   - `page-header` for title/breadcrumb/actions.
   - `toolbar` for search/filter/action rows.
   - `stat-card` or `overview-card` for KPI tiles.
   - `content-card` and `card-body-custom` for tables/forms.
   - `.table-responsive` plus clear min-width rules for wide tables.
4. Make mobile behavior explicit with Bootstrap grid and breakpoints.
5. Keep long Vietnamese labels from overflowing with wrapping, fixed table widths, or responsive stacking.
6. Verify no sidebar/header overlap on desktop and mobile.

## KPI/OKR Workflow Rules

- KPI status display should use `WorkflowStatusHelper` or existing status dictionaries.
- KPI progress/check-in scheduling should respect `KpiCheckInScheduleHelper` and existing deadline fields.
- Work project task links to KPI/OKR belong around `WorkProjectsController`, `WorkItem`, `KPIId`, `OKRKeyResultId`, and `KpiImpactWeight`.
- When adding KPI/OKR automation, update both write path and visible status/progress feedback.

## AI Rules

- AI calls should use existing `AIController`, `GeminiService`, and `AIDataService` partials where possible.
- Client POSTs must include anti-forgery headers via existing JS helpers.
- AI output blocks must handle loading, warnings, errors, empty result, and long text wrapping.
- Do not present AI suggestions as final database changes until the user applies/confirms them.

## Verification

Run focused checks for the touched surface:

```powershell
dotnet build .\Manage-KPI-or-OKR-System.csproj
dotnet test .\tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj
```

For UI/layout changes, also run the app and smoke-test at least one touched page when feasible.

If build fails because `Manage-KPI-or-OKR-System.exe` is locked, check for orphaned `dotnet` processes or `.build-check/server-*.pid`, stop only the relevant local app process, then rebuild.

For deployment packaging, prefer:

```powershell
dotnet publish .\Manage-KPI-or-OKR-System.csproj -c Release -o .\publish\plesk
dotnet ef migrations script --idempotent -o .\publish\database-migrations.sql
Compress-Archive -Path .\publish\plesk\* -DestinationPath .\publish\Manage-KPI-or-OKR-System-plesk-web.zip -Force
```

## Handoff Notes

When reporting changes, include:

- Files changed.
- What layout/behavior standard was applied.
- Verification command results.
- Any remaining manual smoke test needed, especially for pages requiring login, live database, Gemini API, SMTP, or production hosting settings.
