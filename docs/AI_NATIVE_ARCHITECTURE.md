# Kiến trúc AI-native cho KPI/OKR

## Trạng thái đã triển khai

Hệ thống hiện có chín luồng AI-native hoạt động theo nguyên tắc **AI đề xuất, con người quyết định**:

1. **Goal Planning Agent**
   - Đọc KPI, OKR, Key Result hoặc Work Project mà người dùng được phép truy cập.
   - Có vòng lặp agent giới hạn: mô hình chỉ được gọi tool `search_evidence`; server tự tạo tenant/ACL filter và thực thi truy xuất.
   - Trả đúng ba task plan có assignee, deadline, phụ thuộc, đóng góp KPI/KR, rủi ro, khoảng trống dữ liệu và citation. Fit do server tính theo 35/25/20/10/10; dưới 60% độ phủ bằng chứng thì ẩn điểm tổng.
   - Run được lưu từ `Planning` qua retrieval/generation/validation/critic tới `WaitingApproval`; endpoint xem lại dựng lại draft đã lưu, tái kiểm tra source/RAG ACL và xoay approval token/row-version. Confirm cũng tái kiểm tra ACL ngay trước write nên modal cũ không thể áp dụng bằng chứng đã bị thu hồi.
   - Không có tool ghi dữ liệu. Task chỉ được tạo qua luồng xác nhận hiện có.
   - Khi người dùng xác nhận, server kiểm tra lại source/project/assignee/department/KPI/KR bằng cùng validator của thao tác thủ công. Nhiều task cùng KPI/nhân viên được tổng hợp thành một check-in `Pending`, ghi durable outbox theo source version và không thay đổi KPI/KR chính thức.

2. **Check-in AI Evaluator**
   - Được xếp hàng sau khi check-in `Pending` thủ công hoặc check-in tổng hợp từ task đã commit.
   - Chỉ dùng check-in `Approved` làm baseline chính thức; bản đang chờ luôn là dữ liệu tạm thời.
   - Rubric deterministic xét tiến độ theo lịch, deadline, pass/fail threshold, giai đoạn kỳ đánh giá và KPI thuận/nghịch.
   - Rubric định tính/behavioral được version hóa theo KPI/chu kỳ bằng `EvaluationRubrics` và `EvaluationCriteria`. Trang quản lý rubric nằm trong KPI Details, chỉ tạo version mới; version đã phát hành là bất biến và version mới làm proposal đang chờ trở thành `Stale`, rồi requeue bằng rubric đang hiệu lực tại thời điểm đánh giá.
   - Trả official baseline, projected progress, classification do server suy ra, nguồn, data gaps và confidence 40% coverage + 25% authority + 20% consistency + 15% freshness. Dưới 0,60 chỉ abstain phần chấm định tính; kết quả định lượng vẫn do công thức hệ thống quyết định.
   - Lưu metadata kiểm toán của run/proposal/citation; không lưu prompt, ghi chú check-in hoặc đoạn tài liệu.
   - Cổng rollout trung tâm có kill switch và bốn mode `Disabled`, `Shadow`, `Pilot`, `GeneralAvailability`. `Shadow` vẫn lưu proposal để đo chất lượng nhưng trả `CanApplyToDraft=false`; `Pilot` chỉ xử lý check-in thuộc tenant cho phép và, nếu cấu hình, phòng ban đang hoạt động của chính nhân viên được đánh giá. Queue, worker, evaluator và hai đường áp dụng phía server đều kiểm tra lại gate nên request giả hoặc thay đổi assignment sau enqueue đều fail-closed.
   - Chấp nhận/từ chối đề xuất AI không thay đổi điểm, trạng thái duyệt, xếp hạng hay thưởng. Quy trình review của con người vẫn là cổng cuối.
   - Quản lý có thể đưa đề xuất có row-version còn hiệu lực vào form review dưới dạng bản nháp, chỉnh sửa rồi tự gửi quyết định. Proposal chỉ được ghi `AppliedByHuman` trong cùng transaction với quyết định review; điểm cuối lệch hơn 10 điểm so với baseline công thức bắt buộc có lý do. Khi duyệt, snapshot `Approved` được enqueue lại trong cùng transaction để tạo quan sát chính thức; bản nháp hoặc thao tác AI riêng lẻ không tự duyệt hay thay đổi thưởng/xếp hạng.

3. **OKR Key Result AI Advisor**
   - Đánh giá một giá trị KR đang được người dùng cân nhắc trước khi cập nhật chính thức.
   - Server tự nạp KR/OKR, kiểm tra scope, tính tiến độ và trạng thái theo quy tắc deterministic có hỗ trợ KR nghịch.
   - RAG/DeepSeek chỉ bổ sung nguồn và diễn giải giới hạn. Candidate-only, nguồn cũ, gián tiếp hoặc reliability thấp đều không đủ để phân loại.
   - Proposal có fingerprint của KR, OKR cha và candidate; proposal cũ bị supersede khi nguồn thay đổi.
   - Accept/Reject chỉ ghi metadata quyết định. `CurrentValue` và `ResultStatus` chỉ đổi khi con người gửi form cập nhật OKR.

4. **Evaluation Review Draft Advisor**
   - `GenerateReview` kiểm tra quyền trên dữ liệu đánh giá ở server rồi gọi model qua `IAIModelClient`; không còn gọi Gemini trực tiếp.
   - Chỉ lưu bản nháp hiển thị giới hạn 2.000 ký tự, source fingerprint và citation metadata; không lưu prompt, context/RAG excerpt hoặc raw provider response.
   - `AgentDraftAction` ở trạng thái `AwaitingHumanReview` cho tới khi người dùng áp dụng vào form hoặc từ chối. Khi áp dụng, server khóa nguồn, kiểm tra row-version, fingerprint và quyền RAG hiện tại rồi mới trả nội dung cho browser.
   - Fingerprint gồm cả trạng thái workflow. Edit/Submit/DirectorReview đóng các draft đang chờ trong cùng lần commit với source; draft cũ không thể sống lại sau vòng `Draft -> Pending -> Rejected`.
   - Browser chỉ cho tạo/áp dụng khi employee, period và score vẫn khớp snapshot của form lúc mở; thay đổi chưa lưu phải được lưu trước rồi mới tạo draft mới.
   - Thao tác áp dụng chỉ điền textarea để con người tiếp tục sửa và gửi form chuẩn. Nó không tự ghi `ReviewComment`, điểm, xếp loại, trạng thái duyệt, thưởng hoặc kỷ luật.

5. **Customer Segment Advisor**
   - Đọc snapshot KPI/OKR, check-in đã duyệt và dữ liệu doanh thu trong đúng tenant/phạm vi nhân viên/phòng ban của người gọi; không gửi email, mã hoặc tên nhân viên sang model.
   - Gọi model qua `IAIModelClient`, bắt buộc strict JSON, nguồn do server cấp và cho phép trả danh sách rỗng khi thiếu bằng chứng.
   - Chỉ đưa ra phân khúc, khoảng trống dữ liệu và hành động tham khảo. Contract và UI không còn `PotentialScore`, xếp hạng hoặc xác suất do LLM tự khai.
   - Sau model call, server dựng lại snapshot trong transaction `Serializable`; dữ liệu hoặc phạm vi đổi thì trả conflict và không dùng đề xuất cũ.
   - Chỉ lưu `AgentRun` và citation metadata/hash; không lưu prompt, authorized context, nội dung đề xuất hay raw provider response. Luồng này không có thao tác ghi vào bảng nghiệp vụ.

6. **Performance Analysis Advisor**
   - Chỉ tổng hợp `CheckInDetail` thuộc check-in `Approved` trong đúng tenant và phạm vi nhân viên/phòng ban; nếu không có tiến độ đo lường thì server abstain trước khi gọi model.
   - Trả strict JSON gồm tổng quan, điểm mạnh, rủi ro và hành động có source ID do server cấp. Model không được xếp hạng nhân viên, dự báo xác suất hoặc đưa quyết định thưởng/kỷ luật.
   - Dashboard render từng insight bằng text đã escape và hiển thị nguồn. Kết quả không thay đổi điểm, xếp loại, trạng thái duyệt hoặc thưởng.
   - Sau model call, server dựng lại snapshot trong transaction `Serializable`; fingerprint đổi thì trả conflict và không lưu kết quả cũ. Chỉ `AgentRun` cùng citation metadata/hash được lưu khi source vẫn khớp.

7. **KPI Suggestion Advisor**
   - Chỉ cho role được phép tạo KPI chọn kỳ đang mở, nhân viên/phòng ban và OKR/Key Result trong phạm vi được cấp quyền; context gửi model không chứa tên, mã, email hoặc số điện thoại nhân viên.
   - Gọi qua `IAIModelClient` và bắt buộc strict JSON gồm 3–5 bản nháp hoặc danh sách rỗng để abstain. Tên, đơn vị, KPI thuận/nghịch và quan hệ target/pass/fail được server kiểm tra theo cùng quy tắc của form tạo KPI.
   - Mỗi bản nháp phải dùng source ID do server cấp và luôn viện dẫn snapshot lập KPI được cấp quyền. Sau model call, server dựng lại snapshot trong transaction `Serializable`; source đổi thì trả conflict và không lưu metadata cũ.
   - Chỉ lưu `AgentRun` và citation metadata/hash; không lưu prompt, context, bản nháp hoặc raw provider response. Nút áp dụng chỉ điền form cùng phạm vi/kỳ/OKR đã chọn; KPI chính thức vẫn phải qua POST `KPIs/Create` và toàn bộ validator/quyền nghiệp vụ.
   - Endpoint/UI `RefineKpiSuggestions` đã bị loại bỏ vì chỉ chỉnh dữ liệu do browser gửi lại mà không có source-version đáng tin cậy.

8. **Chat Advisor**
   - `AIController.Chat` không còn gọi Gemini trực tiếp. Server xác thực membership/role tenant hiện hành, dựng lại principal chuẩn từ role và assignment đang hoạt động, từ chối kỳ đánh giá giả hoặc ngoài tenant rồi mới tạo snapshot KPI/OKR.
   - Câu hỏi giới hạn 1.000 ký tự; lịch sử chỉ nhận tối đa tám message `user`/`assistant`, mỗi message tối đa 1.000 ký tự và tổng tối đa 4.000 ký tự. Lịch sử, câu hỏi, context, RAG excerpt và raw provider response đều là dữ liệu tạm thời, không được lưu.
   - Snapshot chỉ dùng check-in `Approved`; RAG tối đa ba tài liệu dùng filter tenant/ACL do server sinh. Mỗi kết quả được đối chiếu lại với document/version/chunk active trong SQL và ACL hiện hành trước khi gửi model. Nếu role/phòng ban đổi trong lúc retrieval thì dừng trước model.
   - Model phải trả strict JSON đúng `answer` và `sourceIds`, chỉ dùng source ID do server cấp; khi thiếu bằng chứng phải trả answer rỗng. Sau model call, server dựng lại snapshot và kiểm tra lại membership, ACL, version/fingerprint trong transaction `Serializable`; nguồn stale hoặc bị thu hồi trả conflict.
   - Chỉ lưu `AgentRun` và citation metadata; không lưu nội dung hội thoại/câu trả lời. Widget escape Markdown giới hạn, hiển thị nguồn bằng DOM text, không gửi lặp câu hỏi hiện tại và luôn ghi rõ kết quả chỉ mang tính tư vấn.

9. **OKR Key Result Suggestion Advisor**
   - Thay thế hoàn toàn đường gọi Gemini trực tiếp trước đây của `SuggestKeyResultsAPI` và `RefineKeyResultSuggestions`; cả hai đi qua `IAIModelClient` và cùng một contract strict JSON.
   - Server nạp Objective cùng toàn bộ KR chính thức trong đúng tenant, xác thực membership/role/quyền `OKRS_CREATE` hiện hành và dùng cùng `OkrKeyResultAccessScope` với luồng nghiệp vụ.
   - Bản nháp chỉ nhận đơn vị trong danh mục chuẩn, chỉ tiêu dương tối đa hai chữ số thập phân, tên không trùng KR chính thức và source ID do server cấp. Agent được phép trả danh sách rỗng để abstain.
   - Sau model call, server dựng lại quyền và fingerprint OKR/KR trong transaction `Serializable`; dữ liệu hoặc scope đổi thì trả conflict và không dùng bản nháp stale.
   - Chỉ lưu `AgentRun` cùng citation metadata/hash. Prompt, context, nội dung refine, bản nháp và raw provider response không được lưu. Việc tạo KR chính thức vẫn chỉ xảy ra khi con người chọn/sửa rồi gửi `AddMultipleKeyResults` qua validator chuẩn.

Fit score được server tính theo 35% khớp mục tiêu, 25% kết quả lịch sử cùng nhóm nguồn, 20% assignment/phòng ban, 10% workload/deadline và 10% chất lượng bằng chứng. Đây vẫn không phải đánh giá kỹ năng hay công suất thực tế của nhân sự; thành phần lịch sử bị chấm 0 khi thiếu mẫu và tổng điểm bị ẩn nếu độ phủ bằng chứng dưới 60%. Outcome likelihood là ước lượng thực nghiệm trên lịch sử task của chính nguồn, có Beta smoothing và báo `InsufficientData` khi dưới 20 mẫu; chưa được coi là calibrated probability cho một cohort tương đồng.

Chat và gợi ý/refine KR đã rời Gemini, đi qua model gateway cùng lifecycle nguồn/quyền có kiểm soát. Smart Alerts cũng không còn gọi model: rule engine chỉ dùng dữ liệu KPI/KR và check-in đã duyệt, gộp một cảnh báo ưu tiên cao nhất cho mỗi nguồn rồi reconcile atomically để cảnh báo đã hết rủi ro biến mất ngay. Ba endpoint sinh task Gemini cũ cùng request/response và parser/prompt/history liên quan đã bị gỡ; UI chỉ dùng `CreateGoalPlanningDraft` rồi `ConfirmDecompose`. `GeminiService`, cấu hình Gemini, modal/API lịch sử raw và các runtime caller liên quan đã bị gỡ. Bảng `AIGenerationHistories` cũ được giữ lại chỉ để tương thích migration và retention dữ liệu lịch sử; runtime không còn reader/writer mới.

## RAG và ranh giới bảo mật

Luồng mục tiêu:

```text
Tài liệu -> MinerU -> chunk -> BGE-M3 (1024 chiều)
         -> Azure AI Search
         -> hybrid retrieval có TenantId + ACL
         -> citation/confidence -> agent
```

Các adapter MinerU, BGE-M3 và Azure AI Search đã có. Lớp persistence SQL có `KnowledgeDocument`, version bất biến, metadata chunk và `DocumentIngestionJob`. `DocumentIngestionQueue` tạo intent idempotent theo `(TenantId, DocumentVersionId, Operation, PipelineVersion, AccessPolicyVersion)` và từ chối tài liệu bị xóa, ACL sai, checksum/URI không hợp lệ hoặc requester giả mạo. Nội dung nguồn/chunk vẫn ở private Blob; SQL chỉ giữ metadata, URI và checksum.

Ingestion worker đã nối đủ MinerU -> parse -> BGE-M3 -> Azure Search với các rào chắn sau:

- claim có lease, heartbeat bằng DbContext riêng trong suốt external call, retry/backoff và `DeadLetter`;
- gửi idempotency key ổn định theo job cho MinerU và dùng search key ổn định theo document version/pipeline/ACL;
- chỉ nhận HTTPS exact origin, tắt redirect/cookie cho client RAG và loại built-in logger khỏi client mang SAS;
- kiểm tra magic/package của tệp và bắt buộc quét ClamAV trước khi gửi tài liệu sang MinerU;
- ghi chunk SQL ở trạng thái inactive trước khi upsert Azure; transaction `Serializable` khóa theo document, chỉ intent pipeline hợp lệ mới nhất được kích hoạt và vô hiệu hóa atomically các pipeline cũ;
- truy vấn Azure luôn thêm `IsCurrent`, tenant/ACL phía server rồi đối chiếu từng `ChunkId` với SQL active, ACL hiện tại, version `Indexed` và trạng thái xóa;
- xóa mềm tạo durable `Delete` intent, khóa chunk trong SQL trước rồi retry de-index Azure.

Code worker đã hoàn thành nhưng RAG chưa được coi production-ready cho tới khi index, private Blob/SAS, ClamAV và các provider thật được cấu hình và kiểm thử trên staging. Mỗi truy vấn production phải có:

- `TenantId` do middleware xác định từ membership hợp lệ;
- ACL filter do server sinh từ `user:{id}`, `role:{role}` và `department:{id}`; department claim được nạp lại từ assignment đang hoạt động của đúng tenant;
- kiểm tra lại `TenantId` trên từng kết quả;
- vector đúng 1024 chiều.

Index Azure AI Search tối thiểu cần các field:

| Field | Kiểu/đặc tính |
|---|---|
| `TenantId` | `Edm.Int32`, filterable |
| `AllowedPrincipalIds` | `Collection(Edm.String)`, filterable |
| `DocumentId`, `VersionId`, `ChunkId` | key/metadata có thể truy vết |
| `Title`, `Content` | searchable |
| `ObservedAt`, `Reliability`, `IsCurrent` | metadata confidence |
| `contentVector` | vector 1024 chiều |

Không nhận filter từ browser hoặc prompt. Tài liệu chưa có ACL hợp lệ không được index vào kho production.

## Cấu hình triển khai

Dùng secret store hoặc biến môi trường; `.env` chỉ dành cho local. Mẫu cấu hình ở [`.env.example`](../.env.example). Trước khi bật AI:

1. Chạy các migration mới theo đúng thứ tự:
   - `20260727090000_HardenWorkflowIntegrity`
   - `20260727135849_IntroduceTenantIsolation`
   - `20260727152031_HardenAiHumanReviewAndExternalIdentity`
   - `20260727153240_AddVerifiableAiEvidenceMetadata`
   - `20260727161708_AddOkrKeyResultAiAdvisoryValue`
   - `20260805200300_AddDurableCheckInAiEvaluationOutbox`
   - `20260810083128_AddRagIngestionPersistence`
   - `20260810095927_CanonicalizeOkrProjectRelationship`
   - `20260810101540_AddTenantRowLevelSecurity`
   - `20260810105645_AddGenericAgentDraftActions`
   - `20260810204208_AddGoalPlanningApprovalProof`
   - `20260810214630_AddVersionedCheckInEvaluationRubrics`
2. Giữ `AiAdvisoryRollout:KillSwitch=true` và `CheckInEvaluationMode=Disabled` cho tới khi hoàn tất kiểm thử. Mở `Shadow` trước; chỉ chuyển `Pilot` khi đã cấu hình ít nhất một `PilotTenantIds` và tùy chọn `PilotDepartmentIds`. `GeneralAvailability` chỉ dùng sau khi cổng chất lượng được phê duyệt.
3. Tạo index Azure AI Search với schema/ACL ở trên.
4. Cấu hình DeepSeek, BGE-M3, Azure Search, MinerU, private Blob, exact `KnowledgeStorage:AllowedReadOrigins` và pin `DocumentIngestion:PipelineVersion` qua secret store/biến môi trường.
5. Triển khai ClamAV và cấu hình `MalwareScanner`; lỗi cấu hình phải làm job retry/dead-letter, không được bypass quét.
6. Kiểm thử tenant A không thể truy xuất citation của tenant B và kiểm thử de-index trên staging trước khi bật ingestion.

Queue đánh giá check-in dùng SQL outbox tenant-scoped và commit cùng transaction tạo/cập nhật check-in. Gate chặn enqueue khi feature dừng/ngoài pilot; worker lọc tenant/phòng ban trước claim và kiểm tra lại trước model call. Nếu cấu hình hoặc assignment đổi trong khoảng claim, lease được trả về `Pending` mà không tiêu hao attempt để có thể tiếp tục an toàn khi scope được mở lại. Worker claim bằng lease có điều kiện, phục hồi lease hết hạn sau restart, retry có exponential backoff và chuyển `DeadLetter` sau giới hạn. Job được idempotent theo `(TenantId, CheckInId, SourceVersion)`; membership/role luôn được nạp lại trước khi chạy. Outbox chỉ lưu metadata vận chuyển và mã lỗi giới hạn, không lưu prompt, ghi chú hay exception text. `AgentRun` vẫn bắt đầu khi proposal được tạo thành công và tiếp tục là lifecycle review của con người, không bị dùng làm transport queue.

SQL Server RLS là lớp phòng vệ sau global query filter: 57 bảng nghiệp vụ tenant-scoped có filter predicate cùng block predicate cho `AFTER INSERT` và `AFTER UPDATE`. Mỗi logical SQL connection được gắn `TenantId` và `SystemUserId` read-only qua `SESSION_CONTEXT`; unresolved context dùng sentinel fail-closed. Không có runtime bypass. Hai worker nền đọc danh sách tenant hoạt động từ bảng platform `Tenants`, sau đó claim từng hàng trong tenant context riêng và luân phiên tenant để tránh bỏ đói hàng đợi.

Trang `/KnowledgeDocuments` đồng thời là điểm vận hành AI/RAG cho role tenant `Admin`/`Administrator`/`Director`/`HR`. Bảng check-in outbox chỉ hiển thị tối đa 50 job metadata gần nhất của tenant. Nút chạy lại chỉ có cho `DeadLetter`; server khóa row trên SQL Server, kiểm tra row-version, trạng thái check-in vẫn `Pending` và fingerprint nguồn chưa đổi, sau đó đặt lại attempt/lease bằng actor hiện tại và ghi audit metadata. `Cancelled` không thể bị cưỡng ép chạy lại vì thường biểu thị quyền hoặc nguồn đã bị thu hồi/thay đổi.

Queue ingestion chụp tenant/ACL/version/pipeline tại lúc enqueue và unique index chống tạo trùng. Worker nạp lại document/version/ACL trước external write, dừng khi lease mất, không kích hoạt chunk nếu ACL/version stale, và cho phép enqueue lại intent đã `Cancelled`/`DeadLetter` sau khi người có quyền yêu cầu retry.

Luồng quản trị nguồn đã có tại `/KnowledgeDocuments` cho role tenant `Admin`/`Administrator`/`Director`/`HR`: upload nguồn mới hoặc version mới vào private Blob, ACL có cấu trúc, trạng thái version/job, retry có kiểm soát, cập nhật ACL để re-index và xóa mềm. Submission ID chống upload lặp; ACL/xóa/retry mang row-version để từ chối form stale. Controller không nhận tenant/owner/status/URI/checksum từ browser, không có endpoint download công khai và audit chỉ ghi metadata giới hạn. Upload tạo reservation SQL `Failed` trước external write; reservation đồng thời được tuần tự hóa bằng transaction-scoped application lock theo tenant/document hoặc submission và có retry hẹp cho SQL deadlock. Sau đó hệ thống conditional-create Blob (`If-None-Match: *`) và chỉ cuối cùng mới enqueue/audit trong transaction khác. Vì vậy lỗi mạng, crash hoặc lỗi mở/finalize transaction vẫn để lại URI/checksum bền vững để cùng nội dung tiếp tục, không tạo Blob vô chủ và không có request thua nào được xóa Blob của request thắng.

Trang này cũng tổng hợp metadata 30 ngày theo tenant: số index hoàn tất/dead-letter, success rate, tỷ lệ có retry, latency trung bình/P95, proposal citation coverage, tỷ lệ citation vừa current vừa directly relevant và abstain rate. Citation coverage chỉ đo proposal có ít nhất một citation metadata; không được trình bày như citation precision và không thay thế kiểm mẫu trước pilot.

Dashboard cùng trang có khu vực hiệu chỉnh riêng cho Check-in AI, chỉ lấy proposal provisional được tạo trong 30 ngày của tenant hiện tại. Hệ thống phân biệt quyết định dùng/không dùng bằng `HumanDecision`; dữ liệu cũ được fallback có kiểm soát từ `AgentApproval.Decision` hoặc trạng thái human-review. Việc này không phụ thuộc lifecycle `Status` hiện tại vì proposal đã áp dụng vẫn được chuyển `Stale` khi review đóng. Chỉnh điểm được đo bằng `HumanReviewScore - ProjectedScore`, hiển thị là chênh AI–người duyệt qua delta có dấu và MAE. Tỷ lệ abstain định tính lấy từ kết quả criterion `Qualitative`/`Behavioral`; dải confidence tái lập đúng ngưỡng `MinimumConfidenceToPropose` của rubric/version gắn với từng proposal, sau đó phân Moderate/High ở mốc 0,80. Count luôn là aggregate; rate và delta chỉ xuất hiện từ 20 mẫu hợp lệ. Không có breakdown nhân viên/phòng ban, tên người duyệt, comment, rationale hay prompt. Chỉ số này không ghép điểm director vì điểm tổng hợp kỳ đánh giá không cùng đơn vị với một check-in.

Migration được kiểm tra bằng `dotnet-ef 10.0.5`, gồm chạy từ database rỗng, canonical OKR-project, RLS, Goal Planning approval proof và versioned check-in evaluation rubrics theo Up/Down/reapply trên SQL Server. Luồng admin upload -> retry -> đổi ACL -> xóa mềm, check-in outbox DeadLetter -> retry, raw SQL/EF chéo tenant, worker claim luân phiên tenant, calibration query tách hai tenant, concurrent evaluation-review draft, Check-in evaluator/rubric lifecycle và Goal Planning concurrent draft/double-confirm/recovery cũng đã chạy qua database tạm. Migration mới nhất là `20260810214630_AddVersionedCheckInEvaluationRubrics`; model snapshot không có drift. Chưa apply vào database production vì workspace không có credential/database production; phải backup và rehearsal trên staging theo [quy trình migration](DATABASE_MIGRATION_DEPLOYMENT.md) trước khi rollout.

## Lộ trình phát triển đề xuất

### Giai đoạn tiếp theo: RAG vận hành

- Chốt chính sách thời hạn lưu rồi mới bổ sung retention purge vật lý; không tự đặt thời gian xóa tài liệu doanh nghiệp.
- Theo dõi trang vận hành AI/RAG hiện đã xem/retry được ingestion job và check-in outbox `DeadLetter`; chỉ mở thêm generic outbox/step khi agent workflow dùng chung được triển khai.
- Chạy rehearsal với MinerU/BGE-M3/Azure Search/ClamAV thật, gồm provider timeout, partial batch và cross-tenant leakage.
- Đánh giá retrieval bằng precision@k, citation coverage và cross-tenant leakage test.

### Multi-agent có kiểm soát

- **Retriever Agent**: chỉ tìm nguồn được phép.
- **Planner Agent**: tạo phương án/task nháp có cấu trúc.
- **Critic Agent**: kiểm tra mâu thuẫn, thiếu nguồn và mục tiêu không đo được.
- **Evaluator Agent**: chấm check-in tham khảo và khai báo confidence/abstain.
- **OKR Advisor Agent**: đánh giá candidate KR có nguồn, không tự cập nhật tiến độ.
- **Executor**: không tự trị; chỉ chạy lệnh đã validate sau khi người có quyền xác nhận.

Các agent chia sẻ `AgentRunId`, giới hạn số bước/tool, deadline và audit metadata. Không agent nào được tự duyệt KPI/OKR, tự sửa điểm hay quyết định thưởng.

### Đo lường và hiệu chỉnh

- Lưu quyết định accept/reject và lý do dạng mã, tránh lưu PII tự do.
- Dashboard Check-in AI hiện theo dõi calibration tenant-scoped theo confidence band, tỷ lệ abstain định tính, quyết định dùng/không dùng, chỉnh điểm và chênh với review của người duyệt; citation validity vẫn nằm trong cùng trang vận hành.
- Chỉ hiển thị xác suất dự đoán sau khi đạt cỡ mẫu tối thiểu và vượt kiểm thử calibration theo từng tenant/phòng ban.
- Mở rộng cùng hợp đồng rollout/canary đã dùng cho Check-in AI sang các advisor khác sau khi từng luồng có dashboard chất lượng và cổng staging tương ứng.
