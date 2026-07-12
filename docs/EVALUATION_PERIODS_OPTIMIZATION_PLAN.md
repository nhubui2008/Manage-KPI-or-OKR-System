# Ke hoach toi uu module EvaluationPeriods

## 1. Muc tieu

Bien `/EvaluationPeriods` thanh man hinh dieu phoi ky danh gia nhanh, de quet va an toan cho cac luong KPI, KPI Check-in va EvaluationResult. Giao dien ke thua ngon ngu cua `/OKRs` va `/MissionVisions`, nhung uu tien thong tin van hanh cua mot ky: khoang ngay, trang thai theo du lieu, trang thai theo thoi gian, so KPI, so ket qua danh gia va cac hanh dong hop le.

Ket qua mong doi:

- Nguoi dung nhin vao biet ngay ky nao dang dien ra, sap bat dau, sap ket thuc, da ket thuc nhung chua dong, hoac da dong.
- Search, filter, sort va paging duoc thuc hien tren database, giu nguyen query state va khong tai toan bo du lieu vao memory.
- Create/Edit/Close/Delete khong lam dut lien ket KPI, check-in hoac ket qua danh gia.
- Moi action thay doi du lieu co permission, POST va antiforgery; action icon va modal co accessible name.
- Desktop, 768x1024 va 390x844 de doc, khong bat nguoi dung keo ngang de thay trang thai/hanh dong.

## 2. Quy tac thuc hien

1. Moi phase phai tao nhanh moi truoc khi sua code; ten nhanh bat dau bang `Ngthebao-phase-...`.
2. Dung CodeGraph truoc khi grep hoac doc source code.
3. Sua file thu cong bang `apply_patch`.
4. Khong revert thay doi khong thuoc task; khong commit `qa-http-okrs-feedback.json`.
5. Lam task theo dung thu tu trong tung phase.
6. **Phai hoan thanh va test task hien tai truoc khi lam task tiep theo.** Chi doi `[ ]` thanh `[x]` khi dong `Test:` cua task da pass.
7. Neu test fail, giu task o `[ ]`, sua va test lai; khong mo rong pham vi de ne loi.
8. Sau moi thay doi backend phai build/test lien quan; sau moi thay doi UI phai reload server va QA Chrome that.
9. Chrome QA dung tab/profile `testchormecodex` qua Chrome extension; khong dung `Profile 9`, khong tao profile moi.
10. Chi commit file EvaluationPeriods va phan tich hop that su can thiet. Khong sua KPI/OKR core neu khong co test hoi quy.

## 3. Definition of Done cho moi task

Mot task chi duoc danh dau `[x]` khi:

- Logic dung pham vi va khong co abstraction/dependency du thua.
- Co test tu dong cho rule/query moi, hoac ghi ro vi sao task chi can structure/Chrome QA.
- Build va test lien quan pass.
- UI task da kiem tra desktop; responsive task da kiem tra them 768x1024 va 390x844.
- Khong co console error/warning moi, duplicate ID, horizontal overflow cap trang, text/action de len nhau.
- Ket qua duoc ghi ngay duoi task theo mau `Test: ...`.

## 4. Baseline Phase 30

### Git va test

- Nhanh baseline: `Ngthebao-phase-30-evaluation-periods-audit-plan`.
- `main`: `e36d37f` (`Merge OKR feedback hardening`).
- Worktree luc bat dau chi co artifact local `qa-http-okrs-feedback.json`; file nay khong nam trong pham vi commit.
- `dotnet build --no-restore`: pass, 0 warning, 0 error.
- Test project dung VSTest + xUnit v2; solution file hien khong include test project.
- `dotnet test tests/ManageKpiOkrSystem.Tests/ManageKpiOkrSystem.Tests.csproj --no-build`: 124/124 pass.
- `dotnet ef migrations has-pending-model-changes --no-build`: khong co pending model change; co 2 warning decimal precision san co o `PaymentTransaction.Amount` va `SaaSPackage.PricePerMonth`, ngoai pham vi.
- `git diff --check`: pass.

### Backend va nghiep vu

- `EvaluationPeriodsController.Index` tai toan bo period active bang entity tracking, khong projection, khong paging/filter/search; ba permission duoc lookup rieng le.
- Index dung `ViewBag` cho status, permission va KPI count; logic normalize type va status bi lap lai trong controller, Razor va JavaScript.
- Trang thai du lieu chi co `Mo`, `Dong`, `Dang xu ly`; Razor lai so sanh `Active`, `In Progress`, `Closed`, `Completed`, nen ca `Mo` va `Dong` deu roi vao badge pending cung mau.
- Du lieu that co 3 ky: `Quy 2/2026` (90 KPI, da het 30/06/2026 nhung van `Mo`), `Nam 2026` (0 KPI, dang trong khoang ngay va `Mo`), `Quy 1/2026` (0 KPI, `Dong`).
- Delete chi soft-disable period, khong canh bao/chan khi co KPI hoac EvaluationResult lien ket. Edit cho doi ngay/status cua ky da co du lieu phu thuoc.
- Validation hien co: bat buoc field, trung ten active, end >= start, month <= 32 ngay, quarter >= 80 ngay, overlap cung type. Chua validate period type whitelist, status hop le, year duration, thay doi ky co du lieu, close/reopen lifecycle.
- KPI va EvaluationResult cho chon moi period `IsActive`; khong loc theo trang thai hoac khoang ngay.
- KPI Check-in chi chan theo trang thai KPI; khong chan period da dong/het han. Tong diem check-in duoc tinh theo tat ca KPI cung `PeriodId`, nen lifecycle period la core dependency.
- EvaluationResult chan duplicate `(EmployeeId, PeriodId)` nhung khong validate period ton tai/mo/dung thoi gian.
- Controller co `[Authorize]` va `[HasPermission]`; form POST duoc Razor sinh antiforgery token. Can bo sung `[ValidateAntiForgeryToken]` hoac xac nhan global antiforgery policy de contract server ro rang.
- Chua co test rieng cho `EvaluationPeriodsController`, `EvaluationPeriod` lifecycle, hay lien ket period voi KPI/Check-in/EvaluationResult.

### Hieu nang va Chrome QA

- Authenticated desktop warm reload `/EvaluationPeriods`: khoang 143 ms voi 3 period; navigation lan dau sau login 472 ms gom redirect/login.
- Trang chua co filter nen khong co baseline filter hien tai de do. Phase 31 phai tao dataset du va benchmark tung filter.
- `/OKRs`: khoang 1768 ms; `/MissionVisions`: 520 ms; `/WorkProjects`: 1699 ms trong cung phien Chrome. Cac so nay chi la Chrome wall-clock tham khao, khong dung de ket luan query rieng le.
- Desktop 1534x880: khong overflow, khong duplicate ID, khong console warning/error; layout qua trong, khong summary/filter va status khong phan biet.
- 768x1024: table container 642 px nhung table 678 px, van can scroll ngang.
- 390x844: table container 272 px nhung table 678 px; chi thay khoang 3 cot, status/action nam ngoai man hinh neu khong keo ngang.
- Action edit/delete chi co accessible name la glyph; khong neu hanh dong va ten ky. Modal edit thieu `aria-labelledby`; delete dung native `confirm`.
- Create page khong overflow cap trang sau khi animation ket thuc, nhung nut action bi xuong 3 dong tai 390 px, section title bi canh tranh voi AI floating button, preview/help lam trang dai. CDN animate.css va nhieu inline style khong dong bo pattern OKRs.

## 5. Lo trinh thuc hien

### Phase 30: Audit va lap baseline

Nhanh: `Ngthebao-phase-30-evaluation-periods-audit-plan`

**Muc tieu:** tao baseline co the lap lai, xac dinh blast radius va chot ke hoach truoc khi thay doi behavior.

- [x] Kiem tra `git status`, nhanh, commit `main` va tao nhanh Phase 30 truoc moi chinh sua.
  - Test: `main=e36d37f`; nhanh hien tai dung ten; artifact local duoc giu nguyen.
- [x] Dung CodeGraph ra controller, model, view, helper permission/status va lien ket KPI/Check-in/EvaluationResult.
  - Test: da truy vet `EvaluationPeriodsController`, `EvaluationPeriod`, hai Razor view, `WorkflowStatusHelper`, `PermissionLookupHelper`, `KPIsController`, `KPICheckInsController.Create`, `EvaluationResultsController`.
- [x] Ra test hien co va chay baseline build/full test/migration/diff.
  - Test: build pass; 124/124 test pass; no pending model changes; `git diff --check` pass.
- [x] Ra du lieu that, status va do thoi gian tai tren Chrome hien tai.
  - Test: 3 period, 90 KPI lien ket; authenticated reload 143 ms; console sach.
- [x] So sanh desktop voi `/OKRs`, `/MissionVisions`, `/WorkProjects`.
  - Test: da chup va kiem tra structure/summary/filter/card cua ca ba trang trong cung phien Chrome.
- [x] QA responsive 390x844, 768x1024 va desktop cho Index/Create.
  - Test: xac nhan table scroll ngang noi bo 678/272 px o 390; Create khong overflow cap trang sau animation nhung action wrap kem.
- [x] Tao ke hoach chi tiet va ghi ro file/rui ro/tieu chi nghiem thu.
  - Test: file nay co checklist, dong `Test:`, rule sequencing, impact map, risk register va acceptance criteria.

File tac dong Phase 30:

- `docs/EVALUATION_PERIODS_OPTIMIZATION_PLAN.md`

Tieu chi nghiem thu Phase 30:

- Baseline co the lap lai; khong thay doi behavior ung dung; khong commit artifact ngoai pham vi.

### Phase 31: Overview, filter va hieu nang

Nhanh: `Ngthebao-phase-31-evaluation-periods-overview-filter`

**Muc tieu:** tao data contract typed, query database hieu qua va man hinh tong quan co filter van hanh.

- [x] Tao `EvaluationPeriodIndexViewModel`, item/summary/filter option typed; gom permission vao model.
  - Test: 10 controller test xac nhan mapping date/status/count/permission; build pass.
- [x] Batch `EVALPERIODS_CREATE/EDIT/DELETE` bang `HasPermissionsAsync`; dung `AsNoTracking` va projection cac cot can thiet.
  - Test: Admin/custom role mapping dung; query khong tracking entity; controller test pass.
- [x] Tao mot scoped `IQueryable` cho period active; ap search/filter/sort truoc `Count/Skip/Take`.
  - Test: search; filter year/type/configured status/operational status; stable sort, paging va page clamp pass.
- [x] Them summary theo tap du lieu scoped: tong, dang dien ra, sap bat dau, sap ket thuc, da dong/da ket thuc.
  - Test: fixed-date dataset kiem tra boundary bat dau/ket thuc va nguong sap toi.
- [x] Them filter can thiet: search, nam, loai ky, trang thai cau hinh, quick filter van hanh; sort gan nhat/sap bat dau/sap ket thuc/moi nhat.
  - Test: query string duoc giu khi paging; clear filter va filtered-empty state dung.
- [x] Projection KPI count va EvaluationResult count theo period, khong N+1; chi query ID cua page hien tai neu can.
  - Test: count mapping dung voi period co/khong co lien ket; aggregate query chay thanh cong tren SQL Server.
- [x] Viet lai header/summary/filter theo ngon ngu visual cua OKRs nhung dung label ky danh gia.
  - Test: Chrome desktop co summary/filter ro; khong console error; empty state co CTA/xoa loc.
- [x] Kiem thu paging/filter bang dataset tu dong 12 period va benchmark tren 3 period that hien co, khong ghi them du lieu vao database QA dung chung.
  - Test: 30 request co xac thuc deu HTTP 200; min/avg/max cua moi filter deu duoi 1 giay, avg 90-477 ms.

File du kien Phase 31:

- `Controllers/EvaluationPeriodsController.cs`
- `Models/ViewModels/EvaluationPeriodIndexViewModels.cs` (moi)
- `Views/EvaluationPeriods/Index.cshtml`
- `wwwroot/css/evaluation-periods.css` (neu style vuot qua phan Razor nho)
- `tests/ManageKpiOkrSystem.Tests/EvaluationPeriodsControllerIndexTests.cs` (moi)

Tieu chi nghiem thu Phase 31:

- Filter/paging khong tai toan bo period vao memory; khong N+1; moi filter warm < 1 giay tren dataset QA; summary va operational status dung boundary.

### Phase 32: Business flow va CRUD

Nhanh: `Ngthebao-phase-32-evaluation-periods-business-flow`

**Muc tieu:** dua lifecycle/validation ve mot duong dung chung, bao ve du lieu phu thuoc va chot quyen action.

- [x] Tao input view model cho Create/Edit, whitelist `MONTH/QUARTER/YEAR`, validate required/length/date/status server-side va hien loi tai form thay vi redirect mat input.
  - Test: invalid duration tra ve dung view/field error; khong luu du lieu; Create/Edit GET HTTP 200.
- [x] Gom normalize va validation create/edit vao mot helper nho co the test; quy dinh duration cho month/quarter/year va overlap cung loai.
  - Test: boundary month 28-31/32, quarter 89-92/93, year 365-366/364; overlap inclusive va edit exclude self.
- [x] Xac dinh lifecycle `Mo -> Dang xu ly -> Dong` va danh sach action hop le; khong bind `StatusId` tu Create/Edit.
  - Test: allowed/forbidden transition theory; lifecycle start/close/reopen; reflection permission test.
- [x] Bao ve Edit khi period da co KPI/check-in/result: cho sua ten an toan; chan doi type/date lam du lieu roi ngoai ky.
  - Test: linked period khong doi khoang ngay/type; ten hop le van sua duoc.
- [x] Bao ve Close: chan KPI chua final, check-in pending hoac EvaluationResult chua approved; thong bao ro so luong blocker.
  - Test: blocker tong hop va happy path lifecycle; status khong doi khi bi chan.
- [x] Bao ve Delete/soft-disable: khong vo hieu hoa period dang duoc KPI/check-in/EvaluationResult tham chieu; phan biet not-found va dependency conflict.
  - Test: linked period van active; unlinked period soft-disable va ghi audit.
- [x] Bo sung POST + `[ValidateAntiForgeryToken]` cho Create/Edit/Delete va action lifecycle; giu `[HasPermission]` tai moi endpoint.
  - Test: reflection test ca ba attribute tren 6 action state-changing.
- [x] Harden tich hop KPI/EvaluationResult: chi cho chon period `Mo`/`Dang xu ly` hop le va validate `PeriodId` server-side.
  - Test: SQL Server GET chi hien `Nam 2026`, loai hai quy dong/qua han; full test core pass.
- [x] Them check period lifecycle vao KPI Check-in: chi period `Mo`/`Dang xu ly` trong khoang ngay cho phep.
  - Test: helper current/closed/unknown/expired; Check-in Create HTTP 200 va full regression pass.
- [x] Chuan hoa audit log/TempData cho create/edit/start/close/reopen/delete theo pattern san co.
  - Test: create/delete co audit; blocker va success message dung, payload chi gom du lieu ky.

File du kien Phase 32:

- `Controllers/EvaluationPeriodsController.cs`
- `Models/ViewModels/EvaluationPeriodFormViewModel.cs` (moi neu can)
- `Services/EvaluationPeriodService.cs` hoac helper nho (chi tao neu controller testability can)
- `Views/EvaluationPeriods/Create.cshtml`
- `Views/EvaluationPeriods/Index.cshtml`
- `Controllers/KPIsController.cs` (chi validation/option period can thiet)
- `Controllers/KPICheckInsController.cs` (chi neu rule lifecycle duoc test va chap nhan)
- `Controllers/EvaluationResultsController.cs` (chi validation/option period can thiet)
- `tests/ManageKpiOkrSystem.Tests/EvaluationPeriodsBusinessFlowTests.cs` (moi)
- Test hoi quy KPI Check-in/EvaluationResult lien quan

Tieu chi nghiem thu Phase 32:

- Khong co transition hoac delete lam mat kha nang truy vet KPI/check-in/result; tat ca action state-changing dung permission, POST, antiforgery; core KPI/OKR test pass.

### Phase 33: UI/UX va final QA

Nhanh: `Ngthebao-phase-33-evaluation-periods-final-qa`

**Muc tieu:** hoan thien visual, responsive, accessibility va test tron luong that.

- [x] Tai cau truc danh sach: table compact desktop va card/stacked row mobile; uu tien ten, operational badge, ngay, KPI/result count va action.
  - Test: 1519 desktop, 768x1024, 390x844; table an/card hien <=900 px; khong overflow hoac cat action.
- [x] Chuan hoa badge co text + mau: dang dien ra, sap bat dau, sap ket thuc, qua han chua dong, da dong.
  - Test: boundary unit mapping va Chrome DOM; badge co text, cham status va contrast rieng.
- [x] Thay native confirm bang data attribute va modal xac nhan co ten ky, dependency summary va focus hop le.
  - Test: open/cancel modal khong submit; co `aria-labelledby`/`aria-describedby`; khong duplicate ID.
- [x] Them `aria-label` duy nhat cho edit/start/close/reopen/delete, gom ten period; focus ring ro.
  - Test: desktop DOM audit 18/18 action co accessible name duy nhat.
- [x] Don Create/Edit UI: bo CDN/inline style khong can, dung asset noi bo, giu preview va native select; action stack o 390 px.
  - Test: preview cap nhat; Create/Edit khong overflow; Select2 wrapper = 0; console sach.
- [x] QA luong that: create, edit, start, close, reopen, delete theo rule va cleanup an toan.
  - Test: SQL period `Id=4` inactive sau flow; audit co du CREATE/STATUS_CHANGE/UPDATE/CLOSE/REOPEN/DELETE.
- [x] QA role Admin, Director, Manager, HR va Employee voi permission matrix.
  - Test: 5 role Index HTTP 200; action visibility khop endpoint; Employee Create/Edit chuyen AccessDenied.
- [x] Chay final verification.
  - Test: build; 163 tests; no pending model changes; diff check; Chrome desktop/768/390 va console sach.

File du kien Phase 33:

- `Views/EvaluationPeriods/Index.cshtml`
- `Views/EvaluationPeriods/Create.cshtml`
- Partial Razor neu giup giam modal/card lap lai
- `wwwroot/css/evaluation-periods.css`
- `wwwroot/js/evaluation-periods.js`
- Test accessibility/view model/business flow lien quan

Tieu chi nghiem thu Phase 33:

- Nguoi dung thay duoc operational status va action ma khong scroll ngang; full flow va permission matrix pass; build/test/migration/diff/Chrome QA sach.

## 6. Risk register

| Rui ro | Muc do | Bien phap |
|---|---|---|
| Dong/disable period lam KPI khong con period de tinh check-in/score | Cao | Dependency query + blocker + transaction + test hoi quy KPI Check-in |
| Doi ngay/type sau khi co KPI/check-in lam sai lich su | Cao | Lock field hoac validate tat ca dependency truoc save |
| Operational status theo `DateTime.Now` lam test khong on dinh | Trung binh | Truyen `today` vao resolver/helper, test fixed date; dung local business date nhat quan |
| Filter summary va list dung khac scope | Cao | Xuat phat tu cung mot filtered `IQueryable`; test summary/list cung dataset |
| Count KPI/result sinh N+1 | Trung binh | Group/projection trong SQL hoac batch current page IDs |
| Status seed tieng Viet khong khop hardcode tieng Anh | Cao | Resolver dung constants/ID theo StatusType va operational status rieng |
| HR co default VIEW nhung khong CREATE/EDIT/DELETE | Trung binh | Batch permission va test matrix, khong suy dien quyen tu role tai Razor |
| Mobile table chi an overflow chu khong de dung | Cao | Card/stacked row that su tai <= 767 px; Chrome QA 390/768 |
| Sua integration lam vo test OKR/KPI core | Cao | Diff toi thieu, test muc tieu + full suite sau moi task integration |

## 7. Acceptance criteria toan bo

- [x] `/EvaluationPeriods` warm load/filter khong co do tre 2-3 giay; filter QA < 1 giay va query duoc thuc hien tren database.
- [x] Summary/list cung scope va phan loai dung ky dang chay, sap bat dau, sap ket thuc, qua han, da dong.
- [x] Filter/search/sort/paging giu query state va co clear/empty state ro.
- [x] Create/Edit/Close/Reopen/Delete tuan thu lifecycle, dependency va permission.
- [x] KPI, KPI Check-in, EvaluationResult khong nhan period gia/khong hop le; core KPI/OKR khong regression.
- [x] Desktop, 768x1024 va 390x844 khong overflow cap trang; mobile khong can scroll ngang de thay status/action.
- [x] Moi action va modal co accessible name; khong duplicate ID; keyboard/focus dung.
- [x] Khong co console error/warning moi.
- [x] `dotnet build`, full test project, pending-model check va `git diff --check` pass.
- [x] Chi file trong pham vi duoc commit; `qa-http-okrs-feedback.json` khong duoc commit/push.

## 8. Nhat ky thuc hien

| Ngay | Phase | Task | Nhanh | Test da chay | Ket qua | Commit |
|---|---|---|---|---|---|---|
| 2026-07-11 | 30 | Git/CodeGraph/backend/data/UI audit va baseline plan | `Ngthebao-phase-30-evaluation-periods-audit-plan` | build, 124 tests, EF pending check, diff check, Chrome desktop/768/390 | Pass; da ghi baseline va risk | `a3eb3f4` |
| 2026-07-12 | 31 | Typed overview, database filter/sort/paging, summary, responsive list va performance | `Ngthebao-phase-31-evaluation-periods-overview-filter` | build, 134 tests, EF pending check, diff check, Chrome desktop/768/390, 30 authenticated HTTP samples | Pass; avg filter 90-477 ms, khong EF/runtime error | `c069632` |
| 2026-07-12 | 32 | Typed CRUD, lifecycle, dependency guards va KPI/EvaluationResult/Check-in integration | `Ngthebao-phase-32-evaluation-periods-business-flow` | build, 163 tests, 6 authenticated SQL Server GET flows, diff check | Pass; lifecycle va integration whitelist hoat dong, khong runtime error | `e1fbf64` |
| 2026-07-12 | 33 | Modal xac nhan, Create/Edit polish, responsive/accessibility, role matrix va full-flow QA | `Ngthebao-phase-33-evaluation-periods-final-qa` | build, 163 tests, EF/diff check, Chrome desktop/768/390, 5 role, SQL CRUD/lifecycle | Pass; console sach, QA period da soft-disable | Commit Phase 33 |
