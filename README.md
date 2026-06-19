# 📊 Hệ Thống Quản Lý KPI/OKR Doanh Nghiệp

> Ứng dụng web quản lý hiệu suất doanh nghiệp toàn diện, tích hợp AI (Gemini), xây dựng trên nền tảng ASP.NET 10 MVC + Entity Framework Core + SQL Server.

## 📋 Mục Lục

- [Tổng Quan](#-tổng-quan)
- [Kiến Trúc Hệ Thống](#-kiến-trúc-hệ-thống)
- [Công Nghệ Sử Dụng](#-công-nghệ-sử-dụng)
- [Các Module Chức Năng](#-các-module-chức-năng)
- [Phân Quyền RBAC](#-phân-quyền-rbac)
- [Tích Hợp AI](#-tích-hợp-ai-gemini)
- [Cơ Sở Dữ Liệu](#-cơ-sở-dữ-liệu)
- [Cài Đặt & Chạy](#-cài-đặt--chạy)
- [Tài Khoản Demo](#-tài-khoản-demo)
- [Cấu Trúc Thư Mục](#-cấu-trúc-thư-mục)

---

## 🎯 Tổng Quan

Hệ thống quản lý KPI/OKR là giải pháp **end-to-end** cho doanh nghiệp, bao gồm:

- **Thiết lập chiến lược**: Sứ mệnh, Tầm nhìn, Mục tiêu chiến lược hàng năm
- **Quản lý OKR**: Objectives & Key Results theo cấp Công ty → Phòng ban → Cá nhân
- **Quản lý KPI**: Giao chỉ tiêu, theo dõi tiến độ, check-in định kỳ
- **Đánh giá hiệu suất**: Xếp hạng tự động (S/A+/A/B+/B/C/D), tính thưởng
- **AI hỗ trợ**: Chatbot tư vấn, gợi ý KPI, phân tích hiệu suất, cảnh báo rủi ro
- **Thông báo & Nhắc nhở**: Deadline check-in, cảnh báo AI, email SMTP

---

## 🏗 Kiến Trúc Hệ Thống

```
┌─────────────────────────────────────────────────┐
│                   Browser (UI)                  │
│         Razor Views + Bootstrap + JS            │
├─────────────────────────────────────────────────┤
│              ASP.NET 10 MVC                     │
│  ┌──────────┐ ┌──────────┐ ┌──────────────────┐│
│  │Controllers│ │  Filters │ │    Helpers       ││
│  │ (22 file) │ │Permission│ │AccessScope/SEO/  ││
│  │           │ │Attribute │ │Progress/Workflow ││
│  └──────────┘ └──────────┘ └──────────────────┘│
├─────────────────────────────────────────────────┤
│                 Services Layer                  │
│  ┌────────────┐ ┌───────────┐ ┌──────────────┐ │
│  │GeminiService│ │AIDataSvc  │ │Notification  │ │
│  │(Gemini API) │ │AIAlertSvc │ │EmailService  │ │
│  └────────────┘ └───────────┘ └──────────────┘ │
├─────────────────────────────────────────────────┤
│          Entity Framework Core 10               │
│          MiniERPDbContext (45 entities)          │
├─────────────────────────────────────────────────┤
│              SQL Server Database                │
└─────────────────────────────────────────────────┘
```

**Design Patterns**: MVC, Repository qua DbContext, Claims-based Auth, RBAC via DB, Background Services.

---

## 🛠 Công Nghệ Sử Dụng

| Thành phần | Công nghệ | Phiên bản |
|---|---|---|
| **Framework** | ASP.NET Core MVC | .NET 10.0 |
| **ORM** | Entity Framework Core | 10.0.5 |
| **Database** | SQL Server | 2019+ |
| **AI Engine** | Google Gemini API | gemini-2.5-flash |
| **Auth** | Cookie + Google OAuth2 | — |
| **Email** | SMTP (Gmail) | — |
| **Export** | EPPlus (Excel) | 7.7.3 |
| **Frontend** | Bootstrap 5 + Vanilla JS | — |
| **Charts** | ApexCharts.js | — |
| **Env Config** | DotNetEnv | 3.1.1 |

---

## 📦 Các Module Chức Năng

### Module 1-2: Nền Tảng & Tổ Chức

| Chức năng | Mô tả |
|---|---|
| **Vai trò (Roles)** | Admin, Director, Manager, HR, Employee — phân quyền chi tiết 60 permissions |
| **Tài khoản (SystemUsers)** | Đăng nhập/đăng ký, Google OAuth, quên mật khẩu (OTP email), đổi mật khẩu |
| **Nhân viên (Employees)** | CRUD, import Excel, auto-gen mã (EMP001), gán phòng ban/chức vụ |
| **Phòng ban (Departments)** | Cây phòng ban phân cấp, gán quản lý, 12 phòng ban demo |
| **Chức vụ (Positions)** | 12 chức danh với RankLevel, auto-gen mã |
| **Tham số hệ thống** | Cấu hình động: tần suất check-in, max KPI/OKR, ngưỡng đạt |

### Module 3: OKR & Mục Tiêu Chiến Lược

- **Sứ mệnh/Tầm nhìn**: Vision, Mission, Yearly Goals với mục tiêu tài chính
- **OKR**: 3 cấp (Công ty/Phòng ban/Cá nhân), gắn Key Results
- **Key Results**: Giá trị mục tiêu/thực tế, đơn vị đo, hỗ trợ chỉ số nghịch (IsInverse)
- **Liên kết**: OKR ↔ Mission, OKR ↔ Phòng ban, OKR ↔ Nhân viên
- **Tiến độ tự động**: Tính % hoàn thành dựa trên Key Results

### Module 4: KPI Setup

- **KPI**: Gắn kỳ đánh giá, loại (Định lượng/Định tính/Hành vi), thuộc tính (Tăng trưởng/Ổn định/Giảm thiểu)
- **KPI Detail**: Target, Pass/Fail Threshold, đơn vị đo, tần suất check-in (ngày), deadline time, reminder
- **Giao KPI**: Theo phòng ban hoặc cá nhân, có trọng số (weight)
- **Liên kết OKR**: KPI gắn với OKR và Key Result cụ thể
- **Workflow**: Bản nháp → Chờ duyệt → Đang thực hiện → Hoàn thành/Không đạt

### Module 5: Check-in & Thực Thi

- **KPI Check-in**: Nhân viên báo cáo tiến độ định kỳ, ghi nhận giá trị đạt được
- **Auto-calculate**: Tiến độ %, giá trị kỳ vọng tại deadline, tiến độ theo lịch
- **Review Queue**: Manager/Director duyệt check-in (Approve/Reject), chấm điểm, nhận xét
- **Employee Tracking**: Dashboard theo dõi tiến độ KPI của nhân viên trong phạm vi quản lý
- **Goal Comments**: Bình luận và đánh giá trên từng KPI/check-in
- **1-on-1 Meetings**: Lên lịch họp riêng Manager-Employee
- **Nhắc nhở tự động**: Cảnh báo deadline sắp đến và quá hạn check-in

### Module 6: Đánh Giá & HR

- **Kỳ đánh giá**: Quý/Năm, trạng thái Mở/Đóng/Đang xử lý
- **KPI Result Comparison**: So sánh target vs achieved, tính % hoàn thành
- **Evaluation Results**: Tổng điểm, xếp hạng (S→D), workflow Draft → Submitted → Director Reviewed
- **Grading Ranks**: 7 bậc (S/A+/A/B+/B/C/D) với ngưỡng điểm
- **Bonus Rules**: Quy tắc thưởng theo rank (% lương + cố định)
- **Realtime Expected Bonus**: Dự toán thưởng theo kỳ
- **Báo cáo**: Export Excel (EPPlus), báo cáo tổng hợp theo phòng ban

### Dashboard & Báo Cáo

- **Tổng quan**: Thống kê KPI/OKR/nhân viên, biểu đồ ApexCharts (Line, Donut, Bar)
- **Tìm kiếm toàn cục**: Search Controller tìm kiếm across entities
- **Audit Logs**: Nhật ký thao tác hệ thống
- **SEO**: Meta tags, sitemap.xml, robots.txt, canonical URLs

---

## 🔐 Phân Quyền RBAC

Hệ thống sử dụng **Role-Based Access Control** kết hợp **Permission-based** authorization:

```
Authentication Flow:
  Cookie Auth → ClaimsTransformation → Permission Claims injection
  Google OAuth → External login → Cookie Auth

Authorization Flow:
  [HasPermission("KPIS_VIEW")] → HasPermissionFilter
    → Admin bypass (toàn quyền)
    → HR default permissions
    → DB lookup: Role → Role_Permission → Permission
```

### Ma Trận Quyền Theo Vai Trò

| Vai trò | Phạm vi dữ liệu | Quyền chính |
|---|---|---|
| **Admin** | Toàn hệ thống | Toàn quyền (60 permissions) |
| **Director** | Toàn công ty | OKR/KPI full, Mission full, Đánh giá + Duyệt, Báo cáo |
| **Manager** | Phòng ban quản lý | OKR/KPI (View+Create+Edit), Check-in Review, Đánh giá |
| **HR** | Toàn bộ nhân sự | Employees full, Kỳ đánh giá, Bonus, Báo cáo |
| **Employee** | Cá nhân | Xem KPI/OKR, Check-in, Xem đánh giá |

### Data Scope (AccessScopeHelper)

- **Admin/Director**: Xem toàn bộ dữ liệu
- **Manager**: Xem dữ liệu phòng ban mình quản lý + cá nhân
- **Employee/Sales**: Chỉ xem dữ liệu được giao trực tiếp hoặc qua phòng ban

---

## 🤖 Tích Hợp AI (Gemini)

### Kiến Trúc AI Services

```
AIController (API endpoints)
  ├── GeminiService         → Gọi Gemini API (rate limit 15/min, 1500/day)
  ├── AIDataService         → Build context data theo role scope
  │   ├── .Suggestions      → Gợi ý KPI thông minh
  │   ├── .Performance      → Phân tích hiệu suất
  │   ├── .CustomerSegments → Phân khúc khách hàng
  │   ├── .Alerts           → Phát hiện rủi ro
  │   └── .Helpers          → Utility functions
  ├── AIAlertService        → Smart alerts (AI + rule-based fallback)
  └── AIHistoryCleanupService → Background cleanup (30 ngày mặc định)
```

### Tính Năng AI

| Tính năng | Mô tả |
|---|---|
| **AI Chat Widget** | Chatbot tư vấn KPI/OKR, context-aware theo dữ liệu thực |
| **Gợi ý KPI** | Đề xuất KPI phù hợp theo OKR, phòng ban, nhân viên |
| **Phân tích hiệu suất** | Đánh giá performance theo kỳ, phòng ban, cá nhân |
| **Phân khúc khách hàng** | Gợi ý customer segments cho Sales |
| **AI Review** | Hỗ trợ viết nhận xét đánh giá |
| **Smart Alerts** | Cảnh báo rủi ro dựa trên dữ liệu KPI thực tế |
| **Lịch sử AI** | Lưu trữ và tái sử dụng kết quả AI, auto-cleanup |

---

## 💾 Cơ Sở Dữ Liệu

### Tổng Quan Schema

**45 entities** chia thành 7 nhóm:

| Nhóm | Bảng | Mô tả |
|---|---|---|
| **Foundation** | Roles, Permissions, Role_Permissions, Statuses, SystemParameters | Nền tảng phân quyền & cấu hình |
| **Organization** | Departments, Positions, SystemUsers, Employees, EmployeeAssignments, GradingRanks | Tổ chức & nhân sự |
| **OKR** | MissionVisions, OKRTypes, OKRs, OKRKeyResults, OKR_Mission/Dept/Employee mappings | Mục tiêu chiến lược |
| **KPI** | EvaluationPeriods, KPITypes, KPIProperties, KPIs, KPIDetails, KPI_Dept/Employee assignments, AdhocTasks | Chỉ tiêu đo lường |
| **Check-in** | CheckInStatuses, FailReasons, KPICheckIns, CheckInDetails, CheckInHistoryLogs, GoalComments, OneOnOneMeetings, KPI_Result_Comparisons | Thực thi & theo dõi |
| **Evaluation** | EvaluationResults, KPIAdjustmentHistories, BonusRules, RealtimeExpectedBonuses, HRExportReports, EvaluationReportSummaries, EvaluationReportIncidents | Đánh giá & thưởng |
| **System** | SystemAlerts, AuditLogs, AIGenerationHistories | Hệ thống & AI |

### ERD Tóm Tắt (Quan Hệ Chính)

```
Role ──1:N── SystemUser ──1:1── Employee
  │                                  │
  └── Role_Permission ── Permission  ├── EmployeeAssignment ── Department
                                     │                           │
                                     ├── KPI_Employee_Assignment │
                                     │        │                  ├── KPI_Department_Assignment
                                     │        └── KPI ───────────┘
                                     │             │
                                     │             ├── KPIDetail (target, threshold, schedule)
                                     │             ├── KPICheckIn → CheckInDetail
                                     │             └── OKR → OKRKeyResult
                                     │
                                     ├── EvaluationResult ── GradingRank ── BonusRule
                                     └── SystemAlert (notifications)
```

### Seed Data

File `seeddata.sql` tạo **240 nhân viên**, 12 phòng ban, 36 OKRs, 108 Key Results, KPIs và check-in data cho demo/test.

---

## 🚀 Cài Đặt & Chạy

### Yêu Cầu

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2019+ (hoặc Azure SQL)
- (Tùy chọn) Gemini API Key cho tính năng AI

### Bước 1: Clone & Cấu Hình

```bash
git clone https://github.com/nhubui2008/Manage-KPI-or-OKR-System.git
cd Manage-KPI-or-OKR-System
```

Tạo file `.env` từ mẫu:

```env
ConnectionStrings__DefaultConnection=Server=localhost;Database=KPIorOKRSystem;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False

GOOGLE_CLIENT_ID=your-google-client-id
GOOGLE_CLIENT_SECRET=your-google-client-secret
GEMINI_API_KEY=your-gemini-api-key

SmtpSettings__Server=smtp.gmail.com
SmtpSettings__Port=587
SmtpSettings__SenderName=KPI_System
SmtpSettings__SenderEmail=your-email@gmail.com
SmtpSettings__Password=your-app-password

ForwardedHeaders__Enabled=false
Database__RunMigrationsOnStartup=true
```

### Bước 2: Khởi Tạo Database

```bash
# Cách 1: Auto migration (đặt Database__RunMigrationsOnStartup=true)
dotnet run

# Cách 2: Manual migration
dotnet ef database update

# Cách 3: Seed data demo (240 members)
# Chạy file seeddata.sql trên SQL Server Management Studio
```

### Bước 3: Chạy Ứng Dụng

```bash
dotnet run
# Truy cập: https://localhost:5001
```

### Cấu Hình Nâng Cao (appsettings.json)

```json
{
  "Gemini": { "Model": "gemini-2.5-flash" },
  "DataProtection": { "KeysPath": "App_Data/DataProtection-Keys" },
  "ForwardedHeaders": {
    "Enabled": true,
    "KnownProxies": ["127.0.0.1"],
    "ForwardLimit": 1
  },
  "Database": { "RunMigrationsOnStartup": false }
}
```

---

## 👤 Tài Khoản Demo

| Username | Password | Vai trò | Mô tả |
|---|---|---|---|
| `admin` | `123` | Admin | Toàn quyền hệ thống |
| `director` | `123` | Director | Giám đốc - quản lý chiến lược |
| `manager` | `123` | Manager | Trưởng phòng Công Nghệ |
| `hr` | `123` | HR | Chuyên viên Nhân Sự |
| `employee` | `123` | Employee | Nhân viên phòng IT |

> Mật khẩu mã hóa SHA-256. Seed data tạo thêm 235 tài khoản `user006`→`user240`.

---

## 📂 Cấu Trúc Thư Mục

```
Manage-KPI-or-OKR-System/
├── Controllers/            # 22 controllers (MVC)
│   ├── AIController.cs          # AI endpoints (chat, suggest, analyze)
│   ├── AuthController.cs        # Login, Register, OAuth, Password
│   ├── DashboardController.cs   # Dashboard & charts
│   ├── KPIsController.cs        # KPI CRUD & workflow
│   ├── KPICheckInsController.cs # Check-in, review, tracking
│   ├── OKRsController.cs        # OKR management
│   ├── EvaluationResultsController.cs  # Đánh giá
│   └── ...                      # Employees, Departments, etc.
├── Models/                 # 45 entity models
│   ├── AI/AIModels.cs           # AI request/response DTOs
│   ├── ViewModels/              # View-specific models
│   ├── KPI.cs, OKR.cs, Employee.cs, etc.
│   └── ...
├── Views/                  # 20 view folders + Shared
│   ├── Shared/
│   │   ├── _Layout.cshtml       # Main layout (sidebar, navbar, notifications)
│   │   ├── _AIChatWidget.cshtml # AI chatbot widget
│   │   └── _AiHistoryModal.cshtml
│   ├── Dashboard/, KPIs/, OKRs/, KPICheckIns/, etc.
│   └── Auth/ (Login, Register, ForgotPassword, etc.)
├── Services/               # Business logic & external integrations
│   ├── GeminiService.cs         # Gemini API client (rate-limited)
│   ├── AIDataService*.cs        # AI context builders (6 partial classes)
│   ├── AIAlertService.cs        # Smart risk alerts
│   ├── AIHistoryCleanupService.cs # Background cleanup job
│   ├── NotificationService.cs   # System & KPI deadline alerts
│   ├── EmailService.cs          # SMTP email
│   ├── OKRProgressService.cs    # OKR progress calculation
│   └── PermissionClaimsTransformation.cs
├── Helpers/                # Utility classes
│   ├── AccessScopeHelper.cs     # Data scope by role
│   ├── WorkflowStatusHelper.cs  # KPI/OKR status management
│   ├── ProgressHelper.cs        # Progress % calculation
│   ├── KpiCheckInScheduleHelper.cs # Deadline & schedule logic
│   ├── PermissionAuthorizationHelper.cs
│   ├── CodeGeneratorHelper.cs   # Auto-gen EMP/DEPT/POS codes
│   ├── SeoHelper.cs             # SEO meta tags
│   └── PaginatedList.cs         # Pagination support
├── Filters/
│   └── HasPermissionAttribute.cs # Permission-based auth filter
├── Data/
│   └── MiniERPDbContext.cs      # EF Core DbContext (45 DbSets)
├── wwwroot/
│   ├── css/site.css (89KB)      # Custom styles
│   ├── js/site.js (52KB)        # Client-side logic
│   └── lib/                     # Bootstrap, jQuery, ApexCharts
├── docs/                   # Presentation slides & demo flows
├── seeddata.sql            # 240-member demo dataset
├── Program.cs              # App startup & DI configuration
├── appsettings.json        # Base configuration
└── .env                    # Secrets (not committed)
```

---

## 🔧 Các Tính Năng Kỹ Thuật Nổi Bật

### Security
- **Anti-CSRF**: `AutoValidateAntiforgeryTokenAttribute` toàn cục
- **Cookie Security**: HttpOnly, SameSite=Lax, Secure in production
- **Data Protection**: Persistent keys survive app pool recycles
- **Password**: SHA-256 hashing
- **Forwarded Headers**: Configurable trusted proxies

### Performance
- **AsNoTracking**: Read queries optimized
- **Pagination**: `PaginatedList<T>` cho danh sách lớn
- **Rate Limiting**: Gemini API 15 req/min, 1500 req/day

### Background Services
- **AIHistoryCleanupService**: Tự động xóa lịch sử AI cũ (configurable retention)

### Workflow Engine
- **KPI Status**: Bản nháp → Chờ duyệt → Đang thực hiện → Gần đạt → Hoàn thành/Không đạt/Từ chối/Hủy bỏ
- **Check-in Review**: Pending → Approved/Rejected
- **Evaluation**: Draft → Submitted → Director Reviewed

---

## 📄 License

Dự án sử dụng EPPlus với NonCommercial License.

---

*Phát triển bởi VietMach Team — ASP.NET 10 + Gemini AI + SQL Server*
