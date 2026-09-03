# HƯỚNG DẪN CẤU HÌNH TỰ ĐỘNG DEPLOY LÊN PLESK (CI/CD GITHUB ACTIONS)

Mỗi khi bạn `git push` hoặc merge code vào nhánh **`main`**, GitHub Actions sẽ tự động:
1. Chạy toàn bộ Unit Tests để kiểm tra lỗi code.
2. Biên dịch gói **Self-Contained `win-x64`** (.NET 10).
3. Đẩy code mới lên thư mục `httpdocs` của Plesk qua kết nối FTP an toàn.
4. Tự động mở khóa file IIS (qua `app_offline.htm`) và khởi động lại website với phiên bản mới.
5. **Không bao giờ ghi đè** file `.env`, `App_Data`, `logs`, hay ảnh người dùng tải lên (`wwwroot/uploads`).

---

## BƯỚC 1: LẤY THÔNG TIN FTP TRÊN PLESK

1. Đăng nhập vào bảng điều khiển **Plesk**.
2. Vào domain của bạn (`manasys.site`) -> chọn mục **FTP Access** (hoặc **Tài khoản FTP**).
3. Tại đây bạn sẽ thấy:
   - **Tên miền / Máy chủ FTP (FTP Server)**: Thường là `manasys.site` hoặc IP máy chủ hosting (ví dụ: IP của hosting bạn đang dùng).
   - **Tên người dùng FTP (FTP Username)**: Tài khoản FTP của bạn.
   - **Mật khẩu (FTP Password)**: Mật khẩu bạn đã đặt cho tài khoản FTP (nếu quên có thể bấm vào tài khoản để đổi mật khẩu mới).
   - **Thư mục chính (Home directory)**:
     - Nếu Home directory là `/` (root của hosting) thì thư mục web sẽ là `/httpdocs/`.
     - Nếu Home directory trỏ thẳng vào `/httpdocs` thì thư mục web sẽ là `/`.

---

## BƯỚC 2: THÊM SECRETS VÀO GITHUB REPOSITORY

Để GitHub Actions có quyền kết nối và đẩy file lên Plesk mà không làm lộ mật khẩu, bạn cần lưu thông tin vào mục Secrets của GitHub:

1. Mở GitHub trong trình duyệt, truy cập vào repository của bạn:
   👉 `https://github.com/nhubui2008/Manage-KPI-or-OKR-System`
2. Bấm vào tab **Settings** (Cài đặt của Repo) ở thanh menu trên cùng.
3. Ở cột bên trái, tìm mục **Security** -> chọn **Secrets and variables** -> bấm **Actions**.
4. Nhấn nút xanh **New repository secret** để thêm lần lượt các Secret sau:

| Tên Secret (Name) | Giá trị (Secret Value) | Ghi chú |
| :--- | :--- | :--- |
| **`FTP_SERVER`** | `manasys.site` *(hoặc IP hosting)* | Địa chỉ máy chủ FTP |
| **`FTP_USERNAME`** | *tên_tài_khoản_ftp* | Username FTP trên Plesk |
| **`FTP_PASSWORD`** | *mật_khẩu_ftp* | Password FTP trên Plesk |
| **`FTP_SERVER_DIR`** *(Tùy chọn)* | `/httpdocs/` | Để trống nếu Home dir FTP đã trỏ sẵn vào httpdocs |

---

## BƯỚC 3: KIỂM TRA TỰ ĐỘNG DEPLOY

Sau khi thêm xong các Secret trên:
1. Bạn commit và push bất kỳ thay đổi nào lên nhánh `main`:
   ```bash
   git add .
   git commit -m "feat: setup auto-deploy to plesk"
   git push origin main
   ```
2. Mở tab **Actions** trên GitHub repository:
   - Bạn sẽ thấy workflow **Deploy to Plesk Windows IIS** đang chạy.
   - Bấm vào để theo dõi trực tiếp các bước: `Run Unit Tests` ➔ `Publish Self-Contained` ➔ `Sync All Application Files via FTP`.
3. Khi workflow hiện tích xanh ✅, trang web `manasys.site` của bạn đã được cập nhật phiên bản mới nhất!

---

## GHI CHÚ QUAN TRỌNG

- **File `.env`**: GitHub Actions đã được cấu hình tự động bỏ qua (exclude) file `.env`. Do đó, file `.env` chứa chuỗi kết nối Database và secret keys trên Plesk của bạn sẽ luôn được giữ nguyên vẹn và bảo mật tuyệt đối.
- **Dữ liệu người dùng**: Thư mục `wwwroot/uploads/` và `App_Data/` cũng được loại trừ, đảm bảo không bị xóa hay ghi đè khi cập nhật code mới.
