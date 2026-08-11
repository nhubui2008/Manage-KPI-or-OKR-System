# Kế hoạch sửa lỗi và phát triển hệ thống KPI/OKR theo hướng AI-native

## 1. Mục tiêu và trình tự triển khai

Hệ thống sẽ được triển khai theo sáu đợt phụ thuộc tuần tự:

1. Vá bảo mật khẩn cấp và các lỗi đã có bằng chứng.
2. Chuẩn hóa dữ liệu, workflow và phân quyền đa tenant.
3. Xây nền tảng RAG, trích nguồn và agent có kiểm soát.
4. Ra mắt Goal Planning Agent: OKR/KPI/KR → kế hoạch công việc.
5. Thêm AI Evaluator phản hồi ngay khi gửi check-in.
6. Chỉ mở dự báo xác suất và multi-agent nâng cao sau khi đủ dữ liệu và vượt các cổng chất lượng.

Mọi hành động ghi dữ liệu của AI phải đi qua bản nháp, kiểm tra nghiệp vụ và phê duyệt của người có thẩm quyền. AI không tự duyệt check-in, đổi hạng, tính thưởng hay đưa ra quyết định nhân sự.

## 2. Sửa toàn bộ lỗi đã xác minh

### Bảo mật và xác thực

- Thu hồi và xoay vòng ngay credential SMTP đã từng được commit trong [repository công khai](https://github.com/nhubui2008/Manage-KPI-or-OKR-System). Xóa giá trị khỏi phiên bản hiện tại, dùng User Secrets/biến môi trường khi phát triển và secret store của nền tảng triển khai ở production. Theo lựa chọn đã chốt, không viết lại lịch sử Git; vì vậy credential cũ bắt buộc phải bị vô hiệu hóa.
- Thay toàn bộ mã reset mật khẩu sáu chữ số dùng `Random`/`TempData` bằng liên kết dùng một lần:
  - Token ngẫu nhiên mật mã, chỉ lưu SHA-256 hash.
  - Hết hạn sau 15 phút, một token đang hoạt động cho mỗi tài khoản, dùng một lần.
  - Phản hồi giống nhau dù email có tồn tại hay không.
  - Có antiforgery, giới hạn theo IP+tài khoản và vô hiệu hóa phiên đăng nhập cũ sau đổi mật khẩu.
- Xóa tham số `password` khỏi GET Login và không bao giờ render lại mật khẩu vào HTML.
- Demo account và mật khẩu mẫu chỉ tồn tại trong Development; startup/pre-deploy phải chặn production nếu còn tài khoản mẫu với mật khẩu mặc định.
- Không trả exception nội bộ cho trình duyệt/JSON; ghi log với correlation ID và trả thông báo chung.
- Mọi kiểm tra quyền không nhận diện được role phải mặc định từ chối. Search chỉ truy vấn và trả từng nhóm Employee/KPI/OKR/Department khi người dùng có đúng quyền tương ứng.
- Luồng mua gói chỉ tạo yêu cầu `PendingAdminApproval`; không tự gán role, kích hoạt thuê bao hoặc kéo dài dùng thử. Chỉ admin mới kích hoạt quyền lợi.

### Tính toàn vẹn dữ liệu và nghiệp vụ

- Gỡ unique filtered index trên `WorkItem.OKRKeyResultId`, thay bằng index không unique để hỗ trợ nhiều task cho một KR. Migration mới không sửa migration cũ và không xóa/vô hiệu hóa dữ liệu khi rollback.
- Chuẩn hóa quan hệ một OKR–nhiều dự án:
  - `WorkProject.SourceOKRId` là nguồn sự thật, mỗi dự án thuộc tối đa một OKR.
  - Di trú dữ liệu từ `LinkedOKRId`/`OKR.LinkedWorkProjectId`, sau đó loại bỏ hai con trỏ trùng lặp.
  - Nếu các con trỏ hiện tại mâu thuẫn, preflight dừng migration và xuất báo cáo; không tự chọn hoặc làm mất dữ liệu.
  - Thêm foreign key `NoAction/Restrict` và index theo tenant.
- Tạo một domain command service dùng chung cho thao tác thủ công và agent. Service phải kiểm tra tenant, phạm vi người thao tác, owner, phòng ban, assignee, quan hệ KPI–KR–OKR–project và quyền ghi trước transaction.
- Dùng input view model thay vì bind trực tiếp `EvaluationResult`; workflow fields, người duyệt và thời gian duyệt luôn do server đặt.
- Chỉ cho sửa kết quả đánh giá ở `Draft` hoặc `Rejected`. `PendingDirectorReview` và `Approved` bị đóng băng; mở lại phải là lệnh riêng có audit.
- Thêm unique constraint `(TenantId, EmployeeId, PeriodId)` cho `EvaluationResult`. Trước migration phải báo cáo bản ghi trùng để quản trị viên hòa giải.
- Tách công thức điểm đang bị lặp thành `IEvaluationCalculator`:
  - Chỉ dùng check-in được duyệt.
  - Xử lý đúng KPI thuận/nghịch, trọng số cá nhân, KPI phòng ban và kỳ đánh giá.
  - Rank/classification luôn được suy ra phía server.
- Bonus không còn được cập nhật từ đề xuất AI hoặc bản check-in tạm. Chỉ dịch vụ compensation tập trung mới tính lại sau khi director phê duyệt kết quả cuối cùng.
- Cho phép nhiều check-in có chủ ý trên cùng KPI, nhưng thêm:
  - `SubmissionId` duy nhất theo tenant để chống double-submit/retry.
  - Unique constraint một `CheckInDetail` cho một `KPICheckIn`.
  - Transaction, row version và xử lý conflict rõ ràng.
- Thêm test project vào solution để `dotnet test <solution>` thực sự chạy toàn bộ test thay vì thành công với 0 test.

### Sửa lớp AI hiện hữu

- Thay các lời gọi Gemini trực tiếp bằng `IAIModelClient`; bỏ endpoint “refine” giả lập và không trình bày `PotentialScore` do LLM tự nghĩ ra như xác suất.
- Chỉ cung cấp check-in đã duyệt cho phân tích chính thức; check-in vừa gửi phải được đánh dấu rõ là dữ liệu dự kiến.
- Thu hẹp nhân sự/phòng ban được đưa vào prompt theo tenant và quyền người dùng.
- Không lưu toàn bộ prompt chứa PII. Lịch sử mới chỉ giữ metadata, hash, model/prompt version, evidence ID, chi phí, độ trễ và kết quả có cấu trúc; dữ liệu lịch sử thô hiện có được sao lưu trước rồi làm sạch.
- Thêm quota theo tenant/người dùng, giới hạn kích thước input/history, timeout, retry hữu hạn, transaction và idempotency cho bước xác nhận.
- Giữ riêng một backlog cho nghi vấn chưa tái hiện; không sửa phỏng đoán hoặc trộn đợt sửa lỗi với thay đổi format toàn repository.

## 3. Nền tảng tenant, RAG và hợp đồng agent

### Cô lập nhiều tenant

- Thêm `Tenant`, `TenantMembership` và tenant subscription; `SystemUser` là danh tính chung, role nghiệp vụ thuộc membership. `PlatformAdmin` là quyền riêng và mọi bypass phải có audit.
- Bổ sung `TenantId` không-null cho dữ liệu nghiệp vụ, AI, tài liệu và audit theo quy trình expand → tạo “Legacy Tenant” → backfill → xác minh → contract.
- Dùng `ITenantContext`, global query filter và SaveChanges interceptor để tự gắn tenant và từ chối tham chiếu chéo tenant. SQL Server Row-Level Security dựa trên `SESSION_CONTEXT` là lớp phòng vệ thứ hai.
- Chuyển các unique index hiện tại như mã phòng ban, mã dự án và tên role thành unique trong tenant.
- Không bật RAG production trước khi kiểm thử cô lập tenant hoàn tất.

### Stack AI đã chốt

- DeepSeek V4 qua typed `HttpClient` và adapter tương thích OpenAI:
  - V4 Pro cho Planner, Critic và Evaluator.
  - V4 Flash cho truy vấn lại, phân loại và tác vụ nhẹ.
  - Model name nằm trong cấu hình, API key chỉ ở secret store.
  - Tool calls và JSON output bám theo [tài liệu DeepSeek](https://api-docs.deepseek.com/guides/tool_calls); JSON phải được schema-validate, retry tối đa một lần và chuyển sang `Abstained/Failed` nếu vẫn sai vì JSON mode có thể trả output rỗng theo [hướng dẫn chính thức](https://api-docs.deepseek.com/guides/json_mode/).
- MinerU chạy trong GPU service riêng, private network; worker gọi đồng bộ `/file_parse` dưới lease heartbeat và chấp nhận at-least-once compute với khóa lưu trữ hội tụ. Tệp PDF/image/DOCX/PPTX/XLSX được quét loại/kích thước/malware, parse thành Markdown rồi chia chunk có section metadata; pin phiên bản source/model/image và hoàn tất rà soát license từ [MinerU chính thức](https://github.com/opendatalab/MinerU).
- BGE-M3 self-host tạo embedding 1024 chiều; lưu model version/checksum để tái index khi đổi model. Đặc tính vector được xác nhận trong [model card BGE-M3](https://huggingface.co/BAAI/bge-m3).
- Qdrant self-host lưu chunk payload và vector BGE-M3. Truy xuất hiện dùng dense vector; server bắt buộc tạo typed filter `TenantId`, `IsCurrent` và ACL user/role/department, sau đó tái kiểm tra từng nguồn với metadata SQL authoritative. Không mô tả đường này là hybrid keyword search khi chưa triển khai keyword retrieval.
- Tài liệu gốc và kết quả MinerU nằm trong bucket MinIO riêng tư qua API S3-compatible; bucket không anonymous access và credential chỉ ở secret store. Số liệu thay đổi thường xuyên như tiến độ, trọng số, workload và check-in được đọc qua tool SQL có whitelist, không vector hóa.

### Dữ liệu và API mới

- Các bảng nền: `AgentRun`, `AgentRunStep`, `AgentDraftAction`, `AgentApproval`, `OutboxMessage`, `KnowledgeDocument`, `KnowledgeDocumentVersion`, `KnowledgeChunk`, `DocumentIngestionJob`, `EvidenceRef`, `EvaluationRubric`, `EvaluationCriterion`, `AiEvaluationProposal`.
- `EvidenceRef` gồm tenant, loại nguồn, source/version ID, tiêu đề, đoạn trích, URI nội bộ, page/section, thời điểm chụp và ACL snapshot. Trước khi đưa vào prompt hoặc hiển thị, server phải tái kiểm tra tenant và quyền trên nguồn gốc.
- Các endpoint nội bộ dùng cookie authentication, antiforgery, input DTO, row version và idempotency key:
  - Khởi tạo/xem run Goal Planning Agent.
  - Duyệt, chỉnh sửa hoặc từ chối `AgentDraftAction`.
  - Xem/chạy lại/áp dụng đề xuất đánh giá cho check-in.
  - Upload và theo dõi trạng thái tài liệu RAG.
- Agent chạy bằng state machine bền vững: `Planning → Retrieving → Validating → Critiquing → WaitingApproval → Executing → Completed/Failed`. Tối đa 8 tool calls, 2 vòng critique; LLM không có kết nối DB ghi trực tiếp.
- Executor chỉ nhận approval token một lần và gọi domain command service trong transaction. Mọi lần thực thi phải idempotent và lưu diff trước/sau.

## 4. Hai agent nghiệp vụ và dự báo độ phù hợp

### Goal Planning Agent

- Nhận một OKR/KPI/KR cùng project đích hoặc yêu cầu tạo project.
- Tool read-only được phép: tìm tài liệu, lấy mục tiêu, check-in đã duyệt, task hiện có, năng lực đội, dữ liệu lịch sử tương tự và validator nghiệp vụ.
- Trả đúng ba phương án task plan. Mỗi task có assignee đề xuất, deadline, phụ thuộc, mức đóng góp KPI/KR, rủi ro, khoảng trống dữ liệu và citation.
- `FitScore` 0–100 được server tính, không phải số LLM tự khai:
  - 35% khớp mục tiêu.
  - 25% kết quả lịch sử cùng nhóm KPI/KR.
  - 20% khớp vai trò/phòng ban.
  - 10% workload và deadline.
  - 10% độ phủ/tính mới của bằng chứng.
- Tách riêng `FitScore` và `EvidenceConfidence`. Nếu độ phủ bằng chứng dưới 60%, không hiển thị điểm tổng; agent phải trả “Không đủ dữ liệu”.
- Người có quyền chọn, sửa và duyệt diff. Chỉ sau bước này Executor mới tạo project/task; nhiều task được phép liên kết cùng KR.

### AI Evaluator khi check-in

- Ngay sau khi người dùng gửi check-in thành công, cùng transaction tạo outbox event; worker chạy AI bất đồng bộ. Đề xuất đầu tiên mang nhãn rõ `Dự kiến – check-in chưa duyệt`.
- Evaluator trả đồng thời:
  - `officialBaselineScore`: điểm hiện tại chỉ từ check-in đã duyệt.
  - `projectedScore`: mô phỏng nếu check-in vừa gửi được chấp thuận.
  - Điểm từng tiêu chí định tính nếu có rubric đang hiệu lực.
  - Classification/rank do server suy ra.
  - Confidence, phân rã confidence, rationale, citations và data gaps.
- Với KPI/KR định lượng, công thức hệ thống quyết định điểm; AI chỉ giải thích bằng chứng, bất thường và rủi ro.
- Với tiêu chí định tính, AI chỉ được đề xuất khi có rubric được version hóa. Các mục tiêu cũ mặc định 100% định lượng; thiếu rubric thì evaluator từ chối chấm phần định tính thay vì đoán.
- Confidence là chất lượng dữ liệu, không phải lời tự tin của mô hình:
  - 40% độ phủ bằng chứng.
  - 25% thẩm quyền nguồn.
  - 20% tính nhất quán giữa số liệu và tài liệu.
  - 15% độ mới.
  - Từ 0,80: cao; 0,60–0,79: trung bình; dưới 0,60: không xuất điểm định tính, chỉ nêu khoảng trống.
- Mỗi nhận xét/tiêu chí phải tham chiếu `EvidenceRef`. Tự khai trong check-in vẫn được trích nguồn nhưng được gắn nhãn “self-reported”, không đồng nghĩa với bằng chứng đã xác minh.
- Khi check-in bị sửa, gửi lại, duyệt hoặc từ chối, proposal cũ chuyển `Stale`. Sau khi duyệt, hệ thống tự chạy lại trên snapshot chính thức; nếu từ chối thì proposal không được chuyển sang kết quả đánh giá.
- Quản lý có thể “Áp dụng vào bản nháp”, chỉnh sửa hoặc từ chối. Thao tác áp dụng chỉ copy đề xuất vào trường review/evaluation draft; không tự duyệt check-in hoặc sửa kết quả chính thức.
- Nếu điểm cuối của con người lệch quá 10 điểm so với baseline công thức, yêu cầu ghi lý do. Director vẫn là người duyệt cuối; AI không tác động trực tiếp tới thưởng, kỷ luật hoặc quyết định nhân sự.

### Dự báo xác suất và multi-agent giai đoạn sau

- Trước khi đủ dữ liệu, chỉ hiển thị FitScore và risk band; không gọi điểm LLM là “xác suất thành công”.
- Chỉ huấn luyện `OutcomeProbability` riêng theo tenant khi có ít nhất 1.000 outcome trưởng thành, 4 kỳ đã đóng và tối thiểu 200 mẫu mỗi lớp.
- Dùng regularized logistic regression với point-in-time feature snapshot và time-based validation. Chỉ phát hành khi:
  - ROC-AUC thời gian ≥ 0,70.
  - Brier score tốt hơn base rate ít nhất 10%.
  - ECE toàn bộ ≤ 0,05 và theo nhóm ≤ 0,10.
  - Calibration slope nằm trong 0,8–1,2.
- Không dùng tên, tuổi, giới tính, email, điện thoại, mã thuế hoặc proxy thuộc tính được bảo vệ. DeepSeek chỉ giải thích kết quả của mô hình dự báo, không tự tạo xác suất.
- Sau pilot mới tách thành Evidence Agent read-only, Planning Agent draft-only, Critic/Evaluator read-only và deterministic Executor có approval token. Mỗi agent có scope, ngân sách và audit riêng.

## 5. Kiểm thử, nghiệm thu và rollout

### Kiểm thử bắt buộc

- Build phải giữ 0 lỗi/0 cảnh báo; `dotnet test` ở solution phải chạy toàn bộ 204 test hiện hữu cùng test mới.
- Migration trên bản sao SQL Server production:
  - Legacy tenant được backfill đầy đủ.
  - Nhiều task/KR và nhiều project/OKR hoạt động.
  - Conflict quan hệ hoặc EvaluationResult trùng phải dừng an toàn và xuất báo cáo.
  - Không migration nào âm thầm xóa dữ liệu.
- Security tests cho reset link, chống enumeration/CSRF/rate limit, demo production, exception leakage, category search và mọi đường truy cập chéo tenant.
- Business tests cho KPI thuận/nghịch, trọng số cá nhân/phòng ban, latest approved check-in, nhiều check-in, chống double-submit, workflow freeze, rank và bonus sau director approval.
- Agent tests cho malformed/empty JSON, timeout, retry, prompt injection trong tài liệu, citation giả, nguồn bị thu hồi quyền, proposal stale, idempotency và đảm bảo không có DB write khi chưa duyệt.
- Evaluator tests phải chứng minh check-in Pending/Rejected không đi vào baseline chính thức, low-confidence dẫn đến abstain và AI không thay đổi rank/bonus/approval.
- UI QA cuối cùng phải chạy bằng Chrome Windows với đúng profile `Profile 9 (testchormecodex)` theo quy định repository.

### Cổng phát hành

- Shadow mode trước: lưu proposal nhưng không có nút áp dụng.
- Pilot giới hạn một tenant/phòng ban, sau đó mới mở theo feature flag.
- Điều kiện mở rộng:
  - 0 lỗi rò dữ liệu chéo tenant.
  - 0 hành động ghi thiếu approval.
  - 100% nhận định số liệu có citation.
  - Citation precision kiểm mẫu ≥95%.
  - Structured output hợp lệ ≥99%.
  - Ít nhất 60% task AI đề xuất được giữ lại sau khi người dùng chỉnh sửa.
- Dashboard theo dõi latency, token/cost theo tenant, tỷ lệ retry/abstain, citation failure, proposal accepted/edited/rejected và chênh lệch giữa AI–manager–director.

### Giả định đã khóa

- Một cơ sở dữ liệu SQL Server dùng chung cho nhiều tenant.
- Một KR có nhiều task; một OKR có nhiều dự án.
- AI Evaluator phản hồi ngay khi gửi check-in, nhưng đó là bản dự kiến và được làm mới sau phê duyệt.
- DeepSeek V4 + MinerU GPU + BGE-M3 + Qdrant + MinIO là stack chính; không thêm LangChain/Semantic Kernel ở bản đầu.
- Không viết lại lịch sử Git; credential bị lộ phải được xoay vòng.
- Giữ nguyên và tích hợp cẩn thận các thay đổi chưa commit hiện có của người dùng, không revert.
- Format drift toàn repository là maintenance riêng; đợt sửa lỗi chỉ format file được chạm tới để giữ diff nhỏ và có thể review.
