# Kanban AI Preview Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Gemini-powered Kanban AI preview flow reliable, review-first, and visually consistent with the existing project UI.

**Architecture:** Keep `AITaskDecompositionService` as the backend boundary for prompt, parse, normalize, and confirm logic. Keep `_AITaskDecomposeModal` as the review/edit surface and improve it without changing the endpoint contract.

**Tech Stack:** ASP.NET Core MVC, EF Core InMemory tests, xUnit, Razor partials, Bootstrap 5, Bootstrap Icons, vanilla JavaScript.

---

## File Structure

- Modify `tests/ManageKpiOkrSystem.Tests/AITaskDecompositionServiceTests.cs`: add failing tests for wrapped Gemini JSON, markdown-fenced JSON, duplicate filtering, normalization, and confirm filtering.
- Modify `Services/AITaskDecompositionService.cs`: parse Gemini output more defensively, normalize values case-insensitively, de-duplicate suggestions, improve prompts, and filter valid confirm tasks.
- Modify `Views/Shared/_AITaskDecomposeModal.cshtml`: redesign the modal preview with source summary, task cards/rows, Vietnamese labels, inline loading/empty/error states, and safer button state handling.
- Optionally verify `Views/WorkProjects/Details.cshtml`: keep the current modal trigger and ensure no contract changes are required.

---

### Task 1: Backend Regression Tests

**Files:**
- Modify: `tests/ManageKpiOkrSystem.Tests/AITaskDecompositionServiceTests.cs`
- Test: `tests/ManageKpiOkrSystem.Tests/AITaskDecompositionServiceTests.cs`

- [ ] **Step 1: Write failing tests for parse and normalization**

Add these tests after `DecomposeProjectAsync_UsesLinkedGoalContextAndMapsGeneratedTasksToKpiAndKeyResult`:

```csharp
[Fact]
public async Task DecomposeProjectAsync_ParsesWrappedMarkdownTasksAndNormalizesValues()
{
    await using var context = CreateContext();
    var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
    var project = new WorkProject
    {
        ProjectCode = "PRJ-20260702-001",
        ProjectName = "Improve onboarding",
        Description = "Reduce manual onboarding delays.",
        Status = "Active",
        Priority = "Normal",
        StartDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(30),
        IsActive = true,
        CreatedAt = DateTime.Now
    };
    context.WorkProjects.Add(project);
    await context.SaveChangesAsync();
    context.WorkProjectDepartments.Add(new WorkProjectDepartment
    {
        WorkProjectId = project.Id,
        DepartmentId = department.Id,
        CollaborationRole = "Owner",
        IsActive = true
    });
    await context.SaveChangesAsync();

    var gemini = new FakeGeminiService($$"""
        ```json
        {
          "tasks": [
            {
              "title": "  Build onboarding checklist  ",
              "description": "Create a checklist for every handoff.",
              "priority": "urgent",
              "assigneeId": {{employee.Id}},
              "departmentId": {{department.Id}},
              "kanbanStatus": "review",
              "estimatedDays": 0,
              "kpiImpactWeight": 0
            }
          ]
        }
        ```
        """);
    var service = CreateService(context, gemini);

    var response = await service.DecomposeProjectAsync(
        new DecomposeProjectRequest { WorkProjectId = project.Id },
        AdminPrincipal(),
        CancellationToken.None);

    Assert.True(response.Success);
    var task = Assert.Single(response.Tasks);
    Assert.Equal("Build onboarding checklist", task.Title);
    Assert.Equal("Urgent", task.Priority);
    Assert.Equal("Review", task.KanbanStatus);
    Assert.Equal(1, task.EstimatedDays);
    Assert.Equal(0.1m, task.KpiImpactWeight);
    Assert.Equal(employee.Id, task.AssigneeId);
    Assert.Equal(employee.FullName, task.AssigneeName);
    Assert.Equal(department.Id, task.DepartmentId);
    Assert.Equal(department.DepartmentName, task.DepartmentName);
}

[Fact]
public async Task DecomposeProjectAsync_DropsBlankAndDuplicateSuggestions()
{
    await using var context = CreateContext();
    var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
    var project = new WorkProject
    {
        ProjectCode = "PRJ-20260702-002",
        ProjectName = "Sales cleanup",
        Status = "Active",
        Priority = "Normal",
        IsActive = true,
        CreatedAt = DateTime.Now
    };
    context.WorkProjects.Add(project);
    await context.SaveChangesAsync();
    context.WorkProjectDepartments.Add(new WorkProjectDepartment
    {
        WorkProjectId = project.Id,
        DepartmentId = department.Id,
        CollaborationRole = "Owner",
        IsActive = true
    });
    await context.SaveChangesAsync();

    var gemini = new FakeGeminiService($$"""
        [
          { "title": "", "priority": "High" },
          { "title": "Clean CRM leads", "priority": "High", "assigneeId": {{employee.Id}}, "departmentId": {{department.Id}} },
          { "title": " clean   crm leads ", "priority": "Low", "assigneeId": {{employee.Id}}, "departmentId": {{department.Id}} }
        ]
        """);
    var service = CreateService(context, gemini);

    var response = await service.DecomposeProjectAsync(
        new DecomposeProjectRequest { WorkProjectId = project.Id },
        AdminPrincipal(),
        CancellationToken.None);

    Assert.True(response.Success);
    var task = Assert.Single(response.Tasks);
    Assert.Equal("Clean CRM leads", task.Title);
    Assert.Equal("High", task.Priority);
}
```

- [ ] **Step 2: Write failing test for confirm filtering**

Add this test near `ConfirmDecomposeAsync_CreatesProjectAndWorkItemsFromConfirmedTasks`:

```csharp
[Fact]
public async Task ConfirmDecomposeAsync_IgnoresBlankAndDuplicateReviewedTasks()
{
    await using var context = CreateContext();
    var (department, employee) = await SeedDepartmentAndEmployeeAsync(context);
    var service = CreateService(context, new FakeGeminiService("[]"));

    var response = await service.ConfirmDecomposeAsync(
        new ConfirmDecomposeRequest
        {
            NewProjectName = "Reviewed task plan",
            Tasks =
            {
                new DecomposedTaskDto { Title = " ", Priority = "Urgent" },
                new DecomposedTaskDto
                {
                    Title = "Prepare launch checklist",
                    Description = "Reviewed by the manager.",
                    Priority = "HIGH",
                    AssigneeId = employee.Id,
                    DepartmentId = department.Id,
                    KanbanStatus = "inprogress",
                    EstimatedDays = 2,
                    KpiImpactWeight = 2
                },
                new DecomposedTaskDto
                {
                    Title = " prepare   launch checklist ",
                    Priority = "Low",
                    AssigneeId = employee.Id,
                    DepartmentId = department.Id
                }
            }
        },
        AdminPrincipal(),
        CancellationToken.None);

    Assert.True(response.Success);
    Assert.Equal(1, response.TasksCreated);

    var task = await context.WorkItems.SingleAsync();
    Assert.Equal("Prepare launch checklist", task.Title);
    Assert.Equal("High", task.Priority);
    Assert.Equal("InProgress", task.KanbanStatus);
    Assert.Equal(50, task.ProgressPercentage);
}
```

- [ ] **Step 3: Run tests and verify they fail for the intended reason**

Run:

```powershell
dotnet test .\tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj --filter AITaskDecompositionServiceTests
```

Expected: the new tests fail because wrapped JSON is not parsed, lower-case priority/status are not normalized, and duplicate confirm tasks are not filtered.

---

### Task 2: Backend Implementation

**Files:**
- Modify: `Services/AITaskDecompositionService.cs`
- Test: `tests/ManageKpiOkrSystem.Tests/AITaskDecompositionServiceTests.cs`

- [ ] **Step 1: Add parse helpers and normalization helpers**

In `AITaskDecompositionService`, update `ParseTasks`, `ExtractJsonArray`, `NormalizePriority`, `NormalizeKanbanStatus`, and add a title key helper:

```csharp
private List<DecomposedTaskDto> ParseTasks(string text)
{
    var json = ExtractJsonPayload(text);
    if (string.IsNullOrWhiteSpace(json))
    {
        return new List<DecomposedTaskDto>();
    }

    try
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var taskElement = root.ValueKind == JsonValueKind.Array
            ? root
            : root.ValueKind == JsonValueKind.Object && root.TryGetProperty("tasks", out var tasks)
                ? tasks
                : default;

        if (taskElement.ValueKind != JsonValueKind.Array)
        {
            return new List<DecomposedTaskDto>();
        }

        return JsonSerializer.Deserialize<List<DecomposedTaskDto>>(taskElement.GetRawText(), _jsonOptions)
            ?? new List<DecomposedTaskDto>();
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, "Gemini returned invalid task JSON.");
        return new List<DecomposedTaskDto>();
    }
}

private static string ExtractJsonPayload(string text)
{
    var trimmed = text.Trim();
    if (trimmed.StartsWith("```", StringComparison.Ordinal))
    {
        trimmed = trimmed.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("```", string.Empty)
            .Trim();
    }

    var arrayStart = trimmed.IndexOf('[');
    var objectStart = trimmed.IndexOf('{');
    var startsWithObject = objectStart >= 0 && (arrayStart < 0 || objectStart < arrayStart);
    if (startsWithObject)
    {
        var objectEnd = trimmed.LastIndexOf('}');
        return objectEnd > objectStart ? trimmed[objectStart..(objectEnd + 1)] : trimmed;
    }

    var arrayEnd = trimmed.LastIndexOf(']');
    return arrayStart >= 0 && arrayEnd > arrayStart ? trimmed[arrayStart..(arrayEnd + 1)] : trimmed;
}

private static string NormalizePriority(string? priority)
{
    var match = Priorities.FirstOrDefault(item => string.Equals(item, priority?.Trim(), StringComparison.OrdinalIgnoreCase));
    return match ?? "Normal";
}

private static string NormalizeKanbanStatus(string? status)
{
    var match = KanbanStatuses.FirstOrDefault(item => string.Equals(item, status?.Trim(), StringComparison.OrdinalIgnoreCase));
    return match ?? "Todo";
}

private static string NormalizeTitleKey(string? title)
{
    return string.Join(' ', (title ?? string.Empty)
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .ToUpperInvariant();
}
```

- [ ] **Step 2: Filter and de-duplicate generated suggestions**

In `MapTasksAsync`, replace the return pipeline with a grouped pipeline:

```csharp
return parsedTasks
    .Where(t => !string.IsNullOrWhiteSpace(t.Title))
    .GroupBy(t => NormalizeTitleKey(t.Title))
    .Where(group => !string.IsNullOrWhiteSpace(group.Key))
    .Select(group => group.First())
    .Take(10)
    .Select(t =>
    {
        var assignee = t.AssigneeId.HasValue && contextBundle.Employees.TryGetValue(t.AssigneeId.Value, out var employee)
            ? employee
            : null;
        var department = t.DepartmentId.HasValue && contextBundle.Departments.TryGetValue(t.DepartmentId.Value, out var dept)
            ? dept
            : assignee?.Department;
        var keyResultId = t.OKRKeyResultId.HasValue && keyResultIds.Contains(t.OKRKeyResultId.Value)
            ? t.OKRKeyResultId
            : okr?.KeyResults.FirstOrDefault()?.Id ?? kpi?.OKRKeyResultId;
        var mappedKpiId = t.KPIId.HasValue && kpiIds.Contains(t.KPIId.Value)
            ? t.KPIId
            : kpi?.Id;

        return new DecomposedTaskDto
        {
            Title = Trim(t.Title, 220),
            Description = Trim(t.Description, 2000),
            Priority = NormalizePriority(t.Priority),
            AssigneeId = assignee?.Id,
            AssigneeName = assignee?.Name,
            DepartmentId = department?.Id,
            DepartmentName = department?.Name,
            KanbanStatus = NormalizeKanbanStatus(t.KanbanStatus),
            EstimatedDays = Math.Clamp(t.EstimatedDays <= 0 ? 1 : t.EstimatedDays, 1, 365),
            KpiImpactWeight = NormalizeImpactWeight(t.KpiImpactWeight),
            KPIId = mappedKpiId,
            OKRKeyResultId = keyResultId,
            KeyResultName = okr?.KeyResults.FirstOrDefault(kr => kr.Id == keyResultId)?.KeyResultName
        };
    })
    .ToList();
```

- [ ] **Step 3: Filter reviewed confirm tasks**

In `ConfirmDecomposeAsync`, replace `validTasks` creation with:

```csharp
var validTasks = request.Tasks
    .Where(t => !string.IsNullOrWhiteSpace(t.Title))
    .GroupBy(t => NormalizeTitleKey(t.Title))
    .Where(group => !string.IsNullOrWhiteSpace(group.Key))
    .Select(group => group.First())
    .ToList();
```

- [ ] **Step 4: Improve prompt contracts**

Update the three prompt builder methods so the instruction explicitly says to return either a JSON array or `{ "tasks": [...] }`, only canonical values, and no duplicate tasks.

- [ ] **Step 5: Run targeted tests and verify pass**

Run:

```powershell
dotnet test .\tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj --filter AITaskDecompositionServiceTests
```

Expected: all `AITaskDecompositionServiceTests` pass.

---

### Task 3: Modal UI/UX Upgrade

**Files:**
- Modify: `Views/Shared/_AITaskDecomposeModal.cshtml`
- Verify: `Views/WorkProjects/Details.cshtml`

- [ ] **Step 1: Replace technical table with review-focused layout**

Keep the same element IDs used by JavaScript, but update modal markup to include:

```html
<div class="ai-task-modal__summary d-none" id="aiTaskSummary"></div>
<div id="aiTaskEmpty" class="ai-task-empty d-none">Gemini chưa trả về task phù hợp.</div>
<div id="aiTaskTableBody" class="ai-task-review-list"></div>
```

Each generated task row must keep `.js-ai-task-field` inputs for `title`, `description`, `priority`, `kanbanStatus`, `assigneeId`, `departmentId`, `estimatedDays`, `kpiImpactWeight`, `kpiId`, and `okrKeyResultId`.

- [ ] **Step 2: Add modal-scoped CSS**

Use existing tokens such as `--card-bg`, `--card-border`, `--primary-light`, `--context-primary-text`, `--radius-md`, and `--transition-fast`. Keep radii at 8px or below for controls and avoid introducing a new palette.

- [ ] **Step 3: Upgrade JavaScript state handling**

Update the modal script to:

```javascript
const statusLabels = { Backlog: 'Backlog', Todo: 'Cần làm', InProgress: 'Đang làm', Review: 'Chờ review', Done: 'Hoàn thành', Blocked: 'Đang vướng' };
const priorityLabels = { Low: 'Thấp', Normal: 'Bình thường', High: 'Cao', Urgent: 'Khẩn cấp' };
```

Render assignee and department names as visible text, preserve IDs in hidden fields, update the summary after edits/removals, disable confirm while loading/confirming, and show inline errors without clearing reviewed tasks.

- [ ] **Step 4: Confirm payload compatibility**

Verify `collectTasks()` still returns:

```javascript
{
  title,
  description,
  priority,
  kanbanStatus,
  assigneeId,
  departmentId,
  estimatedDays,
  kpiImpactWeight,
  kpiId,
  okrKeyResultId
}
```

---

### Task 4: Verification

**Files:**
- Verify all modified files.

- [ ] **Step 1: Run targeted tests**

Run:

```powershell
dotnet test .\tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj --filter AITaskDecompositionServiceTests
```

Expected: all targeted tests pass.

- [ ] **Step 2: Run full test project**

Run:

```powershell
dotnet test .\tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj
```

Expected: all tests pass.

- [ ] **Step 3: Build application**

Run:

```powershell
dotnet build .\Manage-KPI-or-OKR-System.csproj
```

Expected: build succeeds without errors.

- [ ] **Step 4: Inspect final diff**

Run:

```powershell
git diff -- Services/AITaskDecompositionService.cs tests/ManageKpiOkrSystem.Tests/AITaskDecompositionServiceTests.cs Views/Shared/_AITaskDecomposeModal.cshtml docs/superpowers/plans/2026-07-02-kanban-ai-preview.md
```

Expected: diff is limited to the planned backend tests, backend service, modal partial, and plan file.

---

## Self-Review

- Spec coverage: backend parsing, normalization, prompt clarity, modal UX, error states, and verification are covered.
- Placeholder scan: no TBD/TODO/fill-in steps remain.
- Type consistency: tests and implementation use existing `DecomposeProjectRequest`, `ConfirmDecomposeRequest`, `DecomposedTaskDto`, and endpoint-compatible JSON field names.
