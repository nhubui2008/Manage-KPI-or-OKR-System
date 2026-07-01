---
name: datn-html-slide-report
summary: Tạo slide báo cáo DATN dạng HTML 16:9, có đồng hồ đếm ngược, nhúng video demo 3 phút và luồng demo trực tiếp cho bảo vệ online FPT Polytechnic.
version: 1.0.0
---

# SKILL: Tạo slide HTML báo cáo dự án tốt nghiệp FPT Polytechnic

## 1. Mục tiêu

Skill này dùng để tạo **bộ slide báo cáo dự án tốt nghiệp dạng HTML** thay cho PowerPoint. Slide phải chạy tốt khi báo cáo online, có thể mở trực tiếp bằng trình duyệt, có **đếm ngược thời gian trình bày**, có **slide video demo 3 phút**, và có phần **demo trực tiếp** theo đúng quy trình:

```text
Giới thiệu → Video demo → Demo trực tiếp → Kỹ thuật / Kiểm thử / Tổng kết → Hỏi đáp
```

Áp dụng mặc định cho đề tài:

```text
Hệ thống vận hành thông minh cho doanh nghiệp vừa và nhỏ,
hỗ trợ quản lý đa cấp và đưa ra quyết định bằng AI
```

Công nghệ dự án mặc định:

```text
ASP.NET MVC 10 + SQL Server + AI API
```

## 2. Đầu ra bắt buộc

Khi được yêu cầu tạo slide, phải tạo tối thiểu các file sau:

```text
/datn-html-slides/
├── index.html
├── assets/
│   ├── demo.mp4              # video demo nếu người dùng cung cấp
│   ├── logo.png              # nếu có
│   └── screenshots/          # ảnh màn hình nếu có
└── README.md                 # hướng dẫn mở slide và thay video
```

Nếu người dùng yêu cầu **1 file duy nhất**, có thể nhúng video bằng base64, nhưng chỉ làm khi video nhỏ. Mặc định ưu tiên dùng:

```html
<video src="assets/demo.mp4" controls></video>
```

vì cách này nhẹ, ổn định và dễ thay video.

## 3. Thời lượng chuẩn

Tổng buổi bảo vệ:

```text
30 phút
├── 15 phút trình bày
└── 15 phút hỏi đáp
```

Bộ slide phải có đồng hồ đếm ngược mặc định **15:00**.

Timeline trình bày:

```text
00:00 - 00:45   Chào hội đồng, giới thiệu đề tài
00:45 - 02:00   Bối cảnh, vấn đề, lý do chọn đề tài
02:00 - 03:00   Mục tiêu, phạm vi, đối tượng sử dụng
03:00 - 03:15   Dẫn vào video demo
03:15 - 06:15   Chiếu video demo 3 phút
06:15 - 11:30   Demo trực tiếp
11:30 - 13:00   Công nghệ, database, AI, kiểm thử
13:00 - 14:30   Kết quả, hạn chế, hướng phát triển
14:30 - 15:00   Cảm ơn, mời hội đồng đặt câu hỏi
```

Nếu người dùng có thời lượng khác, thay đổi timer và timeline tương ứng.

## 4. Cấu trúc slide mặc định

Tạo khoảng **10–12 slide**, không nhồi quá nhiều chữ.

### Slide 1: Trang bìa

Nội dung:

- Trường Cao đẳng FPT Polytechnic
- Báo cáo dự án tốt nghiệp
- Tên đề tài
- Nhóm thực hiện
- Giảng viên hướng dẫn
- Công nghệ chính

### Slide 2: Thành viên nhóm

Mặc định nhóm 7 người:

| Thành viên | Vai trò |
|---|---|
| Quân | Leader, AI |
| Như | Frontend KPI/OKR |
| An | Backend KPI/OKR |
| Nhật | Frontend vận hành |
| Bảo | Backend vận hành |
| Phong | Fullstack AI, order feature |
| Khánh | Tester |

### Slide 3: Bối cảnh và vấn đề

Nêu vấn đề thực tế:

- Doanh nghiệp vừa và nhỏ quản lý công việc, KPI/OKR còn rời rạc.
- Dữ liệu nằm ở Excel, Zalo, Google Sheet, Trello hoặc nhiều công cụ khác nhau.
- Quản lý khó theo dõi tiến độ, hiệu suất và rủi ro vận hành theo thời gian thực.
- Thiếu công cụ AI hỗ trợ tổng hợp, phân tích và đề xuất hành động.

### Slide 4: Mục tiêu và phạm vi

Nêu các mục tiêu chính:

- Quản lý người dùng, vai trò, phòng ban.
- Quản lý KPI/OKR theo cá nhân, nhóm, phòng ban.
- Quản lý công việc và vận hành nội bộ.
- Dashboard báo cáo.
- AI hỗ trợ phân tích tiến độ, cảnh báo rủi ro và gợi ý hành động.

### Slide 5: Quy trình nghiệp vụ tổng quan

Hiển thị luồng:

```text
Tạo phòng ban / nhân sự
        ↓
Thiết lập KPI/OKR
        ↓
Giao việc / vận hành
        ↓
Nhân viên cập nhật tiến độ
        ↓
Dashboard tổng hợp
        ↓
AI phân tích / cảnh báo / đề xuất
        ↓
Đánh giá kết quả
```

### Slide 6: Video demo 3 phút

Slide này bắt buộc có video:

```html
<video id="demoVideo" src="assets/demo.mp4" controls preload="metadata"></video>
```

Yêu cầu:

- Video phải nằm giữa slide.
- Có nút “Phát video demo”.
- Có chú thích: “Video demo tổng quan hệ thống trong 3 phút”.
- Khi bấm phát video, timer tổng vẫn tiếp tục chạy, trừ khi người dùng bật chế độ pause.

### Slide 7: Demo trực tiếp

Slide này không chứa video, mà là checklist thao tác demo trực tiếp:

```text
1. Đăng nhập theo vai trò
2. Dashboard tổng quan
3. KPI/OKR
4. Công việc / vận hành
5. Cập nhật tiến độ
6. AI phân tích / cảnh báo / gợi ý
7. Báo cáo / thống kê
```

Có thể thêm nút mở link demo:

```html
<a class="demo-link" href="https://your-demo-url" target="_blank">Mở hệ thống demo</a>
```

Nếu chưa có link demo, để placeholder:

```text
{{DEMO_URL}}
```

### Slide 8: Thiết kế hệ thống / Database

Chỉ trình bày nhóm bảng chính:

- Users, Roles, Permissions
- Departments, Positions
- KPIs, Objectives, KeyResults, KPIProgress
- Tasks, TaskComments, TaskHistories
- AIInsights, Reports, Notifications, AuditLogs

Nếu có ERD ảnh, đặt trong `assets/screenshots/erd.png` và hiển thị trên slide.

### Slide 9: Công nghệ và kiến trúc

Nêu công nghệ:

- ASP.NET MVC 10
- SQL Server
- Entity Framework Core
- HTML/CSS/JS
- Bootstrap/Tailwind nếu có
- Chart.js/ApexCharts nếu có
- AI API
- GitHub

Nêu kiến trúc:

```text
View → Controller → Service → Repository → Database
                         ↓
                      AI Service
```

### Slide 10: Kiểm thử

Nêu nội dung test:

- Đăng nhập, phân quyền.
- Quản lý KPI/OKR.
- Quản lý vận hành/công việc.
- Dashboard.
- AI Assistant.
- Validate form.
- Quyền truy cập dữ liệu.

Nếu có số liệu test case thì hiển thị:

```text
Tổng test case: {{TOTAL_TC}}
Pass: {{PASS_TC}}
Fail ban đầu: {{FAIL_TC}}
Đã sửa: {{FIXED_BUG}}
Còn tồn: {{OPEN_BUG}}
```

### Slide 11: Kết quả đạt được và hạn chế

Kết quả:

- Hoàn thiện các chức năng cốt lõi.
- Có luồng KPI/OKR.
- Có luồng vận hành/công việc.
- Có dashboard.
- Có AI hỗ trợ phân tích.
- Có kiểm thử và sửa lỗi.

Hạn chế:

- AI phụ thuộc chất lượng dữ liệu đầu vào.
- Chưa cá nhân hóa sâu cho từng doanh nghiệp.
- Chưa có mobile app riêng.
- Dashboard có thể cần tối ưu thêm khi dữ liệu lớn.

### Slide 12: Hướng phát triển và cảm ơn

Hướng phát triển:

- Tối ưu dashboard realtime.
- Tích hợp email/Zalo/Google Calendar.
- Phát triển mobile app.
- Cá nhân hóa AI theo từng doanh nghiệp.
- Bổ sung báo cáo BI nâng cao.

Kết thúc:

```text
Nhóm em xin cảm ơn thầy cô đã lắng nghe.
Nhóm em xin nhận câu hỏi từ hội đồng.
```

## 5. Yêu cầu giao diện HTML

### Bắt buộc

- Slide tỷ lệ **16:9**.
- Chạy offline bằng trình duyệt.
- Có điều hướng bằng bàn phím.
- Có đồng hồ đếm ngược cố định ở góc phải trên.
- Có thanh tiến độ slide.
- Có số slide hiện tại / tổng số slide.
- Có chế độ toàn màn hình.
- Có hỗ trợ video demo.
- Có notes ngắn cho người trình bày, có thể bật/tắt.

### Phím tắt

```text
ArrowRight / Space  → Slide tiếp theo
ArrowLeft           → Slide trước
F                   → Bật/tắt fullscreen
T                   → Tạm dừng / tiếp tục timer
R                   → Reset timer về 15:00
N                   → Bật/tắt presenter notes
V                   → Phát / tạm dừng video demo nếu đang ở slide video
```

## 6. Yêu cầu timer đếm ngược

Timer phải có:

- Thời gian mặc định: 15 phút.
- Hiển thị dạng `MM:SS`.
- Khi còn dưới 5 phút, thêm trạng thái cảnh báo nhẹ.
- Khi còn dưới 1 phút, thêm trạng thái cảnh báo mạnh.
- Khi hết giờ, hiển thị `00:00` và thông báo “Hết thời gian trình bày”.
- Không tự động chuyển slide.

HTML mẫu:

```html
<div id="timer" class="timer">15:00</div>
<button id="timerToggle">Pause</button>
<button id="timerReset">Reset</button>
```

JavaScript mẫu:

```javascript
const TOTAL_SECONDS = 15 * 60;
let remainingSeconds = TOTAL_SECONDS;
let timerRunning = true;

function formatTime(seconds) {
  const m = Math.floor(seconds / 60).toString().padStart(2, '0');
  const s = Math.floor(seconds % 60).toString().padStart(2, '0');
  return `${m}:${s}`;
}

function updateTimer() {
  const timer = document.getElementById('timer');
  timer.textContent = formatTime(remainingSeconds);
  timer.classList.toggle('warning', remainingSeconds <= 300 && remainingSeconds > 60);
  timer.classList.toggle('danger', remainingSeconds <= 60);

  if (remainingSeconds <= 0) {
    timer.textContent = '00:00';
    timerRunning = false;
    document.body.classList.add('time-up');
  }
}

setInterval(() => {
  if (timerRunning && remainingSeconds > 0) {
    remainingSeconds -= 1;
    updateTimer();
  }
}, 1000);
```

## 7. Yêu cầu video demo

### Cách nhúng mặc định

Video đặt tại:

```text
assets/demo.mp4
```

Trong HTML:

```html
<video id="demoVideo" controls preload="metadata">
  <source src="assets/demo.mp4" type="video/mp4" />
  Trình duyệt của bạn không hỗ trợ video.
</video>
```

### Khi người dùng cung cấp video

Nếu đang tạo file trong môi trường có quyền thao tác file:

1. Tạo thư mục `assets` nếu chưa có.
2. Copy video người dùng cung cấp vào `assets/demo.mp4`.
3. Nếu video không phải `.mp4`, giữ tên gốc và cập nhật `src` tương ứng.
4. Không đổi chất lượng video trừ khi người dùng yêu cầu nén.

### Nếu người dùng muốn import video khi đang mở HTML

Có thể thêm input chọn file:

```html
<input id="videoPicker" type="file" accept="video/*" />
<video id="demoVideo" controls></video>

<script>
  document.getElementById('videoPicker').addEventListener('change', (event) => {
    const file = event.target.files[0];
    if (!file) return;
    const url = URL.createObjectURL(file);
    const video = document.getElementById('demoVideo');
    video.src = url;
    video.load();
  });
</script>
```

Lưu ý: cách này chỉ dùng tạm trong trình duyệt, không lưu video vĩnh viễn vào HTML.

## 8. Kịch bản nói theo từng thành viên

### Quân — Leader, AI

Mở đầu:

```text
Em xin kính chào quý thầy cô trong hội đồng.
Nhóm em xin phép bắt đầu phần báo cáo dự án tốt nghiệp với đề tài:
“Hệ thống vận hành thông minh cho doanh nghiệp vừa và nhỏ,
hỗ trợ quản lý đa cấp và đưa ra quyết định bằng AI”.

Dự án tập trung vào hai nhóm nghiệp vụ chính là quản lý KPI/OKR và quản lý vận hành nội bộ,
đồng thời tích hợp AI để hỗ trợ phân tích dữ liệu, cảnh báo rủi ro và gợi ý hành động cho nhà quản lý.
```

Giới thiệu thành viên:

```text
Nhóm em gồm 7 thành viên.
Em là Quân, phụ trách leader và phần AI.
Bạn Như phụ trách frontend KPI/OKR.
Bạn An phụ trách backend KPI/OKR.
Bạn Nhật phụ trách frontend vận hành.
Bạn Bảo phụ trách backend vận hành.
Bạn Phong phụ trách fullstack AI và order feature.
Bạn Khánh phụ trách kiểm thử hệ thống.
```

Dẫn video:

```text
Trước khi demo trực tiếp, nhóm em xin phép trình chiếu video demo ngắn khoảng 3 phút
để thầy cô có cái nhìn tổng quan về các chức năng chính của hệ thống.
```

### Như — Frontend KPI/OKR

```text
Em xin trình bày phần frontend KPI/OKR.
Ở màn hình này, người quản lý có thể xem danh sách mục tiêu, trạng thái thực hiện,
tiến độ và các chỉ số liên quan.
Giao diện được thiết kế để người dùng dễ theo dõi KPI/OKR theo cá nhân, nhóm hoặc phòng ban.
```

### An — Backend KPI/OKR

```text
Em phụ trách backend cho module KPI/OKR.
Backend xử lý các nghiệp vụ như tạo KPI/OKR, gán mục tiêu cho nhân viên hoặc phòng ban,
lưu tiến độ cập nhật và tính toán tỷ lệ hoàn thành.
Dữ liệu KPI/OKR được liên kết với người dùng, phòng ban và chu kỳ đánh giá,
giúp hệ thống tổng hợp dashboard và phục vụ phân tích AI.
```

### Nhật — Frontend vận hành

```text
Em xin trình bày phần frontend vận hành.
Module này giúp quản lý và nhân viên theo dõi công việc hằng ngày,
trạng thái xử lý và tiến độ từng đầu việc.
Giao diện được tổ chức theo dạng danh sách hoặc Kanban để người dùng dễ quan sát.
```

### Bảo — Backend vận hành

```text
Em phụ trách backend cho module vận hành.
Backend xử lý việc tạo công việc, phân công người thực hiện, cập nhật trạng thái,
lưu lịch sử thay đổi và kiểm soát quyền truy cập theo vai trò.
```

### Phong — Fullstack AI, order feature

```text
Em phụ trách fullstack phần AI và order feature.
Với AI, hệ thống lấy dữ liệu từ KPI/OKR, công việc và tiến độ thực hiện để tạo ngữ cảnh phân tích.
AI sau đó đưa ra nhận xét, cảnh báo rủi ro hoặc gợi ý hành động cho người quản lý.
```

### Khánh — Tester

```text
Em phụ trách kiểm thử hệ thống.
Nhóm đã kiểm thử các luồng chính gồm đăng nhập, phân quyền, quản lý KPI/OKR,
quản lý công việc vận hành, dashboard, AI và validate dữ liệu đầu vào.
Trong quá trình test, nhóm ghi nhận lỗi, chuyển lại cho developer xử lý và kiểm tra lại sau khi sửa.
```

## 9. Slide demo trực tiếp nên có checklist

Trong slide demo trực tiếp, tạo bảng:

| Thứ tự | Người demo | Nội dung | Trạng thái |
|---|---|---|---|
| 1 | Như | KPI/OKR frontend | Sẵn sàng |
| 2 | An | Luồng backend KPI/OKR | Sẵn sàng |
| 3 | Nhật | Vận hành frontend | Sẵn sàng |
| 4 | Bảo | Luồng backend vận hành | Sẵn sàng |
| 5 | Phong | AI + order feature | Sẵn sàng |
| 6 | Khánh | Kiểm thử | Sẵn sàng |

## 10. Nội dung README.md

README phải có:

```text
# Hướng dẫn mở slide báo cáo DATN

1. Mở file index.html bằng Chrome hoặc Edge.
2. Đặt video demo tại assets/demo.mp4.
3. Nếu video không chạy, kiểm tra tên file và định dạng .mp4.
4. Bấm F để bật toàn màn hình.
5. Dùng phím mũi tên hoặc Space để chuyển slide.
6. Bấm T để tạm dừng/tiếp tục timer.
7. Bấm R để reset timer.
8. Khi báo cáo online, nhớ bật Share audio khi chiếu video.
```

## 11. Checklist trước khi xuất file

Trước khi trả file cho người dùng, kiểm tra:

- [ ] `index.html` mở được bằng trình duyệt.
- [ ] Slide hiển thị đúng 16:9.
- [ ] Chuyển slide bằng bàn phím hoạt động.
- [ ] Timer chạy đúng từ 15:00.
- [ ] Pause/reset timer hoạt động.
- [ ] Video demo hiển thị trên slide video.
- [ ] Nếu có video thật, đường dẫn `assets/demo.mp4` đúng.
- [ ] Link demo trực tiếp mở tab mới.
- [ ] Không có chữ placeholder quan trọng chưa thay, trừ khi người dùng chưa cung cấp dữ liệu.
- [ ] Font dễ đọc khi chia sẻ màn hình online.
- [ ] Slide không quá nhiều chữ.

## 12. Quy tắc viết nội dung slide

- Không copy nguyên báo cáo vào slide.
- Mỗi slide chỉ nên có 3–5 ý chính.
- Ưu tiên sơ đồ luồng, bảng vai trò, checklist demo.
- Với phần kỹ thuật, chỉ nêu kiến trúc và bảng chính, không đọc chi tiết từng bảng database.
- Với phần AI, phải nói rõ AI dùng dữ liệu nào, xử lý ở đâu, trả về kết quả gì.
- Với phần kiểm thử, phải có lỗi đã test thật, không nói chung chung.

## 13. Prompt mẫu để gọi skill

Người dùng có thể yêu cầu:

```text
Dùng skill datn-html-slide-report tạo slide HTML báo cáo online 15 phút cho nhóm 7 người.
Có video demo 3 phút tại ./demo.mp4, link demo là https://...
Đề tài: Hệ thống vận hành thông minh cho doanh nghiệp vừa và nhỏ, hỗ trợ quản lý đa cấp và đưa ra quyết định bằng AI.
```

Khi nhận yêu cầu trên, hãy tạo `index.html`, copy video vào `assets/demo.mp4` nếu có, và tạo README hướng dẫn chạy.
