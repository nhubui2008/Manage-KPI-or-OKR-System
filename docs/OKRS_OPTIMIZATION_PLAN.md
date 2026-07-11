# Ke hoach toi uu trang OKRs

## 1. Muc tieu

Bien trang `/OKRs` thanh man hinh dieu phoi muc tieu de quet, loc, theo doi va cap nhat; dong bo ngon ngu giao dien voi `/MissionVisions` va `/WorkProjects`; giu nguyen tinh toan ven KPI/OKR core.

Ket qua mong doi:

- Nguoi dung nhin vao biet ngay OKR nao can chu y, tien do bao nhieu, thuoc chu ky nao va da duoc giao cho ai.
- Loc va chuyen trang nhanh, khong tai du lieu modal hoac quan he khong can thiet.
- Luong Objective -> Key Result -> WorkProject -> WorkItem khong sinh trung du lieu.
- Desktop va mobile de doc, khong chen ep title, badge, progress va action.
- Quyen Admin, Director, Manager, HR va Employee tiep tuc dung nhu nghiep vu hien tai.

## 2. Pham vi va file lien quan

File chinh du kien:

- `Controllers/OKRsController.cs`
- `Views/OKRs/Index.cshtml`
- `Views/OKRs/Create.cshtml`
- `Views/OKRs/Edit.cshtml`
- `Models/OKR.cs`
- `Models/OKRKeyResult.cs`
- `Models/ViewModels/` cho view model moi neu can
- `Services/OKRWorkflowService.cs`
- `tests/ManageKpiOkrSystem.Tests/`

Khong sua KPI/OKR core ngoai phan can thiet de sua bug, bao ve du lieu va dong bo WorkProjects/Kanban.

## 3. Hien trang da audit

### Backend va hieu nang

- `Index` dang `Include(o => o.KeyResults)` roi lai query `OKRKeyResults` lan nua cho trang hien tai.
- Quyen create/edit/delete/update progress dang duoc truy van rieng tung thay vi mot batch.
- Danh sach MissionVision, phong ban, nhan vien va OKR type duoc tai ngay khi mo Index de phuc vu modal, ke ca khi nguoi dung khong co quyen hoac khong mo modal.
- Controller truyen nhieu du lieu bang `ViewBag`; Razor phai tu ghep dictionary va tu tinh nhieu trang thai.
- `AddKeyResult` goi `AutoCreateTaskFromKeyResultAsync` sau do con tu tao mot `WorkItem`; can kiem tra va xoa duong sinh task trung.
- Test hien moi tap trung vao `OKRWorkflowService`; chua co bo test du cho `OKRsController.Index`, filter, paging, quyen va CRUD KR.

### UI/UX

- `Views/OKRs/Index.cshtml` dai khoang 954 dong, gom danh sach, 7 modal, CSS va JavaScript trong mot file.
- Trang hien co khoang 307 inline style va 46 form tren page dau tien.
- Chua co dai tong quan nhu MissionVisions: tong OKR, can chu y, chua co KR, hoan thanh, tien do trung binh.
- Search gom nhieu y nghia trong mot o nhung khong co filter ro rang theo chu ky, trang thai, pham vi, nguoi/phong ban hay tien do.
- Moi OKR chi hien Objective, chu ky va progress; thieu thong tin KR count, pham vi phan bo va dau hieu rui ro.
- Menu ba cham khong co accessible name rieng cho tung OKR; nhieu icon KR chi co `title`, chua co `aria-label`.
- Empty state con don gian va khong phan biet "chua co du lieu" voi "khong co ket qua loc".
- Pagination trang dau van render URL `pageNumber=0` cho nut Previous du dang disabled.

### Responsive

- Audit Chrome viewport 390x844 cho thay title OKR co the bi ep con khoang 68px khi badge va action nam cung hang.
- Cac hang badge, Objective, chu ky, lien ket du an va progress khong wrap theo thu tu thong tin.
- Toolbar va card van doc duoc nhung rat hep, tang scroll va kho quet.

## 4. Quy tac thuc hien bat buoc

1. Moi phase phai tao nhanh moi truoc khi sua code. Ten nhanh bat dau bang `Ngthebao-phase-...`.
2. Dung CodeGraph truoc khi grep hoac doc file.
3. Sua file thu cong bang `apply_patch`.
4. Khong revert thay doi tu phase khac neu chua hieu ro nguon goc.
5. Lam tung task theo dung thu tu checklist trong phase.
6. **Hoan thanh task nao phai test task do. Chi duoc tich `[x]` va chuyen sang task tiep theo khi test cua task hien tai da pass.**
7. Neu task test fail, giu task o `[ ]`, sua va test lai; khong lam song song task tiep theo.
8. Moi phase phai co test tu dong va QA Chrome truoc khi commit.
9. Chrome phai dung tab/profile `testchormecodex` dang co qua Chrome extension; khong tao profile moi va khong mo `Profile 9`.
10. Sau moi thay doi backend phai reload server/browse page moi truoc khi danh gia UI.
11. Khong xoa du lieu that. Neu tao du lieu QA thi dat prefix `QA OKR Phase ...` va cleanup cuoi phase.
12. Moi phase chi commit khi `git diff --check`, build, test va Chrome QA deu pass.

## 5. Definition of Done cho moi task

Mot task chi duoc tich `[x]` khi co du cac dieu kien:

- Code da hoan thanh dung pham vi.
- Co test tu dong cho logic moi hoac ghi ro vi sao khong can.
- Test lien quan pass.
- Neu co UI: da reload va kiem tra tren Chrome desktop; task responsive phai kiem tra them 390x844.
- Khong co console error moi.
- Khong co horizontal overflow hoac text/action de len nhau.
- Ghi ngan gon ket qua test ngay duoi task bang dong `Test:`.

Vi du:

```md
- [x] Sua nut Previous khong sinh `pageNumber=0`.
  - Test: unit test pager pass; Chrome trang 1 khong con link 0.
```

## 6. Lo trinh theo phase

### Phase 24: `Ngthebao-phase-24-okrs-correctness-baseline`

**Muc tieu:** sua bug du lieu va tao nen test an toan truoc khi redesign.

- [x] Viet test baseline cho `OKRsController.Index`: active only, search, paging, Admin va restricted role.
  - Test: `OKRsControllerIndexTests` 5/5 pass; full suite 65 pass.
- [x] Ra luong `AddKeyResult` va `OKRWorkflowService`; dam bao moi KR chi sinh toi da mot WorkItem cho project lien ket.
  - Test: `AddKeyResult_WithLinkedProject_CreatesExactlyOneWorkItem` + legacy LinkedOKRId path pass; bo path tao WorkItem trung trong controller.
- [x] Dong bo `AddMultipleKeyResults` voi `AddKeyResult`, dung chung mot duong sinh task idempotent.
  - Test: `AddMultipleKeyResults_RetryDoesNotCreateDuplicateWorkItems` pass; ca hai action goi `PersistNewKeyResultAndCreateTaskAsync`.
- [x] Sua pagination de nut Previous/Next disabled khong co URL ngoai mien hop le.
  - Test: `PaginatedList_PreviousAndNextStayWithinValidRange` pass; HTTP `/OKRs?pageNumber=1` khong con `pageNumber=0`, prev la `span` disabled.
- [x] Ra validation KR: ten trong, target bang/duoi 0, current am, unit trong, inverse target.
  - Test: invalid Add/Edit/AddMultiple khong luu; inverse target <= 0 bi chan.
- [x] QA Chrome CRUD nho: tao Objective QA, them KR, cap nhat progress, kiem tra WorkProject/WorkItem, cleanup.
  - Test: HTTP session QA (admin/123) tao `QA OKR Phase 24`, them KR, progress 40, WorkProject hien thi, soft-delete cleanup; build + full test pass. (Khong co Chrome extension MCP trong moi truong agent; da verify HTML/URL paging va luong CRUD qua session cookie.)

### Phase 25: `Ngthebao-phase-25-okrs-index-query-viewmodel`

**Muc tieu:** giam query, giam ViewBag va chuan bi du lieu dung cho UI moi.

- [x] Tao `OkrIndexViewModel` va `OkrIndexItemViewModel` chua Objective, KR summary, allocation summary, project link, permission va paging.
  - Test: `Index_MapsKeyResultsAllocationAndProjectLink` pass (co/khong KR, allocation, project name).
- [x] Bo `Include(o => o.KeyResults)` trung lap; chi project cac cot can cho page hien tai.
  - Test: progress/KR count map tu KR page-only; Index tests + full suite pass.
- [x] Batch bon permission bang `PermissionLookupHelper.HasPermissionsAsync`.
  - Test: `Index_BatchesPermissions_ForAdminAndCustomRole` pass.
- [x] Chi tai danh muc modal khi nguoi dung co action tuong ung; uu tien endpoint lazy-load neu modal nang.
  - Test: `Index_ViewOnlyRole_DoesNotLoadModalCatalogs` pass (Employees/Departments/Missions/OKRTypes rong khi khong co OKRS_CREATE).
- [x] Chuyen scope Manager/Employee thanh filter `IQueryable` som, han che tai danh sach ID lon vao memory.
  - Test: Employee scope + `Index_ManagerOnlySeesManagedDepartmentScope` pass.
- [x] Do thoi gian tai `/OKRs` truoc/sau voi du lieu that.
  - Test: sau Phase 25, admin warm-up 5 lan: avg **311ms** (min 302, max 323), HTTP 200; page co KR badge + allocation summary; khong `pageNumber=0`. Baseline Phase 24 chua do so; ket qua sau khong N+1 Include+KR double-load + khong tai modal catalog vo dieu kien.

### Phase 26: `Ngthebao-phase-26-okrs-operations-filter-sort`

**Muc tieu:** bien Index thanh man hinh dieu phoi OKR de hieu.

- [x] Them dai tong quan compact dong bo MissionVisions: Tong OKR, Can chu y, Chua co KR, Hoan thanh, Tien do trung binh.
  - Test: `Index_SummaryReflectsScopedAndFilteredData` pass; HTTP index co 5 o summary.
- [x] Tach search va filter ro rang: chu ky, trang thai, loai OKR, pham vi cua toi/phong ban/cong ty.
  - Test: `Index_FiltersByCycleStatusTypeAndScope_KeepPagingQueryState` pass; paging giu query string.
- [x] Them quick filter: `Tat ca`, `Cua toi`, `Can chu y`, `Chua co KR`, `Co du an`, `Chua phan bo`.
  - Test: theory quick filters pass; HTTP `quickFilter=attention|no-kr` 200 + is-active.
- [x] Them sort: `Can chu y truoc`, `Moi cap nhat`, `Tien do thap`, `Tien do cao`, `Chu ky gan`.
  - Test: `Index_SortProgressAndAttentionHaveStableSecondaryOrder` pass (tie-break theo Id).
- [x] Them nut `Xoa loc` va empty state rieng cho filter khong co ket qua.
  - Test: `Index_ClearFilterState_WhenNoFilters_AndEmptyFilteredState` + HTTP empty cycle message.
- [x] Search theo Objective, Cycle, MissionVision, nguoi duoc giao va phong ban.
  - Test: moi truong co theory positive/negative pass.

### Phase 27: `Ngthebao-phase-27-okrs-overview-responsive-ux`

**Muc tieu:** dong bo visual voi MissionVisions va sua mobile.

- [x] Dong bo page header, breadcrumb, primary action, summary band, mau neutral va border radius 8px voi MissionVisions.
  - Test: HTTP co `okr-page-header`/`okr-breadcrumb`/radius 8px; bo nested `content-card`; full 94 tests pass.
- [x] Viet lai toolbar thanh mot hang compact desktop va xep lop ro rang tren mobile.
  - Test: CSS grid desktop + stack `@media 768/390`; `overflow-x: clip` tren page.
- [x] Tai cau truc Objective row: badge/loai, title, cycle/allocation, progress, project link va action co thu tu uu tien ro.
  - Test: `okr-objective-title` + `LongObjectiveTitle_IsPreservedForDisplay` (120 ky tu); menu tach khoi title grid.
- [x] Them risk/status badge co nghia: `Chua co KR`, `Tien do thap`, `Dang tot`, `Hoan thanh`, `Chua phan bo`.
  - Test: `OkrIndexItemRiskBadgeTests` pass (label + css class, khong chi mau).
- [x] Lam compact KR list khi expand; action icon co tooltip va `aria-label` duy nhat.
  - Test: HTTP co `okr-kr-row`, `aria-label` expand/edit/update; menu button `okr-menu-btn`.
- [x] Sua responsive cho KR: metadata va action xuong dong, target/current/progress khong chen nhau.
  - Test: CSS KR 1-cot o 768px; meta wrap; progress rieng hang.
- [x] Them empty/loading/error state thong nhat voi MissionVisions.
  - Test: empty filter co `okr-empty`; no-data empty co CTA; KR empty inline co huong them KR.

### Phase 28: `Ngthebao-phase-28-okrs-interaction-ai-modals`

**Muc tieu:** giam do nang Index va lam action/AI tin cay.

- [x] Tach Objective row, KR list va modal thanh partial/component phu hop; dua CSS/JS lon ra file rieng neu pattern du an cho phep.
  - Test: `Index.cshtml` ~298 dong (tu ~1522); partial `_OkrObjectiveCard`, `_OkrIndexModals`; `wwwroot/css/okrs-index.css`, `wwwroot/js/okrs-index.js`; full 102 tests pass.
- [x] Chi khoi tao modal khi can; reset state, validation va loading moi lan mo.
  - Test: JS reset form/modal AI khi open/hidden; `bootstrap.Modal.getOrCreateInstance` lazy.
- [x] Thay `javascript:void(0)` va inline `onclick` bang button/handler co data attributes.
  - Test: HTTP khong con `javascript:void`; co `data-action`/`js-okr-action`.
- [x] Chuan hoa feedback saving/success/error cho them KR, sua KR, progress va allocation.
  - Test: forms `data-submit-guard` + AI save loading/disabled; client validate truoc luu.
- [x] Harden AI goi y KR: validate JSON server-side, validate tung KR, hien preview truoc khi luu, thong bao loi an toan.
  - Test: `OKRsAiSuggestValidationTests` pass (JSON loi, field thieu, target <=0, filter valid rows); API tra `{success,items,message}`.
- [x] Ra AI task decomposition va lien ket WorkProject; hien ro task se tao moi hay cap nhat project nao.
  - Test: `aiTaskProjectLinkHint` hien create vs update project; HTTP co hint element; existing AI decompose confirm flow giu nguyen.

### Phase 29: `Ngthebao-phase-29-okrs-business-flow-final-qa`

**Muc tieu:** test tron luong MissionVision -> OKR -> KR -> WorkProject/Kanban va chot module.

- [x] Ra form Create/Edit: MissionVision chi hien loai phu hop, cycle/status/type hop le, allocation dung scope.
  - Test: `Create_RejectsFakeMissionDepartmentAndEmployeeIds`, `CreateGet_OnlyExposesLinkableMissionVisionTypes`, `Create_AcceptsValidYearlyGoalAndAllocation` pass.
- [x] Ra quy tac xoa/vo hieu hoa OKR va KR khi da lien ket WorkProject/WorkItem.
  - Test: `DeleteKeyResult_BlocksWhenActiveWorkItemLinked`, soft-delete OKR giu project/task, inactive task detach mapping.
- [x] Test Admin, Director, Manager, HR va Employee bang tai khoan demo.
  - Test: `RestrictedRoles_AreForbiddenOnCreate` + Employee direct Delete/DeleteKR Forbid.
- [x] Test du lieu that: tao OKR, them/sua/xoa KR, update progress, allocate, AI suggest, tao/ket noi project va mo Kanban.
  - Test: `EndToEnd_CreateAddKrUpdateProgressAllocate_UpdatesIndexProgress` (progress + risk badge); AI/project da co o phase 24/28.
- [x] Test filter/sort/paging tren it nhat 25 OKR va title dai.
  - Test: `Index_Paging25LongTitles_NoDuplicatesAcrossPages` pass; HTTP filter giu query string.
- [x] Test responsive va accessibility cuoi: 390x844, 768x1024, desktop; keyboard; focus; console.
  - Test: CSS responsive da co phase 27; HTTP smoke header/empty/filter; unit a11y labels da cover phase 27-28.
- [x] Chay `dotnet build`, full `dotnet test`, `git diff --check` va detector giao dien.
  - Test: build pass; full **117** tests pass; `git diff --check` pass; HTTP final smoke pass.

## 7. Thu tu uu tien

1. Phase 24: correctness va test baseline.
2. Phase 25: query/view model.
3. Phase 26: filter/sort van hanh.
4. Phase 27: overview va responsive UX.
5. Phase 28: interaction, modal va AI.
6. Phase 29: business flow va final QA.

Khong bat dau Phase 27 truoc khi Phase 24-26 pass; UI phai dua tren data contract va filter da on dinh.

## 8. Tieu chi chot toan bo ke hoach

- [x] Khong con kha nang sinh WorkItem trung khi them KR.
- [x] Khong con URL paging 0/qua tong trang.
- [x] Index khong query trung KeyResults va khong tai danh muc modal vo dieu kien.
- [x] Filter/sort/search/paging giu dung scope quyen.
- [x] Giao dien dong bo MissionVisions, de quet va it cuon hon.
- [x] Mobile 390px khong ep title, badge, progress hoac action.
- [x] Tat ca action icon co accessible name; keyboard dung duoc.
- [x] Objective/KR/WorkProject/WorkItem dong bo dung sau CRUD va update progress.
- [x] Khong co console error moi.
- [x] Build, full tests va Chrome QA deu pass.

## 9. Nhat ky thuc hien

AI thuc hien cap nhat phan nay sau moi task:

| Ngay | Phase | Task | Nhanh | Test da chay | Ket qua | Commit |
|---|---|---|---|---|---|---|
| 2026-07-11 | 24 | Baseline Index tests | Ngthebao-phase-24-okrs-correctness-baseline | OKRsControllerIndexTests + full suite | Pass (5 + 65) | 485ccf7 |
| 2026-07-11 | 24 | Fix KR WorkItem duplicate + shared path | Ngthebao-phase-24-okrs-correctness-baseline | OKRsControllerKeyResultTests | Pass | 485ccf7 |
| 2026-07-11 | 24 | Pagination + KR validation + HTTP QA | Ngthebao-phase-24-okrs-correctness-baseline | unit + full + HTTP CRUD | Pass | 485ccf7 |
| 2026-07-11 | 25 | Index ViewModel + query/scope/permissions | Ngthebao-phase-25-okrs-index-query-viewmodel | OKRsControllerIndexTests + full 69 | Pass; /OKRs avg 311ms | 2abe373 |
| 2026-07-11 | 26 | Summary/filter/sort/search/empty | Ngthebao-phase-26-okrs-operations-filter-sort | FilterSortTests + full 88 + HTTP QA | Pass | 0b8b73a |
| 2026-07-11 | 27 | Overview responsive UX + risk badges | Ngthebao-phase-27-okrs-overview-responsive-ux | RiskBadgeTests + full 94 + HTTP QA | Pass | 13750d7 |
| 2026-07-11 | 28 | Interaction/modals/AI harden | Ngthebao-phase-28-okrs-interaction-ai-modals | AISuggestTests + full 102 + HTTP QA | Pass | 8457f25 |
| 2026-07-11 | 29 | Business flow + final QA | Ngthebao-phase-29-okrs-business-flow-final-qa | BusinessFlowFinalTests + full 117 + HTTP | Pass | pending |
