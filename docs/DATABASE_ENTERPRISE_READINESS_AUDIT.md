# Báo cáo rà soát Database và luồng nghiệp vụ

Ngày đánh giá: 12/08/2026
Phạm vi: toàn bộ mô hình EF Core, migration, truy vấn trọng yếu, ràng buộc tenant/RBAC, luồng OKR–KPI–đánh giá–thưởng và kiểm tra runtime an toàn trên database hiện có.

> Báo cáo không chứa connection string, mật khẩu hoặc giá trị từ file `.env`. Không reset, reseed, migrate hay xóa dữ liệu trong quá trình đánh giá.

> Tài liệu dùng nội bộ vì có fingerprint vận hành (phiên bản SQL Server, quy mô bảng và tên schema). Cần lược bỏ các chi tiết này trước khi phát hành ra ngoài tổ chức.

## 1. Kết luận điều hành

Hệ thống có nền tảng ứng dụng khá tốt để tiếp tục nâng lên môi trường doanh nghiệp: dữ liệu được cô lập theo tenant, có global query filter, kiểm tra tenant khi ghi, SQL row-level security, authorization/RBAC, audit và các unique/index quan trọng. Không phát hiện lỗi P0 gây mất dữ liệu hoặc lộ dữ liệu tenant trong phạm vi khảo sát.

Hệ thống **chưa nên được coi là hoàn tất chuẩn production doanh nghiệp** cho đến khi xử lý phần vận hành SQL Server: instance hiện là SQL Server 2019 RTM cũ, Query Store đang tắt, tài khoản kiểm tra không có quyền đọc DMV theo dõi hiệu năng, và chưa có số liệu tải thực tế 7–14 ngày. Database hiện nhỏ nên kết quả kiểm tra chứng minh tính đúng đắn và cấu trúc, chưa thay thế load test.

Các tối ưu an toàn đã được triển khai trong mã nguồn:

- Bỏ truy vấn `Count + 1` khi sinh mã dự án; dùng mã không cần round-trip database và khó va chạm.
- Gộp kiểm tra WorkItem của nhiều Key Result thành một truy vấn thay vì N+1 truy vấn.
- Tuần tự hóa workflow tự động theo từng OKR bằng transaction và row lock trên SQL Server, ngăn hai request đồng thời tạo trùng project/task nhưng vẫn giữ quan hệ one-to-many của luồng thủ công.
- Bổ sung `AsNoTracking()` cho nhánh đọc cây OKR.
- Bổ sung unit test và SQL Server integration test cho tính duy nhất/độ dài mã, idempotency, số truy vấn WorkItem và hai `DbContext` chạy đồng thời.

## 2. Dấu vết runtime đã kiểm tra

| Hạng mục | Kết quả tại thời điểm kiểm tra | Ý nghĩa |
|---|---:|---|
| Database / schema nghiệp vụ | `manasys` / `biz` | Khớp cấu hình multi-schema hiện tại |
| SQL Server | Express 2019 RTM, build `15.0.2000.5` | Cần lập kế hoạch vá/nâng cấp có backup và rehearsal |
| Compatibility level | `150` | Phù hợp SQL Server 2019 |
| Recovery model | `SIMPLE` | Cần xác nhận RPO/RTO; không tự đổi trong lần rà soát này |
| Query Store | OFF | Chưa có lịch sử plan/regression để tối ưu dựa trên bằng chứng |
| Read committed snapshot | OFF | Không phải lỗi mặc định; chỉ cân nhắc sau kiểm thử tranh chấp khóa |
| Page verify | CHECKSUM | Cấu hình tốt cho phát hiện hỏng trang dữ liệu |
| Heap | 0 | Các bảng khảo sát đều có clustered/index phù hợp |
| Foreign key chưa có index hỗ trợ | 0 | Không phát hiện thiếu index FK trực tiếp |
| Migration đã áp dụng | 45 | Migration history nhất quán tại thời điểm kiểm tra |
| Duplicate WorkItem active theo tenant/KR | 0 | Không thấy bản ghi tự động trùng trong dữ liệu hiện tại |
| Duplicate WorkProject active theo tenant/SourceOKR | 0 | Dữ liệu hiện tại sạch; quan hệ vẫn được giữ one-to-many hợp lệ |

Những bảng có nhiều dữ liệu nhất vẫn ở quy mô nhỏ: `SystemAlerts` 488 dòng, `KPI_Employee_Assignments` 480 dòng, `KPICheckIns`/chi tiết khoảng 269 dòng, lịch sử khoảng 268 dòng, nhóm nhân sự khoảng 240 dòng, `OKRKeyResults` 109 dòng, `KPIs`/`KPIDetails` 84 dòng, `OKRs` 37 dòng và `EvaluationPeriods` 3 dòng. `EvaluationResults` chưa có dữ liệu tại thời điểm kiểm tra.

Do tài khoản ứng dụng không có `VIEW SERVER STATE`, báo cáo không khẳng định index nào “không dùng” hoặc đề xuất index theo missing-index DMV. Đây là giới hạn có chủ đích của quyền tối thiểu, nhưng môi trường vận hành nên có một tài khoản monitoring chỉ đọc riêng.

### 2.1 Kiểm tra ứng dụng bằng Chrome Profile 9

Ứng dụng được chạy tại `http://127.0.0.1:5208` bằng biến môi trường chỉ nạp vào process; không sao chép hoặc sửa file `.env` trong repository. QA dùng đúng Chrome Profile 9 (`testchormecodex`) và chỉ thực hiện thao tác đọc, không submit form hoặc tạo/sửa/xóa dữ liệu thật.

- Các route `/Dashboard/Index`, `/OKRs`, `/WorkProjects`, `/KPIs`, `/KPICheckIns`, `/EvaluationPeriods`, `/EvaluationResults`, `/EvaluationResults/ReviewBoard` và `/BonusRules` đều render đúng trang, không xuất hiện trang lỗi hoặc exception phía trình duyệt.
- Các route tạo mới `/OKRs/Create`, `/WorkProjects/Create`, `/KPIs/Create`, `/EvaluationPeriods/Create` và `/EvaluationResults/Create` đều render form và có antiforgery token; không gửi form trong lần kiểm tra này.
- Console không ghi nhận warning/error của ứng dụng trên các route được khảo sát.
- Ở kích thước `1366x768` và `390x844`, các trang `/OKRs`, `/WorkProjects`, `/KPIs` và `/EvaluationPeriods` không tràn ngang.
- Dashboard quản trị hiển thị dữ liệu thật gồm 240 nhân viên, 37 OKR, 84 KPI, 268 check-in đã duyệt và tiến độ OKR trung bình 79,7%; các bảng check-in gần đây và nhân sự nổi bật cũng có dữ liệu. Tài khoản kiểm tra thuộc role `Admin` nhưng trang hồ sơ báo **chưa liên kết hồ sơ nhân viên**, nên một số vùng dữ liệu theo cá nhân có thể rỗng dù dữ liệu tổng hợp theo tenant vẫn hiển thị. Đây là khác biệt phạm vi truy cập hợp lệ, không phải database rỗng.

Nợ trải nghiệm/accessibility phát hiện trong QA: ô tìm kiếm của `/KPIs`, `/EvaluationPeriods` và `/WorkProjects` đang dựa vào placeholder mà chưa có label/`aria-label` rõ ràng; `/WorkProjects` chưa có heading `h1`. Các điểm này không làm sai nghiệp vụ database nhưng nên được xử lý trong đợt chuẩn hóa giao diện tiếp theo.

## 3. Bản đồ miền dữ liệu và luồng nghiệp vụ

### 3.1 Nền tảng tenant, tổ chức và bảo mật

- Tenant/SaaS xác định phạm vi dữ liệu; đơn vị, phòng ban, vị trí và nhân viên tạo cây tổ chức.
- Identity, role, permission và policy kiểm soát quyền truy cập controller/action.
- EF Core global query filters tự giới hạn tenant và bản ghi bị soft-delete.
- Guard trong `SaveChanges` ngăn ghi chéo tenant; SQL session context/RLS tạo thêm lớp bảo vệ ở database.
- Audit, alert và notification giữ dấu vết các thay đổi/nghiệp vụ quan trọng.

### 3.2 Chiến lược đến thực thi OKR

1. Mission/Vision và các mục tiêu chiến lược tạo định hướng.
2. OKR được tạo, phân cấp và gắn Mission; Key Result định lượng kết quả.
3. Khi OKR/KR đủ điều kiện, workflow tạo hoặc đồng bộ WorkProject và WorkItem.
4. WorkProject có thể chứa nhiều task cho cùng một Key Result trong luồng AI decomposition; vì vậy không được ép unique `(TenantId, OKRKeyResultId)` trên toàn bảng WorkItem.
5. Một OKR nguồn có thể sinh nhiều project hợp lệ; vì vậy không được ép unique `(TenantId, SourceOKRId)`.

### 3.3 KPI, check-in và đánh giá

1. KPI/KPI detail định nghĩa chỉ số, trọng số, đơn vị và chu kỳ.
2. Assignment gắn KPI cho nhân viên/đơn vị trong phạm vi tenant.
3. Người dùng check-in; chi tiết, bằng chứng và lịch sử lưu tiến độ thực tế.
4. Submission/review status điều phối duyệt; AI review là hỗ trợ, không thay thế authorization hoặc quyết định nghiệp vụ.
5. EvaluationPeriod khóa phạm vi thời gian; EvaluationResult tổng hợp kết quả theo nhân viên và kỳ.
6. BonusRule/Reward sử dụng kết quả đã duyệt để tính thưởng theo quy tắc hiện hành.

### 3.4 AI/RAG và xử lý nền

- AI decomposition tạo đề xuất task từ mục tiêu/KR nhưng vẫn ghi qua service và tenant guard.
- RAG/chunk/vector/outbox hỗ trợ truy xuất và xử lý nền.
- Outbox giúp tách thao tác nền khỏi request; cần tiếp tục theo dõi retry, idempotency và dead-letter trong môi trường vận hành.

## 4. Kiểm tra thiết kế dữ liệu

### Điểm đạt

- Có tenant key/filter và soft-delete nhất quán trên các aggregate chính.
- Có index cho toàn bộ foreign key được kiểm tra; không có heap.
- Có unique constraint cho các khóa nghiệp vụ phù hợp, ví dụ `(TenantId, ProjectCode)`, `(TenantId, SubmissionId)` và `(TenantId, EmployeeId, PeriodId)` của kết quả đánh giá.
- Migration có lịch sử rõ ràng; runtime đang ở migration mới nhất tại thời điểm khảo sát.
- Dữ liệu hiện tại không có duplicate active ở hai quan hệ tự động hóa trọng yếu.

### Rủi ro còn lại

| Mức | Rủi ro | Ảnh hưởng | Hướng xử lý |
|---|---|---|---|
| P1 | SQL Server 2019 đang ở RTM cũ | Thiếu nhiều bản sửa lỗi/bảo mật/tối ưu tích lũy | Backup, restore rehearsal, kiểm tra compatibility rồi vá lên CU/GDR được phê duyệt |
| P1 | Query Store tắt | Không có bằng chứng plan regression/top query theo thời gian | Bật có kiểm soát ở staging trước, cấu hình quota/capture, theo dõi rồi mới production |
| P1 | Chưa có monitoring DMV | Không đo được wait, top plans và index usage bằng tài khoản hiện tại | Cấp quyền chỉ đọc cho tài khoản monitoring riêng; không mở rộng quyền tài khoản ứng dụng |
| P1 | Tạo project sau khi lưu OKR là best-effort | Lỗi giữa hai bước có thể cần retry/đối soát | Đưa automation vào outbox/idempotent worker ở một thay đổi nghiệp vụ riêng |
| P2 | RCSI đang OFF | Dưới tải cao có thể xuất hiện reader/writer blocking | Thu wait stats và load test trước; không bật chỉ dựa trên phỏng đoán |
| P2 | Statistics có tỷ lệ modification tương đối cao trên vài bảng nhỏ | Có thể ảnh hưởng plan khi dữ liệu tăng | Theo dõi Query Store/stat age; cập nhật theo bằng chứng và lịch bảo trì |
| P2 | Dữ liệu hiện tại nhỏ | Chưa chứng minh hiệu năng ở tải doanh nghiệp | Tạo staging ẩn danh gần production và chạy load/concurrency test |

## 5. Tối ưu đã triển khai

### 5.1 Sinh mã dự án không truy vấn database

Trước đây mã dùng số lượng project hiện tại (`Count + 1`). Cách này tạo thêm round-trip và hai request đồng thời có thể sinh cùng mã. `WorkProjectCodeGenerator.Create()` hiện tạo mã dạng `PRJ-yyyyMMdd-<16 ký tự hex>`:

- không truy vấn database;
- tối đa 29 ký tự, nằm trong giới hạn 30 ký tự hiện tại;
- vẫn được bảo vệ cuối cùng bằng unique index `(TenantId, ProjectCode)`;
- được dùng thống nhất trong workflow OKR, controller WorkProject và AI task decomposition.

### 5.2 Loại bỏ N+1 khi bảo đảm WorkItem cho Key Result

Workflow hiện chuẩn hóa/deduplicate danh sách Key Result, lấy tất cả ID đã tồn tại bằng **một** truy vấn `Contains`, kết hợp các entity đang được tracking rồi chỉ thêm phần thiếu. Chi phí truy vấn chuyển từ tỷ lệ theo số Key Result sang một lần đọc theo tập khóa.

### 5.3 Read-only OKR tree

Các truy vấn chỉ đọc trong `OKRsController.GetTree` dùng `AsNoTracking()`, giảm change-tracking allocation và tránh giữ state không cần thiết.

### 5.4 Chống tạo trùng khi workflow OKR chạy đồng thời

Workflow tự động tạo project/task được bao quanh bởi transaction `ReadCommitted`. Trên SQL Server, service khóa dòng OKR nguồn bằng `UPDLOCK, HOLDLOCK` trước khi kiểm tra và ghi dữ liệu. Khi thêm Key Result, controller cũng lấy chính khóa này ngay đầu outer transaction, trước lệnh `INSERT`, để mọi luồng dùng cùng thứ tự khóa OKR → Key Result → WorkItem và tránh lock-order inversion. Phạm vi khóa theo từng OKR nên hai OKR khác nhau vẫn có thể xử lý song song; transaction do caller truyền vào vẫn được tôn trọng và service chỉ commit/rollback transaction do chính nó tạo.

Giải pháp này cố ý chỉ tuần tự hóa luồng automation, không thêm unique constraint rộng làm hỏng các quan hệ one-to-many hợp lệ. Integration test trên database SQL Server LocalDB tạm thời đã xác nhận:

- hai `DbContext` cùng gọi tạo project cho một OKR chỉ để lại một project và đúng một task tự động cho mỗi Key Result;
- transaction thứ nhất giữ khóa OKR; probe xác nhận request thứ hai đã tới cùng lệnh khóa, vẫn bị chặn trước commit và chỉ hoàn tất sau khi transaction thứ nhất giải phóng khóa;
- câu lệnh khóa có cả `UPDLOCK` và `HOLDLOCK`;
- với transaction do caller sở hữu, khóa OKR xuất hiện trước lệnh insert Key Result và service không tự commit transaction đó;
- bước đồng bộ WorkItem chỉ phát một câu `SELECT ... FROM [WorkItems]` thay vì N+1.

Database thử nghiệm được tạo với tên ngẫu nhiên, migrate riêng và xóa trong `finally`; không dùng hoặc thay đổi database ứng dụng thật. Test được đánh dấu `Skipped` rõ ràng khi máy chạy không cấu hình `KPI_SQLSERVER_TEST_CONNECTION`, thay vì báo xanh giả như đã thực thi SQL.

## 6. Những thay đổi cố ý không thực hiện

- Không thêm unique index WorkItem theo Key Result vì nghiệp vụ AI decomposition cho phép nhiều task trên cùng KR.
- Không thêm unique index WorkProject theo SourceOKR vì mô hình và test hiện hành cho phép one-to-many.
- Không đổi các phép chuẩn hóa trạng thái `Trim/ToUpper` khi chưa chuẩn hóa dữ liệu và contract đầu vào; thay đổi vội có thể làm sai dữ liệu lịch sử.
- Không bật Query Store/RCSI, update statistics hàng loạt, đổi recovery model hoặc chạy migration trên database thật.
- Không đưa connection string hoặc nội dung `.env` vào mã nguồn/tài liệu.

## 7. Lộ trình vận hành doanh nghiệp đề xuất

### Giai đoạn A — an toàn và quan sát

- [ ] Chốt RPO/RTO và xác nhận `SIMPLE` có đáp ứng yêu cầu khôi phục hay không.
- [ ] Tạo backup đầy đủ và thử restore trên môi trường cô lập.
- [ ] Vá/nâng SQL Server theo ma trận được Microsoft hỗ trợ; kiểm tra driver và rollback plan.
- [ ] Tạo tài khoản monitoring chỉ đọc, tách biệt tài khoản ứng dụng.
- [ ] Bật Query Store ở staging với quota/capture phù hợp, kiểm tra overhead.

### Giai đoạn B — đo bằng tải gần thực tế

- [ ] Dùng dữ liệu ẩn danh có phân bố gần production.
- [ ] Chạy tải đồng thời cho OKR save/automation, KPI check-in/review, Evaluation Results và báo cáo.
- [ ] Thu p50/p95/p99, error rate, lock waits, deadlocks, top CPU/IO query và plan regression trong 7–14 ngày.
- [ ] Chỉ thêm/bỏ index sau khi đối chiếu query plan, write overhead và tenant selectivity.
- [ ] Thử RCSI ở staging nếu bằng chứng cho thấy reader/writer blocking; kiểm tra tempdb trước khi đề xuất production.

### Giai đoạn C — độ tin cậy nghiệp vụ

- [ ] Chuyển OKR-to-project automation sang outbox/idempotent worker nếu yêu cầu bắt buộc “đã lưu OKR thì chắc chắn có project”.
- [ ] Bổ sung reconciliation job cảnh báo aggregate thiếu liên kết.
- [ ] Thiết lập SLO, dashboard, alert và runbook cho DB latency, deadlock, timeout, outbox backlog và failed jobs.
- [ ] Diễn tập backup/restore, failover và phục hồi theo lịch.

## 8. Tiêu chí xác nhận sau mỗi thay đổi database tương lai

- Migration chạy được trên bản sao staging và có phương án rollback/data preservation.
- Không làm mất global tenant filter, RLS, soft-delete, authorization hoặc audit.
- Query plan sử dụng predicate/index đúng tenant; không tạo full scan ngoài chủ đích.
- Build toàn solution và test suite đều đạt.
- Browser QA bằng Chrome Profile 9 kiểm tra dữ liệu thật ở chế độ không phá hủy.
- Không log secret/PII và không đưa file môi trường vào Git.

## 9. Giới hạn của lần đánh giá

- Số liệu runtime là snapshot trên database hiện có, không phải benchmark production.
- Không có `VIEW SERVER STATE`, nên chưa thể kết luận về waits, index usage và missing-index DMV.
- Tài khoản database ứng dụng không có quyền tạo database; phép thử concurrency vì vậy được chạy trên SQL Server LocalDB cô lập, cùng provider và migration của dự án.
- Không thay đổi cấu hình instance/database vì các thay đổi đó cần backup, cửa sổ bảo trì và phê duyệt vận hành riêng.
- Chrome QA xác nhận luồng và dữ liệu hiển thị; nó không thay thế load test hoặc kiểm thử phục hồi thảm họa.

Tài liệu vận hành tham khảo: [bản cập nhật SQL Server mới nhất](https://learn.microsoft.com/en-us/troubleshoot/sql/releases/download-and-install-latest-updates) và [best practices cho Query Store](https://learn.microsoft.com/en-us/sql/relational-databases/performance/best-practice-with-the-query-store?view=sql-server-ver17).
