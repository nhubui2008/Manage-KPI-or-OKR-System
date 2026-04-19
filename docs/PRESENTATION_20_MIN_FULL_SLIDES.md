# Bài Thuyết Trình 20 Phút - VietMach KPI/OKR System

> File tổng hợp cuối cùng, dựa trên các tài liệu trước: `PRESENTATION_20_MIN_OUTLINE.md`, `PRESENTATION_FULL_MARKDOWN.md` và `DEMO_FLOW_6_MEMBERS.md`.
>
> Mục tiêu: dùng trực tiếp để tạo slide PowerPoint/Canva/Marp. Mỗi slide có **người nói**, **thời lượng**, **nội dung trên slide**, **sơ đồ Mermaid nếu cần**, và **lời thoại gợi ý**.

---

## Tổng Thời Lượng Và Phân Vai

| Vai trò | Slide phụ trách | Thời lượng |
| --- | --- | ---: |
| PO | Slide 1-4 | 3 phút |
| SM | Slide 5-7 | 3 phút |
| Luồng hệ thống | Slide 8-11 | 4 phút |
| Frontend | Slide 12-14 | 3 phút |
| Backend | Slide 15-16 | 4 phút |
| Tester | Slide 17-18 | 3 phút |
| **Tổng** | **18 slide** | **20 phút** |

---

## Slide 1 - Tên Đề Tài Và Thành Viên

**Người nói:** PO  
**Thời lượng:** 0.5 phút

### Nội dung trên slide

- **VietMach KPI/OKR System**
- Hệ thống quản lý KPI/OKR cho doanh nghiệp
- Thành viên theo vai trò:
  - PO
  - SM
  - Luồng hệ thống
  - Frontend
  - Backend
  - Tester
- Mục tiêu bài nói: trình bày bài toán, giải pháp, luồng nghiệp vụ, kiến trúc, giao diện và kiểm thử.

### Sơ đồ

```mermaid
flowchart LR
    A["PO<br/>Bài toán"] --> B["SM<br/>Quy trình"]
    B --> C["Luồng hệ thống<br/>Nghiệp vụ"]
    C --> D["Frontend<br/>Giao diện"]
    D --> E["Backend<br/>Kỹ thuật"]
    E --> F["Tester<br/>Chất lượng"]
```

### Lời thoại gợi ý

> Xin chào thầy cô và các bạn. Nhóm em trình bày đề tài VietMach KPI/OKR System, một hệ thống web hỗ trợ doanh nghiệp quản lý KPI và OKR từ mục tiêu chiến lược đến đánh giá cuối kỳ. Bài thuyết trình được chia theo 6 vai trò: PO, SM, luồng hệ thống, frontend, backend và tester để thể hiện đầy đủ góc nhìn sản phẩm, quy trình, nghiệp vụ, kỹ thuật và chất lượng.

---

## Slide 2 - Bài Toán Và Nhu Cầu Khách Hàng

**Người nói:** PO  
**Thời lượng:** 1 phút

### Nội dung trên slide

- Khách hàng mục tiêu: doanh nghiệp có nhiều phòng ban, nhiều cấp quản lý.
- Nhu cầu chính:
  - Theo dõi mục tiêu chiến lược và KPI theo kỳ.
  - Nhân viên check-in tiến độ minh bạch.
  - Quản lý duyệt dữ liệu trước khi tính điểm.
  - HR/Director cần báo cáo, xếp loại và thưởng dự kiến.
  - Hệ thống cần phân quyền theo vai trò.
- Vấn đề hiện tại:
  - Quản lý thủ công bằng Excel/file rời.
  - Dữ liệu phân tán, khó truy vết.
  - Đánh giá cuối kỳ dễ thiếu minh bạch.

### Sơ đồ

```mermaid
flowchart TD
    A["Thực trạng"] --> B["KPI/OKR quản lý bằng Excel"]
    A --> C["Dữ liệu rời rạc"]
    A --> D["Khó kiểm soát tiến độ"]
    A --> E["Đánh giá thiếu minh bạch"]

    B --> F["VietMach KPI/OKR System"]
    C --> F
    D --> F
    E --> F

    F --> G["Tập trung dữ liệu"]
    F --> H["Check-in và duyệt"]
    F --> I["Tính điểm/rank/bonus"]
    F --> J["Dashboard và báo cáo"]
    F --> K["Phân quyền theo vai trò"]
```

### Lời thoại gợi ý

> Khi phân tích nhu cầu khách hàng, nhóm nhận thấy nhiều doanh nghiệp vẫn quản lý KPI bằng Excel hoặc các file riêng lẻ. Điều này khiến mục tiêu chiến lược, KPI hằng ngày, check-in tiến độ và đánh giá cuối kỳ không nằm trong cùng một luồng. Hệ thống VietMach KPI/OKR System giải quyết bằng cách tập trung dữ liệu, cho phép check-in có kiểm duyệt, tự động tính điểm và cung cấp dashboard, báo cáo theo từng vai trò.

---

## Slide 3 - Môi Trường Áp Dụng, Quy Mô Và Tầm Nhìn

**Người nói:** PO  
**Thời lượng:** 1 phút

### Nội dung trên slide

- Môi trường áp dụng:
  - Web nội bộ doanh nghiệp.
  - Các vai trò: Admin, Director, Manager, HR, Employee/Sales.
  - Vận hành theo kỳ đánh giá tháng/quý/năm.
- Quy mô:
  - Nhiều module nghiệp vụ: nhân sự, OKR, KPI, check-in, evaluation, report, AI.
  - Nhiều cấp dữ liệu: công ty, phòng ban, cá nhân.
  - Có seed data, test data và tài khoản demo.
- Tầm nhìn:
  - Số hóa quản trị hiệu suất.
  - Kết nối chiến lược công ty với hành động của từng nhân viên.
  - Hỗ trợ ra quyết định bằng dữ liệu.

### Sơ đồ

```mermaid
flowchart TD
    A["Tầm nhìn doanh nghiệp"] --> B["Mission / Vision"]
    B --> C["OKR cấp công ty / phòng ban"]
    C --> D["KPI theo kỳ đánh giá"]
    D --> E["Check-in hằng kỳ"]
    E --> F["Đánh giá nhân sự"]
    F --> G["Dashboard / Report / AI Insights"]
```

### Lời thoại gợi ý

> Hệ thống được thiết kế cho môi trường doanh nghiệp nội bộ, nơi nhiều vai trò cùng tham gia vào quy trình quản trị hiệu suất. Director quan tâm mục tiêu chiến lược, Manager quan tâm vận hành KPI phòng ban, HR quan tâm dữ liệu nhân sự và đánh giá, còn Employee cần xem KPI được giao và check-in tiến độ. Tầm nhìn của hệ thống là biến KPI/OKR thành một quy trình số hóa liên tục, từ chiến lược đến thực thi và báo cáo.

---

## Slide 4 - Chức Năng Chính, Phụ Và Phi Chức Năng

**Người nói:** PO  
**Thời lượng:** 0.5 phút

### Nội dung trên slide

- Chức năng chính:
  - Mission/Vision, OKR, Key Result.
  - KPI, duyệt KPI, phân bổ KPI.
  - KPI Check-in, ReviewQueue.
  - Evaluation Result, Grading Rank, Bonus Rule.
  - Dashboard, EvaluationReports, Export Excel, AI Insights.
- Chức năng phụ:
  - Department, Position, Employee.
  - SystemUser, Role, Permission, AuditLog.
  - Quên mật khẩu OTP, Google OAuth, Notification.
- Phi chức năng:
  - Bảo mật theo role/permission.
  - Dữ liệu tập trung, truy vết được.
  - Giao diện dễ dùng, hỗ trợ dashboard.
  - Có thể triển khai IIS.

### Sơ đồ

```mermaid
mindmap
  root((VietMach KPI/OKR))
    Nền tảng
      User
      Role/Permission
      Department
      Employee
    OKR
      Mission/Vision
      OKR
      Key Result
    KPI
      KPI
      Assignment
      Check-in
      Review
    Evaluation
      Score
      Rank
      Bonus
    Report/AI
      Dashboard
      Excel
      Smart Alerts
```

### Lời thoại gợi ý

> Phạm vi dự án gồm cả nghiệp vụ chính và các module nền tảng. Chức năng lõi là OKR, KPI, check-in, duyệt, đánh giá và báo cáo. Các chức năng phụ như nhân sự, phòng ban, tài khoản và phân quyền giúp hệ thống vận hành đúng thực tế doanh nghiệp. Ngoài ra, hệ thống cũng chú trọng các yêu cầu phi chức năng như bảo mật, truy vết dữ liệu và khả năng triển khai.

---

## Slide 5 - Quy Trình Làm Việc Scrum

**Người nói:** SM  
**Thời lượng:** 1 phút

### Nội dung trên slide

- Nhóm áp dụng Scrum để chia nhỏ công việc.
- Vai trò trong nhóm:
  - PO: xác định yêu cầu và ưu tiên backlog.
  - SM: theo dõi sprint, tháo gỡ vướng mắc.
  - Frontend/Backend: phát triển tính năng.
  - Tester: kiểm tra nghiệp vụ và phân quyền.
- Vòng lặp:
  - Planning.
  - Development.
  - Review.
  - Testing.
  - Retrospective.

### Sơ đồ

```mermaid
flowchart LR
    A["Product Backlog"] --> B["Sprint Planning"]
    B --> C["Sprint Backlog"]
    C --> D["Development"]
    D --> E["Code Review"]
    E --> F["Testing"]
    F --> G{"Đạt Definition of Done?"}
    G -- "Chưa đạt" --> D
    G -- "Đạt" --> H["Sprint Review"]
    H --> I["Retrospective"]
    I --> A
```

### Lời thoại gợi ý

> Vì dự án có nhiều module liên quan với nhau, nhóm áp dụng Scrum để chia nhỏ công việc theo sprint. Product backlog chứa toàn bộ yêu cầu, sau đó nhóm chọn các yêu cầu ưu tiên vào sprint backlog. Trong mỗi sprint, frontend, backend và tester phối hợp để hoàn thành tính năng. Sau khi phát triển, tính năng được review, test và chỉ hoàn thành khi đạt Definition of Done.

---

## Slide 6 - Backlog, Trello, Git Và Sprint Backlog

**Người nói:** SM  
**Thời lượng:** 1 phút

### Nội dung trên slide

- Product backlog:
  - Auth, HR, OKR, KPI, Check-in, Evaluation, Report, AI.
- Sprint backlog:
  - Chia user story thành task frontend, backend, tester.
- Trello:
  - Backlog -> To Do -> In Progress -> Review -> Testing -> Done.
- Git:
  - Commit theo tính năng.
  - Theo dõi lịch sử sửa lỗi.
  - Tránh mất thay đổi khi nhiều người cùng làm.

### Sơ đồ

```mermaid
flowchart LR
    A["Backlog<br/>Yêu cầu chưa làm"] --> B["To Do<br/>Chọn cho sprint"]
    B --> C["In Progress<br/>Đang phát triển"]
    C --> D["Review<br/>Chờ review"]
    D --> E["Testing<br/>Tester kiểm tra"]
    E --> F["Done<br/>Hoàn thành"]
    E -- "Có lỗi" --> C
```

### Lời thoại gợi ý

> Backlog được chia theo module nghiệp vụ. Ví dụ, với Employee có story xem KPI và tạo check-in; với Manager có story duyệt check-in; với Director có story duyệt đánh giá cuối. Trello giúp nhóm theo dõi trạng thái từng task, còn Git giúp quản lý mã nguồn và lịch sử thay đổi. Cách làm này giúp nhóm kiểm soát tiến độ và giảm rủi ro khi nhiều người cùng phát triển.

---

## Slide 7 - Giám Sát Tiến Độ Và Definition Of Done

**Người nói:** SM  
**Thời lượng:** 1 phút

### Nội dung trên slide

- Giám sát tiến độ:
  - Cập nhật task hằng ngày.
  - Kiểm tra Trello và commit Git.
  - Demo nhanh sau mỗi module.
- Definition of Done:
  - Chạy được trên local.
  - Đúng nghiệp vụ.
  - Đúng phân quyền.
  - Có validation.
  - UI không vỡ layout.
  - Test case chính pass.
  - Không làm hỏng luồng cũ.

### Sơ đồ

```mermaid
flowchart TD
    A["Task hoàn thành code"] --> B{"Chạy được?"}
    B -- "Không" --> A
    B -- "Có" --> C{"Đúng nghiệp vụ?"}
    C -- "Không" --> A
    C -- "Có" --> D{"Đúng phân quyền?"}
    D -- "Không" --> A
    D -- "Có" --> E{"Tester pass?"}
    E -- "Không" --> A
    E -- "Có" --> F["Done"]
```

### Lời thoại gợi ý

> Với hệ thống KPI/OKR, hoàn thành không chỉ là code xong. Một task phải chạy được, đúng nghiệp vụ, đúng phân quyền và được tester kiểm tra. Đặc biệt, phân quyền là tiêu chí quan trọng vì hệ thống có nhiều vai trò. Nếu Employee nhìn thấy dữ liệu quản trị hoặc Manager tự duyệt check-in của chính mình thì đó là lỗi nghiệp vụ nghiêm trọng.

---

## Slide 8 - Use Case Và Tác Nhân Hệ Thống

**Người nói:** Luồng hệ thống  
**Thời lượng:** 1 phút

### Nội dung trên slide

- Tác nhân:
  - Admin.
  - Director.
  - Manager.
  - HR.
  - Employee/Sales.
- Use case chính:
  - Đăng nhập, đổi mật khẩu.
  - Quản lý nhân sự, phòng ban, tài khoản.
  - Quản lý OKR, KPI.
  - Check-in KPI.
  - Duyệt check-in và evaluation.
  - Xem dashboard, report, export Excel.
  - AI Chat, Suggest KPI, Smart Alerts.

### Sơ đồ

```mermaid
flowchart LR
    Admin((Admin))
    Director((Director))
    Manager((Manager))
    HR((HR))
    Employee((Employee/Sales))

    UC1["Auth"]
    UC2["Role/User/Employee"]
    UC3["Mission/OKR/KR"]
    UC4["KPI/Assignment"]
    UC5["Check-in"]
    UC6["Review/Evaluation"]
    UC7["Dashboard/Report"]
    UC8["AI Insights"]

    Admin --> UC1
    Admin --> UC2
    Admin --> UC3
    Admin --> UC4
    Admin --> UC5
    Admin --> UC6
    Admin --> UC7
    Admin --> UC8

    Director --> UC3
    Director --> UC4
    Director --> UC6
    Director --> UC7
    Director --> UC8

    Manager --> UC3
    Manager --> UC4
    Manager --> UC5
    Manager --> UC6
    Manager --> UC7
    Manager --> UC8

    HR --> UC2
    HR --> UC6
    HR --> UC7
    HR --> UC8

    Employee --> UC5
    Employee --> UC7
    Employee --> UC8
```

### Lời thoại gợi ý

> Hệ thống có nhiều tác nhân với phạm vi khác nhau. Admin quản trị toàn bộ, Director theo dõi mục tiêu chiến lược và duyệt kết quả cuối, Manager vận hành KPI phòng ban, HR quản lý nhân sự và báo cáo, còn Employee/Sales chủ yếu xem KPI được giao và check-in tiến độ. Điểm quan trọng là cùng một module nhưng mỗi vai trò có quyền và phạm vi dữ liệu khác nhau.

---

## Slide 9 - Workflow Nghiệp Vụ End-To-End

**Người nói:** Luồng hệ thống  
**Thời lượng:** 1.5 phút

### Nội dung trên slide

- Luồng chính:
  - Mission/Vision -> OKR -> Key Result.
  - Key Result -> KPI theo kỳ.
  - KPI được duyệt -> phân bổ cho phòng ban/nhân viên.
  - Employee check-in -> Manager/Director/HR/Admin review.
  - Approved -> tính score, rank, bonus, EvaluationResult.
  - Director review -> Dashboard/Report/AI.
- Nếu Rejected:
  - Không tính điểm chính thức.
  - Có thể rà soát và gửi lại.

### Sơ đồ

```mermaid
flowchart TD
    A["Mission / Vision"] --> B["OKR"]
    B --> C["Key Result"]
    C --> D["KPI theo kỳ đánh giá"]
    D --> E{"KPI được duyệt?"}
    E -- "Rejected" --> E1["Không cho check-in KPI này"]
    E -- "Approved" --> F["Phân bổ KPI<br/>phòng ban / nhân viên / trọng số"]
    F --> G["Employee/Sales check-in"]
    G --> H{"Check-in được duyệt?"}
    H -- "Rejected" --> I["Không tính điểm chính thức"]
    H -- "Approved" --> J["Cập nhật tiến độ KPI"]
    J --> K["Tính total score"]
    K --> L["Map Grading Rank"]
    L --> M["Tính Expected Bonus"]
    L --> N["Tạo/Cập nhật EvaluationResult"]
    N --> O["Manager/Admin gửi Director review"]
    O --> P{"Director/Admin quyết định"}
    P -- "Rejected" --> Q["Trả về rà soát"]
    Q --> O
    P -- "Approved" --> R["Dashboard / Reports / AI Insights"]
```

### Lời thoại gợi ý

> Đây là luồng quan trọng nhất của hệ thống. Doanh nghiệp bắt đầu bằng Mission/Vision, sau đó tạo OKR và Key Result. Từ đó, KPI được tạo theo kỳ đánh giá và phải được duyệt trước khi phân bổ. Khi Employee check-in, dữ liệu đi vào trạng thái chờ duyệt. Nếu được approve, hệ thống mới cập nhật tiến độ, tính điểm, xếp loại, thưởng dự kiến và tạo EvaluationResult. Cuối cùng, Manager gửi lên Director để chốt đánh giá và dữ liệu xuất hiện trên dashboard, report và AI Insights.

---

## Slide 10 - UI Flow Theo Vai Trò

**Người nói:** Luồng hệ thống  
**Thời lượng:** 1 phút

### Nội dung trên slide

- Director:
  - Login -> Dashboard -> OKRs -> EvaluationReports -> ReviewBoard.
- Manager:
  - Login -> KPIs -> AllocatePersonnel -> ReviewQueue -> EvaluationResults.
- Employee/Sales:
  - Login -> Dashboard cá nhân -> KPIs -> KPICheckIns/Create.
- HR:
  - Login -> Employees -> EvaluationPeriods -> BonusRules -> Reports.
- Admin:
  - Login -> Roles -> SystemUsers -> Catalog -> AuditLogs.

### Sơ đồ

```mermaid
flowchart TD
    Login["/Auth/Login"] --> Role{"Vai trò"}

    Role --> Director["Director"]
    Director --> D1["/Dashboard"]
    D1 --> D2["/OKRs"]
    D2 --> D3["/EvaluationReports"]
    D3 --> D4["/EvaluationResults/ReviewBoard"]

    Role --> Manager["Manager"]
    Manager --> M1["/KPIs"]
    M1 --> M2["/KPIs/AllocatePersonnel"]
    M2 --> M3["/KPICheckIns/ReviewQueue"]
    M3 --> M4["/EvaluationResults"]

    Role --> Employee["Employee/Sales"]
    Employee --> E1["/Dashboard"]
    E1 --> E2["/KPIs"]
    E2 --> E3["/KPICheckIns/Create"]

    Role --> HR["HR"]
    HR --> H1["/Employees"]
    H1 --> H2["/EvaluationPeriods"]
    H2 --> H3["/BonusRules"]
    H3 --> H4["/EvaluationReports"]

    Role --> Admin["Admin"]
    Admin --> A1["/Roles"]
    A1 --> A2["/SystemUsers"]
    A2 --> A3["/Catalog"]
    A3 --> A4["/AuditLogs"]
```

### Lời thoại gợi ý

> UI flow được thiết kế theo vai trò. Director đi từ dashboard đến OKR và báo cáo. Manager tập trung vào KPI, phân bổ và hàng chờ duyệt check-in. Employee chỉ cần xem KPI được giao và check-in. HR quản lý dữ liệu nhân sự, kỳ đánh giá và báo cáo. Admin quản lý tài khoản, phân quyền và audit. Cách điều hướng này giúp giảm thao tác thừa và tránh người dùng truy cập nhầm chức năng.

---

## Slide 11 - Phân Rã Chức Năng Và Cấu Trúc Phân Tầng

**Người nói:** Luồng hệ thống  
**Thời lượng:** 0.5 phút

### Nội dung trên slide

- Nhóm chức năng:
  - Foundation: Auth, Role, Permission, User.
  - Organization: Department, Position, Employee.
  - OKR: Mission/Vision, OKR, Key Result.
  - KPI: KPI, Detail, Assignment, Approval.
  - Execution: Check-in, ReviewQueue.
  - Evaluation: Score, Rank, Bonus, Director Review.
  - Report/AI: Dashboard, Excel, AI Insights.
- Phân tầng:
  - Presentation -> Controller -> Service/Helper -> EF Core -> SQL Server.

### Sơ đồ

```mermaid
flowchart TD
    Root["VietMach KPI/OKR System"]
    Root --> F["Foundation<br/>Auth, Role, Permission, User"]
    Root --> O["Organization<br/>Department, Position, Employee"]
    Root --> S["Strategy<br/>Mission/Vision, OKR, KR"]
    Root --> K["KPI<br/>KPI, Detail, Assignment, Approval"]
    Root --> C["Execution<br/>Check-in, ReviewQueue"]
    Root --> E["Evaluation<br/>Score, Rank, Bonus"]
    Root --> R["Report & AI<br/>Dashboard, Excel, Gemini"]

    P["Presentation Layer"] --> CL["Controller Layer"]
    CL --> SV["Service/Helper Layer"]
    SV --> DA["EF Core / DbContext"]
    DA --> DB["SQL Server"]
```

### Lời thoại gợi ý

> Nếu nhìn theo chức năng, hệ thống được chia thành các nhóm từ nền tảng, tổ chức, OKR, KPI, check-in, evaluation đến report và AI. Nếu nhìn theo kỹ thuật, hệ thống đi theo cấu trúc phân tầng: giao diện Razor Views, controller xử lý request, service/helper xử lý nghiệp vụ, EF Core truy cập dữ liệu và SQL Server lưu trữ.

---

## Slide 12 - Phong Cách Thiết Kế Frontend

**Người nói:** Frontend  
**Thời lượng:** 1 phút

### Nội dung trên slide

- Phong cách:
  - Enterprise dashboard.
  - Chuyên nghiệp, rõ ràng, ưu tiên dữ liệu.
- Màu sắc:
  - Xanh dương doanh nghiệp.
  - Nền sáng.
  - Sidebar xanh đậm.
- Thành phần:
  - Sidebar theo permission.
  - Header, notification.
  - Dashboard cards.
  - Data tables.
  - Forms, modals, alerts.
  - Charts và AI widget.
- Thư viện:
  - Razor Views, Bootstrap, jQuery, Select2, Chart.js, SweetAlert2.

### Sơ đồ

```mermaid
flowchart TD
    A["App Layout"]
    A --> B["Sidebar<br/>menu theo permission"]
    A --> C["Header<br/>user + notification"]
    A --> D["Main Content"]
    D --> E["Dashboard Cards"]
    D --> F["Charts"]
    D --> G["Data Tables"]
    D --> H["Forms"]
    D --> I["Modals / Alerts"]
    D --> J["AI Chat Widget"]
```

### Lời thoại gợi ý

> Frontend được thiết kế theo phong cách enterprise dashboard vì hệ thống phục vụ quản lý nội bộ và có nhiều dữ liệu. Màu xanh dương tạo cảm giác chuyên nghiệp, sidebar giúp điều hướng nhanh giữa các module, còn dashboard cards và biểu đồ giúp người dùng nắm tình hình nhanh. Các menu cũng hiển thị theo permission để mỗi vai trò chỉ thấy chức năng phù hợp.

---

## Slide 13 - Giao Diện Hệ Thống Chính

**Người nói:** Frontend  
**Thời lượng:** 1.25 phút

### Nội dung trên slide

- Dashboard:
  - Tổng quan KPI/OKR/check-in theo kỳ.
  - Biểu đồ trạng thái, xu hướng, top hiệu suất.
- OKR:
  - Danh sách OKR, Key Result, tiến độ.
  - Gắn Mission/Vision, phòng ban hoặc nhân viên.
- KPI:
  - Tạo, duyệt/từ chối, phân bổ KPI.
  - Liên kết OKR/Key Result.
- Check-in:
  - Employee nhập giá trị thực đạt.
  - ReviewQueue cho người có quyền duyệt.
- EvaluationReports:
  - Kết quả, rank, bonus.
  - Export Excel.

### Sơ đồ

```mermaid
flowchart LR
    A["Login"] --> B["Dashboard"]
    B --> C["OKRs"]
    B --> D["KPIs"]
    D --> E["KPI Details"]
    E --> F["Allocate Personnel"]
    D --> G["KPI Check-in"]
    G --> H["ReviewQueue"]
    H --> I["EvaluationResults"]
    I --> J["ReviewBoard"]
    J --> K["EvaluationReports"]
    K --> L["Export Excel"]
```

### Lời thoại gợi ý

> Các màn hình chính được thiết kế theo luồng nghiệp vụ. Dashboard là nơi xem tổng quan. OKR thể hiện mục tiêu chiến lược. KPI là nơi tạo, duyệt và phân bổ chỉ tiêu. Check-in là nơi nhân viên cập nhật tiến độ, còn ReviewQueue là nơi quản lý xác nhận dữ liệu. Cuối cùng, EvaluationReports giúp tổng hợp kết quả và xuất Excel phục vụ báo cáo.

---

## Slide 14 - UX, Trạng Thái Và Kiểm Soát Lỗi

**Người nói:** Frontend  
**Thời lượng:** 0.75 phút

### Nội dung trên slide

- UX theo trạng thái:
  - `Pending`: chờ duyệt.
  - `Approved`: đã xác nhận.
  - `Rejected`: bị từ chối.
  - `Draft`: bản nháp đánh giá.
  - `PendingDirectorReview`: chờ Director duyệt.
- Kiểm soát lỗi:
  - Validation form.
  - Alert khi thao tác thành công/thất bại.
  - AccessDenied khi thiếu quyền.
  - Notification nhắc người dùng.
- Mục tiêu:
  - Người dùng biết mình đang ở bước nào.
  - Giảm thao tác nhầm.
  - Giảm nhập sai dữ liệu.

### Sơ đồ

```mermaid
stateDiagram-v2
    [*] --> Pending: Employee gửi check-in
    Pending --> Approved: Manager/Director/HR/Admin duyệt
    Pending --> Rejected: Từ chối
    Approved --> OfficialScore: Tính điểm chính thức
    Rejected --> Ignored: Không tính vào đánh giá
    OfficialScore --> [*]
    Ignored --> [*]
```

### Lời thoại gợi ý

> Do hệ thống có nhiều trạng thái nghiệp vụ, frontend cần hiển thị rõ để người dùng không bị nhầm. Ví dụ, check-in của Employee sẽ là Pending cho đến khi được duyệt. Evaluation có Draft và PendingDirectorReview để biết đánh giá đang ở bước nào. Ngoài ra, các form có validation, các thao tác có alert và người dùng thiếu quyền sẽ bị chuyển sang AccessDenied.

---

## Slide 15 - Kiến Trúc Kỹ Thuật Backend

**Người nói:** Backend  
**Thời lượng:** 2 phút

### Nội dung trên slide

- Công nghệ:
  - ASP.NET Core MVC, .NET 10.
  - Entity Framework Core 10.
  - SQL Server.
  - Cookie Authentication, Google OAuth.
  - EPPlus export Excel.
  - Gemini API.
- Luồng request:
  - Browser -> Middleware -> Controller -> Service/Helper -> MiniERPDbContext -> SQL Server.
- Service chính:
  - GeminiService.
  - AIDataService.
  - AIAlertService.
  - EmailService.
  - NotificationService.
  - OKRProgressService.
- Bảo mật:
  - Cookie HttpOnly.
  - Antiforgery token.
  - Data Protection keys.
  - Environment config.

### Sơ đồ

```mermaid
flowchart LR
    User["Browser"] --> App["ASP.NET Core MVC"]
    App --> Middleware["Middleware<br/>HTTPS, Routing, Auth, Antiforgery"]
    Middleware --> Controllers["Controllers<br/>Auth, Dashboard, OKR, KPI, Check-in, Evaluation, AI"]
    Controllers --> Views["Razor Views"]
    Controllers --> Services["Services<br/>Email, Gemini, AIData, Notification, OKRProgress"]
    Controllers --> Helpers["Helpers<br/>Permission, Progress, Password, Encryption"]
    Controllers --> DbContext["MiniERPDbContext"]
    Services --> DbContext
    Helpers --> DbContext
    DbContext --> SQL["SQL Server"]
    Services --> Gemini["Gemini API"]
    Services --> SMTP["SMTP Email"]
```

```mermaid
sequenceDiagram
    actor User
    participant Browser
    participant Middleware
    participant Controller
    participant Service
    participant Db as MiniERPDbContext
    participant SQL as SQL Server
    participant View as Razor View

    User->>Browser: Thao tác giao diện
    Browser->>Middleware: HTTP request
    Middleware->>Controller: Đã qua auth/antiforgery
    Controller->>Service: Xử lý nghiệp vụ
    Service->>Db: Truy vấn/cập nhật entity
    Db->>SQL: SQL query
    SQL-->>Db: Kết quả
    Db-->>Service: Data
    Service-->>Controller: Result
    Controller->>View: Render model
    View-->>Browser: HTML/CSS/JS
```

### Lời thoại gợi ý

> Backend dùng ASP.NET Core MVC trên .NET 10, Entity Framework Core và SQL Server. Request từ trình duyệt đi qua middleware như routing, authentication, authorization và antiforgery, sau đó vào controller. Controller gọi service hoặc helper để xử lý nghiệp vụ, rồi dùng MiniERPDbContext truy cập SQL Server. Các service như GeminiService, AIDataService, NotificationService và OKRProgressService giúp tách riêng những phần nghiệp vụ phức tạp.

---

## Slide 16 - ERD, Phân Quyền Và AI

**Người nói:** Backend  
**Thời lượng:** 2 phút

### Nội dung trên slide

- Nhóm bảng chính:
  - Foundation: Role, Permission, SystemUser, Employee, Department.
  - OKR: MissionVision, OKR, OKRKeyResult, allocations.
  - KPI: EvaluationPeriod, KPI, KPIDetail, assignments.
  - Check-in: KPICheckIn, CheckInDetail, CheckInStatus, HistoryLog.
  - Evaluation: EvaluationResult, GradingRank, BonusRule, RealtimeExpectedBonus.
  - System/AI: AuditLog, SystemAlert, AIGenerationHistory.
- Phân quyền:
  - `[Authorize]` yêu cầu đăng nhập.
  - `[HasPermission("PERMISSION_CODE")]` kiểm tra quyền.
  - Role_Permission lưu mapping quyền.
- AI:
  - Gemini API.
  - AI chỉ dùng dữ liệu trong phạm vi quyền.
  - Có fallback khi thiếu API key hoặc lỗi Gemini.

### Sơ đồ ERD rút gọn

```mermaid
erDiagram
    ROLE ||--o{ SYSTEM_USER : has
    ROLE ||--o{ ROLE_PERMISSION : grants
    PERMISSION ||--o{ ROLE_PERMISSION : included_in
    SYSTEM_USER ||--|| EMPLOYEE : maps_to
    DEPARTMENT ||--o{ EMPLOYEE_ASSIGNMENT : contains
    POSITION ||--o{ EMPLOYEE_ASSIGNMENT : assigned_as
    EMPLOYEE ||--o{ EMPLOYEE_ASSIGNMENT : works_in

    MISSION_VISION ||--o{ OKR_MISSION_MAPPING : links
    OKR ||--o{ OKR_MISSION_MAPPING : maps_to
    OKR ||--o{ OKR_KEY_RESULT : contains
    OKR ||--o{ OKR_DEPARTMENT_ALLOCATION : allocated_to
    OKR ||--o{ OKR_EMPLOYEE_ALLOCATION : assigned_to

    EVALUATION_PERIOD ||--o{ KPI : contains
    OKR ||--o{ KPI : aligns_with
    OKR_KEY_RESULT ||--o{ KPI : supports
    KPI ||--o{ KPI_DETAIL : has
    KPI ||--o{ KPI_EMPLOYEE_ASSIGNMENT : assigned_to
    EMPLOYEE ||--o{ KPI_EMPLOYEE_ASSIGNMENT : receives

    KPI ||--o{ KPI_CHECK_IN : checked_in
    EMPLOYEE ||--o{ KPI_CHECK_IN : submits
    CHECK_IN_STATUS ||--o{ KPI_CHECK_IN : status
    KPI_CHECK_IN ||--o{ CHECK_IN_DETAIL : has
    KPI_CHECK_IN ||--o{ CHECK_IN_HISTORY_LOG : logs

    EMPLOYEE ||--o{ EVALUATION_RESULT : evaluated
    EVALUATION_PERIOD ||--o{ EVALUATION_RESULT : period
    GRADING_RANK ||--o{ EVALUATION_RESULT : ranks
    GRADING_RANK ||--o{ BONUS_RULE : maps_bonus
    EMPLOYEE ||--o{ REALTIME_EXPECTED_BONUS : earns
```

### Sơ đồ phân quyền và AI

```mermaid
flowchart TD
    A["User đăng nhập"] --> B["Cookie Authentication"]
    B --> C["Claims: UserId, Role, Email"]
    C --> D["PermissionClaimsTransformation"]
    D --> E["Role_Permission trong DB"]
    E --> F["Permission claims"]
    F --> G{"Action có HasPermission?"}
    G -- "Không" --> H["Cho xử lý nếu đã đăng nhập"]
    G -- "Có" --> I{"Có permission?"}
    I -- "Có" --> J["Xử lý request"]
    I -- "Không" --> K["Forbid / AccessDenied"]

    J --> L["AIDataService gom dữ liệu theo quyền"]
    L --> M["GeminiService"]
    M --> N["Gemini API"]
    M --> O["Fallback nếu lỗi"]
```

### Lời thoại gợi ý

> Database được chia thành các nhóm bảng theo nghiệp vụ: foundation, OKR, KPI, check-in, evaluation và system/AI. Các bảng liên kết như Role_Permission, KPI_Employee_Assignment hoặc OKR allocation giúp hệ thống quản lý quan hệ nhiều-nhiều. Về phân quyền, backend kiểm tra bằng Authorize và HasPermission nên người dùng không thể chỉ gọi URL trực tiếp để vượt quyền. AI được tích hợp qua Gemini nhưng dữ liệu đưa vào AI luôn được lọc theo phạm vi quyền của người dùng.

---

## Slide 17 - Test Plan Và Luồng Kiểm Thử

**Người nói:** Tester  
**Thời lượng:** 2 phút

### Nội dung trên slide

- Mục tiêu kiểm thử:
  - Đúng workflow KPI/OKR.
  - Đúng role/permission.
  - Đúng tính score, rank, bonus.
  - Dashboard/report phản ánh dữ liệu sau duyệt.
  - UI rõ trạng thái và không lỗi form chính.
- Nhóm test:
  - Functional test.
  - Role/permission test.
  - Workflow test.
  - UI/UX test.
  - Report/export test.
  - AI/fallback test.
  - Regression test.

### Sơ đồ

```mermaid
flowchart TD
    A["Test Strategy"]
    A --> B["Functional Test<br/>Auth, OKR, KPI, Check-in, Evaluation"]
    A --> C["Role/Permission Test<br/>Admin, Director, Manager, HR, Employee"]
    A --> D["Workflow Test<br/>Mission -> OKR -> KPI -> Check-in -> Review"]
    A --> E["UI/UX Test<br/>Form, table, sidebar, alert"]
    A --> F["Report/Export Test<br/>Dashboard, EvaluationReports, Excel"]
    A --> G["AI/Fallback Test<br/>Gemini success/fail"]
    A --> H["Regression Test<br/>Không làm hỏng luồng cũ"]
```

```mermaid
flowchart LR
    A["Chuẩn bị seed/test data"] --> B["Đăng nhập từng role"]
    B --> C["Mở OKR/KPI"]
    C --> D["Phân bổ KPI"]
    D --> E["Employee check-in"]
    E --> F["Manager duyệt"]
    F --> G["Kiểm tra score/rank/bonus"]
    G --> H["Director review"]
    H --> I["Kiểm tra dashboard/report/export"]
```

### Lời thoại gợi ý

> Phần kiểm thử tập trung vào các rủi ro lớn: sai nghiệp vụ, sai phân quyền và sai tính toán. Tester kiểm tra từng module như Auth, OKR, KPI, Check-in, Evaluation và Report. Đặc biệt, nhóm test theo luồng end-to-end từ tạo mục tiêu, giao KPI, Employee check-in, Manager duyệt, hệ thống tính điểm, Director review và báo cáo cập nhật. Đây là cách đảm bảo hệ thống đáng tin cậy cho đánh giá nhân sự.

---

## Slide 18 - Test Case Quan Trọng Và Kết Luận

**Người nói:** Tester  
**Thời lượng:** 1 phút

### Nội dung trên slide

| Test case | Kết quả mong đợi |
| --- | --- |
| Employee đăng nhập | Chỉ thấy dữ liệu cá nhân/KPI được giao |
| Employee tạo check-in | Trạng thái là `Pending` |
| Manager duyệt check-in | Check-in `Approved`, cập nhật score/rank/bonus |
| Manager tự duyệt check-in của mình | Bị chặn theo nghiệp vụ |
| Director duyệt EvaluationResult | Trạng thái chuyển `Approved` |
| Role thiếu quyền truy cập module | Bị chặn hoặc vào `AccessDenied` |
| Export Excel báo cáo | Tải được file đúng kỳ đánh giá |
| AI thiếu API key | Có fallback, không ảnh hưởng luồng chính |

### Sơ đồ kết luận

```mermaid
flowchart LR
    A["Chiến lược"] --> B["OKR"]
    B --> C["KPI"]
    C --> D["Check-in"]
    D --> E["Review"]
    E --> F["Score / Rank / Bonus"]
    F --> G["Evaluation"]
    G --> H["Dashboard / Report / AI"]
```

### Lời thoại gợi ý

> Các test case quan trọng nhất xoay quanh phân quyền, check-in, duyệt và tính điểm. Employee chỉ được thấy dữ liệu của mình, check-in phải chờ duyệt, Manager chỉ duyệt đúng phạm vi, Director chốt đánh giá cuối và báo cáo phải phản ánh kết quả. Kết luận lại, VietMach KPI/OKR System không chỉ là nơi nhập KPI, mà là hệ thống quản lý trọn vòng đời từ chiến lược, thực thi, phê duyệt, đánh giá đến báo cáo và AI Insights.

---

## Checklist Trước Khi Đưa Vào PowerPoint

- Tổng thời lượng đúng **20 phút**.
- Mỗi vai trò có phần nói rõ:
  - PO: 3 phút.
  - SM: 3 phút.
  - Luồng hệ thống: 4 phút.
  - Frontend: 3 phút.
  - Backend: 4 phút.
  - Tester: 3 phút.
- Các sơ đồ bắt buộc đã có:
  - Problem/Solution.
  - Scrum workflow.
  - Trello board.
  - Use case.
  - Workflow end-to-end.
  - UI flow.
  - Functional decomposition.
  - Layout frontend.
  - Architecture.
  - Request lifecycle.
  - ERD.
  - Permission/AI.
  - Test strategy.
  - Test flow.
- Khi chuyển sang PowerPoint, nên đưa mỗi sơ đồ lớn lên một slide riêng hoặc giữ đúng slide đã chia ở trên.
- Nếu sơ đồ Mermaid quá nhiều chữ, hãy giữ sơ đồ trong tài liệu và dùng bản rút gọn trên slide trình chiếu.

