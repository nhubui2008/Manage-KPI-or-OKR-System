# Tự Động Sinh Dự Án Vận Hành Khi Tạo OKR

## Trạng Thái Đã Thực Hiện - 23/06/2026

Codex đã thực hiện xong phần triển khai theo kế hoạch này trên branch `Ngthebao-auto-create-project-from-okr`.

### Hạng mục đã hoàn thành

| # | Hạng mục | Trạng thái | Ghi chú |
|---|----------|------------|---------|
| 1 | Sửa `Models/OKR.cs` | ✅ Hoàn thành | Đã thêm `LinkedWorkProjectId` để OKR trỏ tới project vận hành đã sinh |
| 2 | Sửa `Models/WorkProject.cs` | ✅ Hoàn thành | Đã thêm `SourceOKRId` để project biết nguồn OKR |
| 3 | Tạo migration | ✅ Hoàn thành | Đã tạo `Migrations/20260623090301_AddOKRWorkProjectLink.cs` và cập nhật `MiniERPDbContextModelSnapshot.cs` |
| 4 | Tạo `Services/OKRWorkflowService.cs` | ✅ Hoàn thành | Đã thêm `IOKRWorkflowService` và `OKRWorkflowService` với logic tự sinh project/task |
| 5 | Đăng ký DI trong `Program.cs` | ✅ Hoàn thành | Đã đăng ký `IOKRWorkflowService` → `OKRWorkflowService` |
| 6 | Sửa `Controllers/OKRsController.cs` | ✅ Hoàn thành | Đã inject workflow service và gọi sau khi tạo OKR, thêm 1 KR, thêm nhiều KR; các lời gọi được bọc `try/catch` |
| 7 | Sửa `Views/OKRs/Index.cshtml` | ✅ Hoàn thành | Đã thêm link `Dự án vận hành` khi OKR có `LinkedWorkProjectId` |
| 8 | Sửa `Views/OKRs/Create.cshtml` | ✅ Hoàn thành | Đã thêm thông báo hệ thống sẽ tự tạo dự án vận hành và task Kanban |
| 9 | Thêm test tự động | ✅ Hoàn thành | Đã thêm test project `tests/ManageKpiOkrSystem.Tests` kiểm tra workflow service |
| 10 | Exclude test folder khỏi web project | ✅ Hoàn thành | Đã thêm `tests\**` vào `DefaultItemExcludes` trong `Manage-KPI-or-OKR-System.csproj` |

### Logic đã triển khai

- Khi tạo OKR mới: hệ thống tự tạo `WorkProject` có tên dạng `[OKR] {ObjectiveName}`, liên kết `WorkProject.SourceOKRId` và `OKR.LinkedWorkProjectId`.
- Nếu OKR có department allocation, project tự sinh được gắn `WorkProjectDepartment` với `CollaborationRole = "Owner"`.
- Khi OKR đã có Key Results, service tự sinh `WorkItem` tương ứng cho từng KR.
- Khi thêm KR sau đó, hệ thống tự sinh thêm task Kanban cho KR mới.
- Service có kiểm tra để tránh tạo trùng project hoặc task cho cùng KR.
- Due date của project/task được suy ra từ `Cycle`, hỗ trợ `Q1`, `Q2`, `Q3`, `Q4`, `Năm` và `Nam`.
- Lỗi tự sinh project/task không làm hỏng flow chính tạo OKR hoặc thêm KR, vì controller đã bọc các lời gọi bằng `try/catch`.

### Verification đã chạy

```bash
dotnet test tests\ManageKpiOkrSystem.Tests\ManageKpiOkrSystem.Tests.csproj
```

Kết quả: `Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3`

```bash
dotnet build
```

Kết quả: `Build succeeded. 0 Warning(s), 0 Error(s)`

### Việc chưa chạy tự động

- Chưa chạy `dotnet ef database update`; bước này vẫn thuộc manual verification/deploy để apply migration vào database thực tế.
- Chưa thực hiện manual verification qua UI bằng tài khoản Admin/Manager.

---

## Mô tả

Khi người dùng tạo một OKR mới (có kèm Key Results), hệ thống sẽ **tự động**:
1. Tạo một `WorkProject` tương ứng với OKR đó (tên dự án = tên Objective)
2. Với mỗi `OKRKeyResult` được thêm vào OKR, tự động tạo một `WorkItem` (task) trong dự án đó, liên kết ngược về KR

Kết quả: Từ OKR → sinh ra quy trình vận hành hoàn chỉnh trên board Kanban.

---

## Phân Tích Codebase Hiện Tại

### Models đã có sẵn (KHÔNG cần tạo mới)
- **`OKR`** (`Models/OKR.cs`): `Id`, `ObjectiveName`, `Cycle`, `OKRTypeId`, `StatusId`, `CreatedById`, `KeyResults` (navigation)
- **`OKRKeyResult`** (`Models/OKRKeyResult.cs`): `Id`, `OKRId`, `KeyResultName`, `TargetValue`, `CurrentValue`, `Unit`, `IsInverse`
- **`WorkProject`** (`Models/WorkProject.cs`): `Id`, `ProjectCode`, `ProjectName`, `Description`, `OwnerId`, `Priority`, `Status`, `StartDate`, `DueDate`, `CreatedById`, `Departments`, `WorkItems`
- **`WorkItem`** (`Models/WorkItem.cs`): `Id`, `WorkProjectId`, `Title`, `Description`, `AssigneeId`, `OKRKeyResultId`, `KanbanStatus`, `Priority`, `DueDate`
- **`WorkProjectDepartment`** (`Models/WorkProjectDepartment.cs`): `Id`, `WorkProjectId`, `DepartmentId`

### Quan hệ quan trọng
- `WorkItem` đã có field `OKRKeyResultId` → có thể liên kết task với KR
- `WorkProject` **chưa có** field liên kết ngược về OKR → cần thêm `SourceOKRId`
- `OKR` **chưa có** field liên kết sang project → cần thêm `LinkedWorkProjectId`

### Controllers liên quan
- **`OKRsController.cs`** (1232 dòng): Chứa `Create(POST)` ở dòng 200-262 và `AddKeyResult(POST)` ở dòng 395-423, `AddMultipleKeyResults(POST)` ở dòng 488-522
- **`WorkProjectsController.cs`** (1204 dòng): Chứa `GenerateProjectCodeAsync()` ở dòng 1029-1034, `ReplaceProjectDepartmentsAsync()` ở dòng 970-987

### Helper/Utility đã có
- `AccessScopeHelper.GetCurrentEmployeeAsync()` — lấy employee hiện tại
- `GenerateProjectCodeAsync()` — tạo mã dự án `PRJ-yyyyMMdd-001`

---

## Proposed Changes

### Phase 1: Database — Thêm liên kết OKR ↔ WorkProject

#### [MODIFY] [WorkProject.cs](file:///e:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System/Models/WorkProject.cs)
Thêm field `SourceOKRId` (nullable int) để biết project này được sinh từ OKR nào:
```csharp
// Thêm sau dòng public bool? IsActive { get; set; } = true; (line 38)
public int? SourceOKRId { get; set; }
```

#### [MODIFY] [OKR.cs](file:///e:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System/Models/OKR.cs)
Thêm field `LinkedWorkProjectId` (nullable int) để truy nhanh project đã sinh:
```csharp
// Thêm sau dòng public DateTime? CreatedAt { get; set; } = DateTime.Now; (line 19)
public int? LinkedWorkProjectId { get; set; }
```

#### [NEW] Migration file
Tạo migration mới bằng lệnh:
```bash
dotnet ef migrations add AddOKRWorkProjectLink
```
Migration sẽ thêm 2 cột:
- `WorkProjects.SourceOKRId` (int, nullable)
- `OKRs.LinkedWorkProjectId` (int, nullable)

---

### Phase 2: Service — Tạo service xử lý logic tự động sinh

#### [NEW] [OKRWorkflowService.cs](file:///e:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System/Services/OKRWorkflowService.cs)

Tạo service mới `IOKRWorkflowService` / `OKRWorkflowService` với 2 method chính:

```csharp
namespace Manage_KPI_or_OKR_System.Services
{
    public interface IOKRWorkflowService
    {
        /// <summary>
        /// Khi tạo OKR xong, tự động sinh WorkProject + WorkItems từ KeyResults.
        /// </summary>
        Task<WorkProject?> AutoCreateProjectFromOKRAsync(int okrId, int? createdByEmployeeId, int? departmentId);

        /// <summary>
        /// Khi thêm KR mới vào OKR đã có project, tự động thêm WorkItem tương ứng.
        /// </summary>
        Task AutoCreateTaskFromKeyResultAsync(int okrId, OKRKeyResult keyResult);
    }
}
```

**Logic chi tiết cho `AutoCreateProjectFromOKRAsync`:**
1. Load OKR kèm KeyResults từ DB
2. Gọi `GenerateProjectCodeAsync()` để tạo mã project
3. Tạo `WorkProject`:
   - `ProjectName` = `"[OKR] " + okr.ObjectiveName`
   - `Description` = `"Dự án tự động sinh từ OKR: " + okr.ObjectiveName`
   - `ProjectCode` = sinh tự động (cùng logic `PRJ-yyyyMMdd-001`)
   - `Status` = `"Active"`
   - `Priority` = `"Normal"`
   - `OwnerId` = `createdByEmployeeId`
   - `CreatedById` = `createdByEmployeeId`
   - `SourceOKRId` = `okr.Id`
   - `StartDate` = `DateTime.Today`
   - `DueDate` = tính từ Cycle (Q1→31/03, Q2→30/06, Q3→30/09, Q4→31/12, Năm→31/12)
4. SaveChanges để có `project.Id`
5. Nếu `departmentId` có giá trị → tạo `WorkProjectDepartment`
6. Cập nhật `okr.LinkedWorkProjectId = project.Id`
7. Với mỗi KeyResult trong `okr.KeyResults` → tạo `WorkItem`:
   - `WorkProjectId` = `project.Id`
   - `Title` = `kr.KeyResultName`
   - `Description` = `$"Mục tiêu: {kr.TargetValue} {kr.Unit}"`
   - `OKRKeyResultId` = `kr.Id`
   - `KanbanStatus` = `"Todo"`
   - `Priority` = `"Normal"`
   - `DueDate` = project.DueDate (cùng deadline)
8. SaveChanges
9. Return project

**Logic chi tiết cho `AutoCreateTaskFromKeyResultAsync`:**
1. Load OKR, kiểm tra `LinkedWorkProjectId` có giá trị không
2. Nếu chưa có project → gọi `AutoCreateProjectFromOKRAsync` trước
3. Nếu đã có project → tạo thêm `WorkItem` cho KR mới:
   - `WorkProjectId` = `okr.LinkedWorkProjectId`
   - `Title` = `kr.KeyResultName`
   - `Description` = `$"Mục tiêu: {kr.TargetValue} {kr.Unit}"`
   - `OKRKeyResultId` = `kr.Id`
   - `KanbanStatus` = `"Todo"`
4. SaveChanges

**Hàm helper tính DueDate từ Cycle:**
```csharp
private DateTime? ResolveDueDateFromCycle(string? cycle)
{
    if (string.IsNullOrEmpty(cycle)) return DateTime.Today.AddMonths(3);
    var year = DateTime.Now.Year;
    // Parse cycle format: "Q1-2026", "Q2-2026", "Năm 2026"
    if (cycle.StartsWith("Q1")) return new DateTime(year, 3, 31);
    if (cycle.StartsWith("Q2")) return new DateTime(year, 6, 30);
    if (cycle.StartsWith("Q3")) return new DateTime(year, 9, 30);
    if (cycle.StartsWith("Q4")) return new DateTime(year, 12, 31);
    if (cycle.Contains("Năm")) return new DateTime(year, 12, 31);
    return DateTime.Today.AddMonths(3);
}
```

**Hàm helper sinh mã project (copy logic từ WorkProjectsController dòng 1029-1034):**
```csharp
private async Task<string> GenerateProjectCodeAsync()
{
    var datePart = DateTime.Now.ToString("yyyyMMdd");
    var countToday = await _context.WorkProjects
        .CountAsync(p => p.ProjectCode != null && p.ProjectCode.StartsWith($"PRJ-{datePart}"));
    return $"PRJ-{datePart}-{countToday + 1:000}";
}
```

---

### Phase 3: Đăng ký DI — Register service

#### [MODIFY] [Program.cs](file:///e:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System/Program.cs)
Thêm dòng đăng ký service:
```csharp
builder.Services.AddScoped<IOKRWorkflowService, OKRWorkflowService>();
```
Thêm vào cùng nhóm với các service khác đã có (tìm dòng có `AddScoped` khác).

---

### Phase 4: Controller — Gọi service khi tạo OKR / thêm KR

#### [MODIFY] [OKRsController.cs](file:///e:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System/Controllers/OKRsController.cs)

**4.1. Inject service mới vào constructor (dòng 20-27):**
```csharp
private readonly MiniERPDbContext _context;
private readonly IGeminiService _geminiService;
private readonly IOKRWorkflowService _workflowService; // THÊM

public OKRsController(MiniERPDbContext context, IGeminiService geminiService, IOKRWorkflowService workflowService) // SỬA
{
    _context = context;
    _geminiService = geminiService;
    _workflowService = workflowService; // THÊM
}
```

**4.2. Trong method `Create(POST)` (dòng 200-262) — sau khi SaveChanges phân bổ (dòng 253), thêm:**
```csharp
// === TỰ ĐỘNG SINH DỰ ÁN TỪ OKR ===
await _workflowService.AutoCreateProjectFromOKRAsync(model.Id, model.CreatedById, departmentId);
```

Vị trí chèn: **sau dòng 253** (`await _context.SaveChangesAsync();`) và **trước dòng 255** (`TempData["SuccessMessage"]`).

Cập nhật TempData message (dòng 255):
```csharp
TempData["SuccessMessage"] = "Đã tạo OKR mới, phân bổ và tự động sinh dự án vận hành thành công!";
```

**4.3. Trong method `AddKeyResult(POST)` (dòng 395-423) — sau SaveChanges (dòng 416), thêm:**
```csharp
// Tự động tạo task trong project liên kết
await _workflowService.AutoCreateTaskFromKeyResultAsync(kr.OKRId!.Value, kr);
```

**4.4. Trong method `AddMultipleKeyResults(POST)` (dòng 488-522) — sau vòng foreach SaveChanges (dòng 516), thêm:**
```csharp
// Tự động tạo tasks trong project liên kết
foreach (var kr in keyResults)
{
    await _workflowService.AutoCreateTaskFromKeyResultAsync(okrId, kr);
}
```

---

### Phase 5: UI — Hiển thị link đến dự án trên trang OKR

#### [MODIFY] [Views/OKRs/Index.cshtml](file:///e:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System/Views/OKRs/Index.cshtml)

Trong phần hiển thị mỗi OKR card, thêm một nút/link nhỏ nếu OKR có `LinkedWorkProjectId`:
```html
@if (okr.LinkedWorkProjectId.HasValue)
{
    <a asp-controller="WorkProjects" asp-action="Details" asp-route-id="@okr.LinkedWorkProjectId" 
       class="btn btn-sm btn-outline-primary mt-2" title="Xem dự án vận hành">
        <i class="bi bi-kanban me-1"></i> Xem dự án vận hành
    </a>
}
```

#### [MODIFY] [Views/OKRs/Create.cshtml](file:///e:/Dự Án Tốt Nghiệp/Manage-KPI-or-OKR-System/Views/OKRs/Create.cshtml)

Thêm thông báo vào sidebar (sau dòng 242) cho người dùng biết sẽ tự động sinh dự án:
```html
<div class="okr-info-step">
    <div class="okr-info-step-num" style="background: linear-gradient(135deg, #7c3aed, #a855f7);">4</div>
    <p class="small text-muted mb-0">Hệ thống sẽ <strong>tự động tạo Dự án vận hành</strong> và các công việc trên bảng Kanban từ OKR và Key Results.</p>
</div>
```

---

## Tóm Tắt Các File Cần Thay Đổi

| # | File | Hành động | Mô tả |
|---|------|-----------|-------|
| 1 | `Models/OKR.cs` | MODIFY | Thêm `LinkedWorkProjectId` |
| 2 | `Models/WorkProject.cs` | MODIFY | Thêm `SourceOKRId` |
| 3 | Migration | NEW | `dotnet ef migrations add AddOKRWorkProjectLink` |
| 4 | `Services/OKRWorkflowService.cs` | NEW | Interface + Implementation |
| 5 | `Program.cs` | MODIFY | Đăng ký DI |
| 6 | `Controllers/OKRsController.cs` | MODIFY | Gọi service ở Create, AddKeyResult, AddMultipleKeyResults |
| 7 | `Views/OKRs/Index.cshtml` | MODIFY | Hiển thị link dự án |
| 8 | `Views/OKRs/Create.cshtml` | MODIFY | Thêm note tự động sinh dự án |

---

## Thứ Tự Thực Hiện (Cho Codex)

```
Bước 1 → Sửa Models (OKR.cs, WorkProject.cs)
Bước 2 → Tạo Migration
Bước 3 → Tạo file Services/OKRWorkflowService.cs (interface + class)
Bước 4 → Sửa Program.cs (đăng ký DI)
Bước 5 → Sửa OKRsController.cs (inject + gọi service)
Bước 6 → Sửa Views (Index.cshtml, Create.cshtml)
Bước 7 → Build kiểm tra: dotnet build
```

---

## Verification Plan

### Build Check
```bash
cd "e:\Dự Án Tốt Nghiệp\Manage-KPI-or-OKR-System"
dotnet build
```

### Manual Verification
1. Chạy `dotnet ef database update` để apply migration
2. Đăng nhập hệ thống với tài khoản Admin/Manager
3. Tạo một OKR mới với Objective name bất kỳ
4. Kiểm tra: WorkProjects → xuất hiện project mới có prefix `[OKR]`
5. Thêm Key Results cho OKR đó
6. Kiểm tra: Trong project details → xuất hiện các task tương ứng trên board Kanban
7. Kiểm tra link "Xem dự án vận hành" trên trang OKR Index

---

## Open Questions

> [!IMPORTANT]
> **Checkbox tùy chọn hay luôn tự động?** Hiện tại kế hoạch này sẽ **luôn tự động** tạo project khi tạo OKR. Nếu bạn muốn thêm checkbox "Tự động tạo dự án vận hành" trên form Create OKR để người dùng chọn có/không, hãy cho tôi biết.

> [!NOTE]
> **Key Results thêm sau:** Khi OKR được tạo, ban đầu chưa có KR nào (KR được thêm sau trên trang Index). Vì vậy project sẽ được tạo trước (rỗng, chưa có task), và task sẽ được thêm dần mỗi khi thêm KR.
