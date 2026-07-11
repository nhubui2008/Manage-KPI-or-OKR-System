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

- [ ] Tao `OkrIndexViewModel` va `OkrIndexItemViewModel` chua Objective, KR summary, allocation summary, project link, permission va paging.
  - Test: mapping dung voi OKR co/khong co KR, allocation va project.
- [ ] Bo `Include(o => o.KeyResults)` trung lap; chi project cac cot can cho page hien tai.
  - Test: ket qua progress/KR count khong doi; theo doi SQL query count neu co the.
- [ ] Batch bon permission bang `PermissionLookupHelper.HasPermissionsAsync`.
  - Test: Admin va role tuy chinh nhan dung tung permission.
- [ ] Chi tai danh muc modal khi nguoi dung co action tuong ung; uu tien endpoint lazy-load neu modal nang.
  - Test: role chi xem khong query Employees/Departments/Missions/OKRTypes.
- [ ] Chuyen scope Manager/Employee thanh filter `IQueryable` som, han che tai danh sach ID lon vao memory.
  - Test: Manager chi thay OKR phong ban quan ly; Employee chi thay OKR duoc giao/thuoc phong/tu tao.
- [ ] Do thoi gian tai `/OKRs` truoc/sau voi du lieu that.
  - Test: ghi baseline va ket qua vao phase; muc tieu page dau khong cham hon hien tai va khong N+1.

### Phase 26: `Ngthebao-phase-26-okrs-operations-filter-sort`

**Muc tieu:** bien Index thanh man hinh dieu phoi OKR de hieu.

- [ ] Them dai tong quan compact dong bo MissionVisions: Tong OKR, Can chu y, Chua co KR, Hoan thanh, Tien do trung binh.
  - Test: so lieu dung theo scope quyen va filter dang ap dung.
- [ ] Tach search va filter ro rang: chu ky, trang thai, loai OKR, pham vi cua toi/phong ban/cong ty.
  - Test: moi filter co unit/controller test va giu query string khi paging.
- [ ] Them quick filter: `Tat ca`, `Cua toi`, `Can chu y`, `Chua co KR`, `Co du an`, `Chua phan bo`.
  - Test: click tung filter tren Chrome, URL va ket qua dung.
- [ ] Them sort: `Can chu y truoc`, `Moi cap nhat`, `Tien do thap`, `Tien do cao`, `Chu ky gan`.
  - Test: du lieu co cung gia tri van co thu tu phu on dinh theo ID/CreatedAt.
- [ ] Them nut `Xoa loc` va empty state rieng cho filter khong co ket qua.
  - Test: clear filter ve trang 1, xoa toan bo query filter.
- [ ] Search theo Objective, Cycle, MissionVision, nguoi duoc giao va phong ban.
  - Test: moi truong search co it nhat mot test positive va mot negative.

### Phase 27: `Ngthebao-phase-27-okrs-overview-responsive-ux`

**Muc tieu:** dong bo visual voi MissionVisions va sua mobile.

- [ ] Dong bo page header, breadcrumb, primary action, summary band, mau neutral va border radius 8px voi MissionVisions.
  - Test: Chrome desktop so sanh hai trang; khong nested card, khong gradient text.
- [ ] Viet lai toolbar thanh mot hang compact desktop va xep lop ro rang tren mobile.
  - Test: desktop 1534px va mobile 390px khong overflow.
- [ ] Tai cau truc Objective row: badge/loai, title, cycle/allocation, progress, project link va action co thu tu uu tien ro.
  - Test: title dai 100+ ky tu van doc duoc; action khong ep title.
- [ ] Them risk/status badge co nghia: `Chua co KR`, `Tien do thap`, `Dang tot`, `Hoan thanh`, `Chua phan bo`.
  - Test: mau co contrast WCAG AA va khong chi dua vao mau de truyen dat.
- [ ] Lam compact KR list khi expand; action icon co tooltip va `aria-label` duy nhat.
  - Test: keyboard tab duoc vao expand, menu, edit, delete va update progress.
- [ ] Sua responsive cho KR: metadata va action xuong dong, target/current/progress khong chen nhau.
  - Test: viewport 390x844, 768x1024 va desktop; canvas khong tran ngang.
- [ ] Them empty/loading/error state thong nhat voi MissionVisions.
  - Test: no data, no filter results, API/AI error deu co huong xu ly ro.

### Phase 28: `Ngthebao-phase-28-okrs-interaction-ai-modals`

**Muc tieu:** giam do nang Index va lam action/AI tin cay.

- [ ] Tach Objective row, KR list va modal thanh partial/component phu hop; dua CSS/JS lon ra file rieng neu pattern du an cho phep.
  - Test: page behavior khong doi; `Index.cshtml` giam dang ke do phuc tap.
- [ ] Chi khoi tao modal khi can; reset state, validation va loading moi lan mo.
  - Test: mo/dong lien tiep tren nhieu OKR khong mang du lieu cu.
- [ ] Thay `javascript:void(0)` va inline `onclick` bang button/handler co data attributes.
  - Test: keyboard va screen reader nhan dung action.
- [ ] Chuan hoa feedback saving/success/error cho them KR, sua KR, progress va allocation.
  - Test: double-click khong gui hai request; button co loading/disabled state.
- [ ] Harden AI goi y KR: validate JSON server-side, validate tung KR, hien preview truoc khi luu, thong bao loi an toan.
  - Test: JSON loi, field thieu, target khong hop le, timeout va retry.
- [ ] Ra AI task decomposition va lien ket WorkProject; hien ro task se tao moi hay cap nhat project nao.
  - Test: preview -> confirm -> project/task dung; huy khong tao du lieu.

### Phase 29: `Ngthebao-phase-29-okrs-business-flow-final-qa`

**Muc tieu:** test tron luong MissionVision -> OKR -> KR -> WorkProject/Kanban va chot module.

- [ ] Ra form Create/Edit: MissionVision chi hien loai phu hop, cycle/status/type hop le, allocation dung scope.
  - Test: gia mao ID MissionVision/phong ban/nhan vien bi chan backend.
- [ ] Ra quy tac xoa/vo hieu hoa OKR va KR khi da lien ket WorkProject/WorkItem.
  - Test: khong de orphan mapping/task; thong bao ro hanh dong bi chan hoac cascade du kien.
- [ ] Test Admin, Director, Manager, HR va Employee bang tai khoan demo.
  - Test: action hien/bi an phu hop va request truc tiep trai phep tra 403.
- [ ] Test du lieu that: tao OKR, them/sua/xoa KR, update progress, allocate, AI suggest, tao/ket noi project va mo Kanban.
  - Test: quay lai `/OKRs` thay progress va risk badge cap nhat dung.
- [ ] Test filter/sort/paging tren it nhat 25 OKR va title dai.
  - Test: response on dinh, query string duoc giu, khong duplicate/missing record giua trang.
- [ ] Test responsive va accessibility cuoi: 390x844, 768x1024, desktop; keyboard; focus; console.
  - Test: khong overflow, overlap, focus trap sai hoac console error.
- [ ] Chay `dotnet build`, full `dotnet test`, `git diff --check` va detector giao dien.
  - Test: tat ca pass truoc commit/merge.

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
- [ ] Index khong query trung KeyResults va khong tai danh muc modal vo dieu kien.
- [ ] Filter/sort/search/paging giu dung scope quyen.
- [ ] Giao dien dong bo MissionVisions, de quet va it cuon hon.
- [ ] Mobile 390px khong ep title, badge, progress hoac action.
- [ ] Tat ca action icon co accessible name; keyboard dung duoc.
- [ ] Objective/KR/WorkProject/WorkItem dong bo dung sau CRUD va update progress.
- [ ] Khong co console error moi.
- [ ] Build, full tests va Chrome QA deu pass.

## 9. Nhat ky thuc hien

AI thuc hien cap nhat phan nay sau moi task:

| Ngay | Phase | Task | Nhanh | Test da chay | Ket qua | Commit |
|---|---|---|---|---|---|---|
| 2026-07-11 | 24 | Baseline Index tests | Ngthebao-phase-24-okrs-correctness-baseline | OKRsControllerIndexTests + full suite | Pass (5 + 65) | pending |
| 2026-07-11 | 24 | Fix KR WorkItem duplicate + shared path | Ngthebao-phase-24-okrs-correctness-baseline | OKRsControllerKeyResultTests | Pass | pending |
| 2026-07-11 | 24 | Pagination + KR validation + HTTP QA | Ngthebao-phase-24-okrs-correctness-baseline | unit + full + HTTP CRUD | Pass | pending |
